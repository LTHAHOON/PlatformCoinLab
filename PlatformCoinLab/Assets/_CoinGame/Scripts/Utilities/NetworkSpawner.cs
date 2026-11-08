using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using CoinGame.Network;

namespace CoinGame.Utilities
{
    // Kept as a component to preserve Netcode behavior and inherited serialized fields.
    public class NetworkSpawner : BaseSpawner
    {
        private NetworkGameObjectPool _networkObjectPool;
        private NetworkObject _spawnNetworkParent;
        private bool _poolHandlerRegistered;
        private Dictionary<ulong, CancellationTokenSource> _playerSpawnTokens;

        protected override void OnPrefabReady()
        {
            if (_spawnParent != null && !_spawnParent.TryGetComponent(out _spawnNetworkParent))
            {
                Debug.LogError("SpawnParent requires a NetworkObject component.", this);
                return;
            }

            if (_canUseObjectPool)
            {
                _networkObjectPool = new NetworkGameObjectPool(_spawnPrefab, _spawnCount, _spawnCount, _spawnParent);
                RegisterPoolHandlerAsync(destroyCancellationToken).Forget();
            }
        }

        protected override async UniTask<CancellationToken?> SpawnOneAsync(CancellationToken token)
        {
            if (!_spawnPrefab.TryGetComponent(out NetworkObject prefabNetworkObject))
            {
                Debug.LogError("SpawnPrefab requires a NetworkObject component.", this);
                return null;
            }
            await UniTask.WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening, cancellationToken: token);
            if (!NetworkManager.Singleton.IsServer)
            {
                return null;
            }

            Vector3 position = await GetSpawnPosition(token);
            EnsurePoolHandler();
            NetworkObject instance = _networkObjectPool != null
                ? _networkObjectPool.Get().GetComponent<NetworkObject>()
                : Instantiate(prefabNetworkObject, position, Quaternion.identity, _spawnParent);
            if (instance == null)
            {
                return null;
            }

            instance.transform.position = position;
            if (_spawnNetworkParent != null)
            {
                await UniTask.WaitUntil(() => _spawnNetworkParent.IsSpawned, cancellationToken: token);
            }

            instance.Spawn();
            if (_spawnNetworkParent != null && !instance.TrySetParent(_spawnNetworkParent))
            {
                Debug.LogWarning($"Failed to parent {instance.name}.", this);
            }

            return _networkObjectPool != null ? _networkObjectPool.GetDespawnToken(instance.gameObject) : instance.destroyCancellationToken;
        }

        private async UniTask RegisterPoolHandlerAsync(CancellationToken token)
        {
            try
            {
                await UniTask.WaitUntil(() => NetworkManager.Singleton != null, cancellationToken: token);
                EnsurePoolHandler();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void EnsurePoolHandler()
        {
            if (_networkObjectPool == null || _poolHandlerRegistered || NetworkManager.Singleton == null)
            {
                return;
            }

            _poolHandlerRegistered = NetworkManager.Singleton.PrefabHandler.AddHandler(
                _spawnPrefab, new PooledNetworkPrefabHandler(_networkObjectPool));
        }

        public bool TryRelocatePlayers()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || !_isDefaultPlayerSpawner)
            {
                return false;
            }

            Physics.SyncTransforms();
            var assignments = new List<(NetworkPlayerController player, Vector3 position, float radius)>();
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                {
                    return false;
                }

                NetworkPlayerController player = client.PlayerObject.GetComponent<NetworkPlayerController>();
                CharacterController controller = client.PlayerObject.GetComponent<CharacterController>();
                if (player == null || controller == null)
                {
                    return false;
                }

                float radius = controller.radius * Mathf.Max(player.transform.lossyScale.x, player.transform.lossyScale.z);
                bool found = false;
                Vector3 candidate = default;
                for (int attempt = 0; attempt < MaxIterations * 4; attempt++)
                {
                    candidate = transform.position + new Vector3(
                        UnityEngine.Random.Range(-_spawnArea.x / 2f, _spawnArea.x / 2f), 0f,
                        UnityEngine.Random.Range(-_spawnArea.z / 2f, _spawnArea.z / 2f));
                    float halfHeight = Mathf.Max(radius, controller.height * player.transform.lossyScale.y / 2f);
                    Vector3 center = candidate + Vector3.Scale(controller.center, player.transform.lossyScale);
                    Vector3 offset = Vector3.up * (halfHeight - radius);
                    bool blocked = false;
                    foreach (Collider hit in Physics.OverlapCapsule(center - offset, center + offset, radius, _blockingLayerMask, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.transform.root != player.transform.root)
                        {
                            blocked = true;
                            break;
                        }
                    }

                    foreach (var assignment in assignments)
                    {
                        Vector3 delta = candidate - assignment.position;
                        delta.y = 0f;
                        if (delta.sqrMagnitude < Mathf.Pow(radius + assignment.radius + 0.05f, 2f))
                        {
                            blocked = true;
                            break;
                        }
                    }

                    if (!blocked)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }

                assignments.Add((player, candidate, radius));
            }

