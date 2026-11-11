using Unity.Netcode;
using UnityEngine;
using CoinGame.Gameplay.Items;
using CoinGame.Utilities;

namespace CoinGame.Network
{
    public class NetworkItemPickup : NetworkBehaviour
    {
        [SerializeField] private ItemData _data;
        public void InitializeServer(ItemData data) 
        { 
            if (IsServer) _data = data;
        }

        public override void OnNetworkSpawn()
        {
            if (_data == null || !TryGetComponent(out SphereCollider pickup)) return;
            float scale = Mathf.Max(0.01f, _data.DropScale);
            transform.localScale *= scale;
            pickup.radius = _data.PickupRadius / scale;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || _data == null || !other.TryGetComponent(out NetworkPlayerStats stats)) return;
            if (stats.TryAcquireItemServer(_data)) NetworkGameObjectPool.Despawn(gameObject);
        }
    }
}
