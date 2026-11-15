using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

namespace CoinGame.Utilities
{
    using CoinGame.Network;
    using CoinGame.Player;

    // Kept as a component to preserve Netcode behavior and inherited serialized fields.
    public class NetworkSpawner : BaseSpawner
    {
        private NetworkGameObjectPool _networkObjectPool;
        private NetworkObject _spawnNetworkParent;
        private bool _poolHandlerRegistered;
        private Dictionary<ulong, CancellationTokenSource> _playerSpawnTokens;
        private readonly List<Vector3> _spawnedPositions = new();
        [SerializeField]
        private List<BoxCollider> _playerZones = new();
        [SerializeField]
        private List<BoxCollider> _collectorPlayerZones = new();
        [SerializeField]
        private List<BoxCollider> _disruptorPlayerZones = new();
        private readonly List<int> _zoneRemaining = new();
        private int _lastZoneRound = -1;
        private int _lastZoneCount = -1;

        protected override async UniTask<Vector3> GetSpawnPosition(CancellationToken token)
        {
            if (_isDefaultPlayerSpawner)
            {
                return await GetPlayerSpawnPosition(token);
            }

            return await GetCoinSpawnPosition(token);
        }

        private async UniTask<Vector3> GetCoinSpawnPosition(CancellationToken token)
        {
            List<BoxCollider> zones = ResolveCoinZones();
            while (zones.Count == 0)
            {
                token.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                zones = ResolveCoinZones();
            }
            int round = NetworkRoundState.Instance != null ? NetworkRoundState.Instance.Round : -1;
            if (round != _lastZoneRound || _spawnCount != _lastZoneCount)
            {
                _lastZoneRound = round;
                _lastZoneCount = _spawnCount;
                _spawnedPositions.Clear();
                BuildZoneAllocation(zones, _spawnCount);
            }

            for (int attempt = 0; attempt < MaxIterations; attempt++)
            {
                token.ThrowIfCancellationRequested();
                int zoneIndex = GetNextZoneIndex();
                if (zoneIndex < 0)
                {
                    break;
                }

                Bounds bounds = zones[zoneIndex].bounds;
                Vector3 candidate = new(
                    UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                    bounds.max.y + 20f,
                    UnityEngine.Random.Range(bounds.min.z, bounds.max.z));
                int groundMask = LayerMask.GetMask("Ground", "Default");
                if (!Physics.Raycast(candidate, Vector3.down, out RaycastHit ground, 50f, groundMask))
                {
                    continue;
                }
                candidate = ground.point + Vector3.up * 0.8f;
                if (IsBlocked(candidate))
                {
                    continue;
                }
                if (_spawnedPositions.Any(x => (x - candidate).sqrMagnitude < 2.25f))
                {
                    continue;
                }
                _zoneRemaining[zoneIndex]--;
                _spawnedPositions.Add(candidate);
                return candidate;
            }

            Debug.LogError("Coin zone capacity was insufficient for the requested spawn count.", this);
            throw new InvalidOperationException("Coin spawn zones cannot satisfy the requested spawn count.");
        }

        private bool IsBlocked(Vector3 position)
        {
            int mask = _blockingLayerMask | LayerMask.GetMask("Default", "Ground");
            foreach (Collider hit in Physics.OverlapSphere(position, _checkRadius, mask, QueryTriggerInteraction.Collide))
            {
                if (IsSpawnZone(hit))
                {
                    continue;
                }

                if (hit.gameObject.layer == LayerMask.NameToLayer("Ground"))
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        private bool IsSpawnZone(Collider hit)
        {
            if (SpawnZoneRegistry.Instance != null)
            {
                return ContainsZone(SpawnZoneRegistry.Instance.CoinZones, hit) ||
                    ContainsZone(SpawnZoneRegistry.Instance.CrateZones, hit) ||
                    ContainsZone(SpawnZoneRegistry.Instance.PlayerZones, hit) ||
                    ContainsZone(SpawnZoneRegistry.Instance.CollectorPlayerZones, hit) ||
                    ContainsZone(SpawnZoneRegistry.Instance.DisruptorPlayerZones, hit);
            }

            return hit.name.StartsWith("CoinZone", StringComparison.Ordinal) ||
                hit.name.StartsWith("CrateZone", StringComparison.Ordinal) ||
                hit.name.EndsWith("_Points", StringComparison.Ordinal);
        }

        private static bool ContainsZone(List<BoxCollider> zones, Collider hit)
        {
            foreach (BoxCollider zone in zones)
            {
                if (zone == hit)
                {
                    return true;
                }
            }

            return false;
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

        private void BuildZoneAllocation(List<BoxCollider> zones, int count)
        {
            _zoneRemaining.Clear();
            if (zones.Count == 0)
            {
                return;
            }

            float totalArea = 0f;
            foreach (BoxCollider zone in zones)
            {
                totalArea += Mathf.Max(0.01f, zone.bounds.size.x * zone.bounds.size.z);
            }

            int assigned = 0;
            List<float> remainders = new();
            foreach (BoxCollider zone in zones)
            {
                float area = Mathf.Max(0.01f, zone.bounds.size.x * zone.bounds.size.z);
                float exact = count * area / totalArea;
                int quota = Mathf.FloorToInt(exact);
                _zoneRemaining.Add(quota);
                remainders.Add(exact - quota);
                assigned += quota;
            }

            while (assigned < count)
            {
                int bestIndex = 0;
                for (int i = 1; i < remainders.Count; i++)
                {
                    if (remainders[i] > remainders[bestIndex])
                    {
                        bestIndex = i;
                    }
                }

                _zoneRemaining[bestIndex]++;
                remainders[bestIndex] = -1f;
                assigned++;
            }
        }

        private int GetNextZoneIndex()
        {
            if (_zoneRemaining.Count == 0)
            {
                return -1;
            }

            int start = UnityEngine.Random.Range(0, _zoneRemaining.Count);
            for (int offset = 0; offset < _zoneRemaining.Count; offset++)
            {
                int index = (start + offset) % _zoneRemaining.Count;
                if (_zoneRemaining[index] > 0)
                {
                    return index;
                }
            }

            return -1;
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

        private List<BoxCollider> ResolveCoinZones()
        {
            if (_spawnZones == null)
            {
                _spawnZones = new();
            }

            _spawnZones.RemoveAll(zone => zone == null);
            if (_spawnZones.Count > 0)
            {
                return _spawnZones;
            }

            return SpawnZoneRegistry.Instance != null
                ? SpawnZoneRegistry.Instance.CoinZones
                : _spawnZones;
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
            if (_spawnZones == null)
            {
                _spawnZones = new();
            }
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

            PopulateZones(_spawnZones, "CoinZone");
            SetTrigger(_spawnZones);
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
