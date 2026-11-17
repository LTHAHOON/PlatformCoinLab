using System.Collections.Generic;
using Unity.Netcode;

namespace CoinGame.Utilities
{
    public sealed class NetworkSpawnBatch
    {
        private readonly List<NetworkObject> _instances = new();
        public void Track(NetworkObject instance)
        {
            _instances.Add(instance);
        }

        public void Clear()
        {
            foreach (NetworkObject instance in _instances)
            {
                if (instance != null && instance.IsSpawned)
                {
                    NetworkGameObjectPool.Despawn(instance.gameObject);
                }
            }

            _instances.Clear();
        }
    }
}