            foreach (var assignment in assignments)
            {
                assignment.player.RelocateForGameRpc(assignment.position);
            }

            return true;
        }

        public bool TryRelocatePlayer(NetworkPlayerController player)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || player == null)
            {
                return false;
            }

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller == null)
            {
                return false;
            }

            float radius = controller.radius * Mathf.Max(player.transform.lossyScale.x, player.transform.lossyScale.z);
            for (int attempt = 0; attempt < MaxIterations * 4; attempt++)
            {
                Vector3 candidate = transform.position + new Vector3(
                    UnityEngine.Random.Range(-_spawnArea.x / 2f, _spawnArea.x / 2f), 0f,
                    UnityEngine.Random.Range(-_spawnArea.z / 2f, _spawnArea.z / 2f));
                float halfHeight = Mathf.Max(radius, controller.height * player.transform.lossyScale.y / 2f);
                Vector3 center = candidate + Vector3.Scale(controller.center, player.transform.lossyScale);
                Vector3 offset = Vector3.up * (halfHeight - radius);
                bool blocked = false;
                foreach (Collider hit in Physics.OverlapCapsule(
                    center - offset, center + offset, radius, _blockingLayerMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform.root != player.transform.root)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                {
                    player.RelocateForGameRpc(candidate);
                    return true;
                }
            }

            return false;
        }

        public void SpawnPlayer(ulong clientId)
        {
            if (_spawnPrefab == null)
            {
                SpawnPlayerWhenReadyAsync(clientId).Forget();
                return;
            }

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || !_isDefaultPlayerSpawner)
            {
                return;
            }

            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId) || !_spawnPrefab.TryGetComponent(out NetworkObject prefab))
            {
                return;
            }

            _playerSpawnTokens ??= new();
            if (_playerSpawnTokens.ContainsKey(clientId))
            {
                return;
            }

            _playerSpawnTokens[clientId] = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            SpawnPlayerAsync(_playerSpawnTokens[clientId].Token, prefab, clientId).Forget();
        }

        private async UniTask SpawnPlayerWhenReadyAsync(ulong clientId)
        {
            try
            {
                await _spawnPrefabInitializationTask;
                if (_spawnPrefab != null)
                {
                    SpawnPlayer(clientId);
                }
            }
            catch (OperationCanceledException) { }
        }

        public void CancelSpawnPlayer(ulong clientId)
        {
            if (_playerSpawnTokens == null || !_playerSpawnTokens.Remove(clientId, out CancellationTokenSource cts))
            {
                return;
            }

            cts.Cancel();
            cts.Dispose();
        }

        private async UniTask SpawnPlayerAsync(CancellationToken token, NetworkObject prefab, ulong clientId)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_spawnDelay), cancellationToken: token);
                await UniTask.WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening, cancellationToken: token);
                NetworkObject instance = Instantiate(prefab, await GetSpawnPosition(token), Quaternion.identity);
                instance.SpawnAsPlayerObject(clientId);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        protected override void OnDestroy()
        {
            if (_poolHandlerRegistered && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.PrefabHandler.RemoveHandler(_spawnPrefab);
            }

            _networkObjectPool?.Dispose();
            if (_playerSpawnTokens != null)
            {
                foreach (CancellationTokenSource cts in _playerSpawnTokens.Values)
                {
                    cts.Cancel();
                    cts.Dispose();
                }
            }

            base.OnDestroy();
        }
    }
}
