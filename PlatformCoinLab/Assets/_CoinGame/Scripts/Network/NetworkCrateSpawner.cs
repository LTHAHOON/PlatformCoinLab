using System.Collections;
using Unity.Netcode;
using UnityEngine;
using CoinGame.Stage;

namespace CoinGame.Network
{
    public class NetworkCrateSpawner : MonoBehaviour
    {
        [SerializeField] private NetworkCrate _cratePrefab;
        [SerializeField] private StageConfig _stageConfig;
        [SerializeField] private Vector3 _area = new(8f, 0f, 8f);
        private bool _spawned;
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
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || _cratePrefab == null || _stageConfig == null) return;
            int count = _stageConfig.GetCrateCount();
            for (int i = 0; i < count; i++)
            {
                Vector3 position = transform.position + new Vector3(Random.Range(-_area.x, _area.x), 0f, Random.Range(-_area.z, _area.z));
                NetworkCrate crate = Instantiate(_cratePrefab, position, Quaternion.identity);
                crate.NetworkObject.Spawn();
            }
        }
    }
}
