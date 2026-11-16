using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CoinGame.Network
{
    using CoinGame.Utilities;

    using CoinGame.Stage;

    public sealed class NetworkCrateSpawner : MonoBehaviour
    {
        [SerializeField] private NetworkCrate _cratePrefab;
        [SerializeField] private StageConfig _stageConfig;
        [SerializeField] private GridSpawnMap _gridSpawnMap;
        [SerializeField] private Vector3 _area = new(8f, 0f, 8f);
        [SerializeField] private List<BoxCollider> _spawnZones = new();
        [SerializeField] private LayerMask _blockingLayerMask;
        [SerializeField] private float _checkRadius = 1.2f;
        private NetworkRoundState _roundState;
        private int _lastSpawnedRound = -1;
        private readonly List<int> _zoneRemaining = new();
        private readonly List<Vector3> _spawnedPositions = new();
        private IEnumerator Start()
        {
            while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                yield return null;
            }
            if (!NetworkManager.Singleton.IsServer)
            {
                yield break;
            }

            while (NetworkRoundState.Instance == null || !NetworkRoundState.Instance.IsSpawned)
            {
                yield return null;
            }

            _roundState = NetworkRoundState.Instance;
            _roundState.StateChanged += OnRoundStateChanged;
            SpawnForRoundServer();
        }

        private void OnDestroy()
        {
            if (_roundState != null)
            {
                _roundState.StateChanged -= OnRoundStateChanged;
            }
        }

        private void OnRoundStateChanged(int round, int targetCoins, int remainingCoins, float remainingTime, bool isActive)
        {
            if (round <= 0 || round == _lastSpawnedRound)
            {
                return;
            }

            SpawnForRoundServer();
        }
        public void SpawnForRoundServer()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer ||
                _cratePrefab == null || _stageConfig == null)
            {
                return;
            }

            int round = NetworkRoundState.Instance != null ? NetworkRoundState.Instance.Round : 0;
            if (round <= 0 || round == _lastSpawnedRound)
            {
                return;
            }

            DespawnExistingCratesServer();
            int count = _stageConfig.GetCrateCount();
            List<BoxCollider> zones = ResolveCrateZones();
            if (zones.Count == 0)
            {
                Debug.LogError("Crate spawn zones are not assigned.", this);
                return;
            }

            _lastSpawnedRound = round;
            BuildZoneAllocation(zones, count);
            for (int i = 0; i < count; i++)
            {
                if (!TryGetPositionServer(out Vector3 position))
                {
                    Debug.LogError("Crate spawn zones cannot satisfy the requested spawn count.", this);
                    return;
                }

                NetworkCrate crate = Instantiate(_cratePrefab, position, Quaternion.identity);
                crate.NetworkObject.Spawn();
            }
        }

        private void DespawnExistingCratesServer()
        {
            foreach (NetworkCrate crate in FindObjectsByType<NetworkCrate>())
            {
                if (crate != null && crate.NetworkObject != null && crate.NetworkObject.IsSpawned)
                {
                    crate.NetworkObject.Despawn(true);
                }
            }
        }

        private bool TryGetPositionServer(out Vector3 position)
        {
            position = default;
            List<BoxCollider> zones = ResolveCrateZones();
            if (zones.Count == 0)
            {
                return false;
            }

            for (int attempt = 0; attempt < 50; attempt++)
            {
                int zoneIndex = GetNextZoneIndex();
                if (zoneIndex < 0)
                {
                    break;
                }

                Bounds bounds = zones[zoneIndex].bounds;
                Vector3 candidate = new(Random.Range(bounds.min.x, bounds.max.x), bounds.max.y + 20f,
                    Random.Range(bounds.min.z, bounds.max.z));
                if (Physics.Raycast(candidate, Vector3.down, out RaycastHit ground, 50f, LayerMask.GetMask("Ground", "Default")))
                {
                    Vector3 candidatePosition = ground.point + Vector3.up * 0.8f;
                    if (IsBlocked(candidatePosition))
                    {
                        continue;
                    }

                    _zoneRemaining[zoneIndex]--;
                    position = candidatePosition;
                    _spawnedPositions.Add(candidatePosition);
                    return true;
                }
            }

            return false;
        }

        private bool IsBlocked(Vector3 position)
        {
            int mask = _blockingLayerMask | LayerMask.GetMask("Default", "Grass", "Environment", "Ground");
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

            foreach (Vector3 spawnedPosition in _spawnedPositions)
            {
                if ((spawnedPosition - position).sqrMagnitude < _checkRadius * _checkRadius * 4f)
                {
                    return true;
                }
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

            return hit.name.StartsWith("CoinZone", System.StringComparison.Ordinal) ||
                hit.name.StartsWith("CrateZone", System.StringComparison.Ordinal) ||
                hit.name.EndsWith("_Points", System.StringComparison.Ordinal);
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

        private void BuildZoneAllocation(List<BoxCollider> zones, int count)
        {
            _zoneRemaining.Clear();
            _spawnedPositions.Clear();
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

            int start = Random.Range(0, _zoneRemaining.Count);
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

        private void OnValidate()
        {
            if (_spawnZones.Count == 0)
            {
                foreach (BoxCollider collider in FindObjectsByType<BoxCollider>())
                {
                    if (collider.name.StartsWith("CrateZone", System.StringComparison.Ordinal))
                    {
                        _spawnZones.Add(collider);
                    }
                }
            }

            foreach (BoxCollider zone in _spawnZones)
            {
                if (zone != null)
                {
                    zone.isTrigger = true;
                }
            }
        }

        private List<BoxCollider> ResolveCrateZones()
        {
            if (_spawnZones.Count > 0)
            {
                return _spawnZones;
            }

            return SpawnZoneRegistry.Instance != null
                ? SpawnZoneRegistry.Instance.CrateZones
                : _spawnZones;
        }
    }
}
