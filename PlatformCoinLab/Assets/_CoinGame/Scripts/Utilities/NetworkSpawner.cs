using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace CoinGame.Utilities
{
    using CoinGame.Network;
    using CoinGame.Player;
    using CoinGame.Session;
    using CoinGame.Stage;

    // Kept as a component to preserve Netcode behavior and inherited serialized fields.
    public class NetworkSpawner : BaseSpawner
    {
        private NetworkGameObjectPool _networkObjectPool;
        private NetworkObject _spawnNetworkParent;
        private bool _poolHandlerRegistered;
        private Dictionary<ulong, CancellationTokenSource> _playerSpawnTokens;
        [SerializeField]
        private List<BoxCollider> _playerZones = new();
        [SerializeField]
        private List<BoxCollider> _collectorPlayerZones = new();
        [SerializeField]
        private List<BoxCollider> _disruptorPlayerZones = new();
        [SerializeField]
        private GridSpawnMap _gridSpawnMap;
        [SerializeField]
        private GridSpawnKind _spawnKind = GridSpawnKind.Coin;
        [SerializeField]
        private StageConfig _stageConfig;
        [SerializeField]
        private SpawnPlacementSettings _placementSettings;
        private readonly NetworkSpawnBatch _spawnBatch = new();

        protected override int GetRoundSpawnCount(StageSession stageSession)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return 0;
            }

            _spawnBatch.Clear();
            if (_spawnKind == GridSpawnKind.Crate)
            {
                return _stageConfig.GetCrateCount();
            }

            return NetworkRoundState.Instance.SpawnCoins;
        }

        protected override async UniTask<Vector3> GetSpawnPosition(CancellationToken token)
        {
            if (_isDefaultPlayerSpawner)
            {
                return await GetPlayerSpawnPosition(token);
            }

            throw new InvalidOperationException("Dynamic spawns require a GridSpawnMap.");
        }

        private async UniTask<Vector3> GetPlayerSpawnPosition(CancellationToken token)
        {
            List<BoxCollider> zones = ResolvePlayerZones();
            while (zones.Count == 0)
            {
                token.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                zones = ResolvePlayerZones();
            }

            for (int attempt = 0; attempt < MaxIterations; attempt++)
            {
                token.ThrowIfCancellationRequested();
                if (TryGetZoneCandidate(zones, out Vector3 candidate))
                {
                    if (!Physics.CheckSphere(candidate, _checkRadius, _blockingLayerMask, QueryTriggerInteraction.Ignore))
                    {
                        return candidate;
                    }
                }
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            Debug.LogError("Player spawn zones could not provide a valid position.", this);
            throw new InvalidOperationException("Player spawn zones cannot provide a valid position.");
        }

        private bool TryGetZoneCandidate(List<BoxCollider> zones, out Vector3 candidate)
        {
            candidate = default;
            if (zones == null || zones.Count == 0)
            {
                return false;
            }

            Bounds bounds = zones[UnityEngine.Random.Range(0, zones.Count)].bounds;
            Vector3 rayOrigin = new(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y + 20f,
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z));
            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit ground, 50f, LayerMask.GetMask("Ground", "Default")))
            {
                return false;
            }

            candidate = ground.point;
            return true;
        }

        private List<BoxCollider> GetPlayerZones(NetworkPlayerStats stats)
        {
            RemoveNullZones(_collectorPlayerZones);
            RemoveNullZones(_disruptorPlayerZones);
            List<BoxCollider> playerZones = ResolvePlayerZones();
            List<BoxCollider> collectorZones = HasZones(_collectorPlayerZones)
                ? _collectorPlayerZones
                : GetRegistryZones(SpawnZoneRegistry.Instance != null ? SpawnZoneRegistry.Instance.CollectorPlayerZones : null, playerZones);
            List<BoxCollider> disruptorZones = HasZones(_disruptorPlayerZones)
                ? _disruptorPlayerZones
                : GetRegistryZones(SpawnZoneRegistry.Instance != null ? SpawnZoneRegistry.Instance.DisruptorPlayerZones : null, playerZones);

            if (stats != null && stats.Role == PlayerRole.Collector && collectorZones.Count > 0)
            {
                return collectorZones;
            }

            if (stats != null && stats.Role == PlayerRole.Disruptor && disruptorZones.Count > 0)
            {
                return disruptorZones;
            }

            return playerZones;
        }

        private List<BoxCollider> ResolvePlayerZones()
        {
            if (_playerZones == null)
            {
                _playerZones = new();
            }

            _playerZones.RemoveAll(zone => zone == null);
            if (_playerZones.Count > 0)
            {
                return _playerZones;
            }

            if (SpawnZoneRegistry.Instance != null)
            {
                List<BoxCollider> registryZones = SpawnZoneRegistry.Instance.PlayerZones;
                RemoveNullZones(registryZones);
                if (HasZones(registryZones))
                {
                    return registryZones;
                }
            }

            foreach (BoxCollider collider in FindObjectsByType<BoxCollider>())
            {
                if (collider.name == "WaitingPlayerSpawnZone")
                {
                    _playerZones.Add(collider);
                    return _playerZones;
                }
            }

            return _playerZones;
        }

        private static List<BoxCollider> GetRegistryZones(List<BoxCollider> zones, List<BoxCollider> fallback)
        {
            RemoveNullZones(zones);
            return HasZones(zones) ? zones : fallback;
        }

        private static bool HasZones(List<BoxCollider> zones)
        {
            return zones != null && zones.Count > 0;
        }

        private static void RemoveNullZones(List<BoxCollider> zones)
        {
            if (zones != null)
            {
                zones.RemoveAll(zone => zone == null);
            }
        }

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
            await UniTask.WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening, cancellationToken: token);
            if (!NetworkManager.Singleton.IsServer)
            {
                return null;
            }

            if (_gridSpawnMap == null || _spawnKind == GridSpawnKind.Player)
            {
                throw new InvalidOperationException("Dynamic spawns require a GridSpawnMap and Coin or Crate kind.");
            }

            Physics.SyncTransforms();
            if (!GridSpawnPlacement.TrySelect(_gridSpawnMap, _spawnKind, _spawnPrefab, _placementSettings,
                out Pose pose, out int reservationId, out bool usedFallback))
            {
                throw new InvalidOperationException($"No valid Ground placement for {_spawnKind}.");
            }

            if (usedFallback)
            {
                Debug.LogWarning($"{name}: no fully clear front; using the greatest available front clearance.", this);
            }

            NetworkObject instance = null;
            try
            {
                EnsurePoolHandler();
                if (_networkObjectPool != null)
                {
                    instance = _networkObjectPool.Get().GetComponent<NetworkObject>();
                }
                else
                {
                    instance = Instantiate(_spawnPrefab, pose.position, pose.rotation, _spawnParent).GetComponent<NetworkObject>();
                }

                instance.transform.SetPositionAndRotation(pose.position, pose.rotation);
                if (_spawnKind == GridSpawnKind.Crate)
                {
                    instance.GetComponent<NetworkCrate>().SetGridReservation(reservationId);
                }
                else
                {
                    instance.GetComponent<NetworkCoinController>().SetGridReservation(reservationId);
                }

                if (_spawnNetworkParent != null)
                {
                    await UniTask.WaitUntil(() => _spawnNetworkParent.IsSpawned, cancellationToken: token);
                }

                token.ThrowIfCancellationRequested();
                instance.Spawn();
                _spawnBatch.Track(instance);
                reservationId = 0;
                if (_spawnNetworkParent != null && !instance.TrySetParent(_spawnNetworkParent))
                {
                    Debug.LogWarning($"Failed to parent {instance.name}.", this);
                }

                if (_networkObjectPool != null)
                {
                    return _networkObjectPool.GetDespawnToken(instance.gameObject);
                }

                return instance.destroyCancellationToken;
            }
            finally
            {
                if (reservationId != 0)
                {
                    GridSpawnReservation.ReleaseReservation(reservationId);
                    if (instance != null)
                    {
                        NetworkGameObjectPool.Despawn(instance.gameObject);
                    }
                }
            }
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

            if (_gridSpawnMap != null)
            {
                return TryRelocatePlayersOnGrid();
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
                NetworkPlayerStats stats = player.GetComponent<NetworkPlayerStats>();
                List<BoxCollider> zones = GetPlayerZones(stats);
                if (zones.Count == 0)
                {
                    return false;
                }

                bool found = false;
                Vector3 candidate = default;
                for (int attempt = 0; attempt < MaxIterations * 4; attempt++)
                {
                    if (!TryGetZoneCandidate(zones, out candidate))
                    {
                        continue;
                    }

                    float halfHeight = Mathf.Max(radius, controller.height * player.transform.lossyScale.y / 2f);
                    Vector3 center = candidate + Vector3.Scale(controller.center, player.transform.lossyScale);
                    Vector3 offset = Vector3.up * (halfHeight - radius);
                    bool blocked = false;
                    int blockingMask = _blockingLayerMask | LayerMask.GetMask("Default");
                    foreach (Collider hit in Physics.OverlapCapsule(center - offset, center + offset, radius, blockingMask, QueryTriggerInteraction.Ignore))
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

        private bool TryRelocatePlayersOnGrid()
        {
            GridSpawnReservation.Configure(_gridSpawnMap);
            GridSpawnReservation.ReleaseAllPlayers();
            List<(NetworkPlayerController player, Vector3 position)> assignments = new();
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                {
                    GridSpawnReservation.ReleaseAllPlayers();
                    return false;
                }

                NetworkPlayerController player = client.PlayerObject.GetComponent<NetworkPlayerController>();
                NetworkPlayerStats stats = client.PlayerObject.GetComponent<NetworkPlayerStats>();
                Vector3 position;
                if (player == null || stats == null ||
                    !GridSpawnReservation.TryReservePlayer(client.ClientId, stats.Role, out position))
                {
                    GridSpawnReservation.ReleaseAllPlayers();
                    return false;
                }

                assignments.Add((player, position));
            }

            foreach ((NetworkPlayerController player, Vector3 position) assignment in assignments)
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

            if (_gridSpawnMap != null)
            {
                NetworkPlayerStats gridStats = player.GetComponent<NetworkPlayerStats>();
                Vector3 gridPosition;
                if (gridStats == null || !GridSpawnReservation.TryReservePlayer(
                    player.OwnerClientId, gridStats.Role, out gridPosition))
                {
                    return false;
                }

                player.RelocateForGameRpc(gridPosition);
                return true;
            }

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller == null)
            {
                return false;
            }

            float radius = controller.radius * Mathf.Max(player.transform.lossyScale.x, player.transform.lossyScale.z);
            NetworkPlayerStats stats = player.GetComponent<NetworkPlayerStats>();
            List<BoxCollider> zones = GetPlayerZones(stats);
            if (zones.Count == 0)
            {
                return false;
            }

            for (int attempt = 0; attempt < MaxIterations * 4; attempt++)
            {
                if (!TryGetZoneCandidate(zones, out Vector3 candidate))
                {
                    continue;
                }

                float halfHeight = Mathf.Max(radius, controller.height * player.transform.lossyScale.y / 2f);
                Vector3 center = candidate + Vector3.Scale(controller.center, player.transform.lossyScale);
                Vector3 offset = Vector3.up * (halfHeight - radius);
                bool blocked = false;
                int blockingMask = _blockingLayerMask | LayerMask.GetMask("Default");
                foreach (Collider hit in Physics.OverlapCapsule(
                    center - offset, center + offset, radius, blockingMask, QueryTriggerInteraction.Ignore))
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

        private void OnValidate()
        {
            if (_playerZones == null)
            {
                _playerZones = new();
            }
            if (_collectorPlayerZones == null)
            {
                _collectorPlayerZones = new();
            }
            if (_disruptorPlayerZones == null)
            {
                _disruptorPlayerZones = new();
            }

            if (_isDefaultPlayerSpawner)
            {
                PopulateZones(_playerZones, "PlayerPoints", "PlayerPoiots", "Collector_Points", "Disruptor_Points");
                PopulateZones(_collectorPlayerZones, "Collector_Points");
                PopulateZones(_disruptorPlayerZones, "Disruptor_Points");
                SetTrigger(_playerZones);
                SetTrigger(_collectorPlayerZones);
                SetTrigger(_disruptorPlayerZones);
                return;
            }

        }

        private static void PopulateZones(List<BoxCollider> zones, params string[] names)
        {
            if (zones.Count > 0)
            {
                return;
            }

            foreach (BoxCollider collider in FindObjectsByType<BoxCollider>())
            {
                foreach (string name in names)
                {
                    if (collider.name.StartsWith(name, StringComparison.Ordinal))
                    {
                        zones.Add(collider);
                        break;
                    }
                }
            }
        }

        private static void SetTrigger(List<BoxCollider> zones)
        {
            foreach (BoxCollider zone in zones)
            {
                if (zone != null)
                {
                    zone.isTrigger = true;
                }
            }
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

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client) ||
                client.PlayerObject != null || !_spawnPrefab.TryGetComponent(out NetworkObject prefab))
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
                Vector3 position = await GetSpawnPosition(token);
                token.ThrowIfCancellationRequested();
                if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client) ||
                    client.PlayerObject != null)
                {
                    return;
                }

                NetworkObject instance = Instantiate(prefab, position, Quaternion.identity);
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
            _spawnBatch.Clear();

            if (_poolHandlerRegistered && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.PrefabHandler.RemoveHandler(_spawnPrefab);
            }

            if (_networkObjectPool != null)
            {
                _networkObjectPool.Dispose();
            }
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
