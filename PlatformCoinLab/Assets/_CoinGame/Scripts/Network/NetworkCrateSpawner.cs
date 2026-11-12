using System.Collections;
using Unity.Netcode;
using UnityEngine;
using CoinGame.Stage;
using System.Linq;

namespace CoinGame.Network
{
    public sealed class NetworkCrateSpawner : MonoBehaviour
    {
        [SerializeField] private NetworkCrate _cratePrefab;
        [SerializeField] private StageConfig _stageConfig;
        [SerializeField] private Vector3 _area = new(8f, 0f, 8f);
        private bool _spawned;
        private readonly System.Collections.Generic.List<Collider> _zones = new();
        private IEnumerator Start()
        {
            while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                yield return null;
            }
            if (NetworkManager.Singleton.IsServer && !_spawned)
            {
                _spawned = true;
                SpawnForRoundServer();
            }
        }
        public void SpawnForRoundServer()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer ||
                _cratePrefab == null || _stageConfig == null)
            {
                return;
            }
            int count = _stageConfig.GetCrateCount();
            for (int i = 0; i < count; i++)
            {
                Vector3 position = GetPositionServer(i);
                NetworkCrate crate = Instantiate(_cratePrefab, position, Quaternion.identity);
                crate.NetworkObject.Spawn();
            }
        }

        private Vector3 GetPositionServer(int index)
        {
            if (_zones.Count == 0)
            {
                _zones.AddRange(FindObjectsByType<BoxCollider>()
                    .Where(x => x.name.StartsWith("CrateZone", System.StringComparison.Ordinal)));
            }
            if (_zones.Count == 0)
            {
                return transform.position + new Vector3(Random.Range(-_area.x, _area.x), 0.8f, Random.Range(-_area.z, _area.z));
            }
            for (int attempt = 0; attempt < 50; attempt++)
            {
                Bounds bounds = _zones[(index + attempt) % _zones.Count].bounds;
                Vector3 candidate = new(Random.Range(bounds.min.x, bounds.max.x), bounds.max.y + 20f,
                    Random.Range(bounds.min.z, bounds.max.z));
                if (Physics.Raycast(candidate, Vector3.down, out RaycastHit ground, 50f, LayerMask.GetMask("Ground", "Default")))
                {
                    return ground.point + Vector3.up * 0.8f;
                }
            }
            Debug.LogWarning("Crate zone spawn retries exceeded.", this);
            return _zones[0].bounds.center + Vector3.up * 0.8f;
        }
    }
}
