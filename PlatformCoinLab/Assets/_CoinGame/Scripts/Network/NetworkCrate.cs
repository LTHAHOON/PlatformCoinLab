using Unity.Netcode;
using UnityEngine;
using CoinGame.Gameplay.Combat;
using CoinGame.Gameplay.Items;
using CoinGame.Player;

namespace CoinGame.Network
{
    public class NetworkCrate : NetworkBehaviour
    {
        [SerializeField] private int _maxHealth = 30;
        [SerializeField] private NetworkItemPickup _pickupPrefab;
        [SerializeField] private ItemData[] _dropTable;
        private readonly NetworkVariable<int> _health = new(30);
        public void ApplyWeaponHitServer(WeaponKind kind, int damage, ulong attacker)
        {
            if (!IsServer || _health.Value <= 0) return;
            _health.Value -= Mathf.Max(1, kind == WeaponKind.Sword ? damage * 2 : damage);
            if (_health.Value <= 0) DropServer();
        }
        public override void OnNetworkSpawn() { if (IsServer) _health.Value = _maxHealth; }
        private void DropServer()
        {
            if (_dropTable != null && _dropTable.Length > 0)
            {
                int total = 0;
                foreach (ItemData entry in _dropTable)
                {
                    if (entry != null && CanDropItemServer(entry)) total += Mathf.Max(0, entry.Weight);
                }
                int roll = Random.Range(0, Mathf.Max(1, total));
                ItemData item = _dropTable[0];
                foreach (ItemData entry in _dropTable)
                {
                    if (entry == null || !CanDropItemServer(entry)) continue;
                    roll -= Mathf.Max(0, entry.Weight);
                    if (roll < 0) { item = entry; break; }
                }
                if (item != null && _pickupPrefab != null)
                {
                    NetworkItemPickup pickup = Instantiate(_pickupPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                    pickup.InitializeServer(item);
                    pickup.NetworkObject.Spawn();
                }
            }
            NetworkObject.Despawn();
        }

        private bool CanDropItemServer(ItemData item)
        {
            if (item.Kind != ItemKind.Boost && item.Kind != ItemKind.Revive) return true;
            bool sawCollector = false;
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                NetworkPlayerStats stats = client.PlayerObject != null ? client.PlayerObject.GetComponent<NetworkPlayerStats>() : null;
                NetworkInventory inventory = client.PlayerObject != null ? client.PlayerObject.GetComponent<NetworkInventory>() : null;
                if (stats == null || inventory == null || stats.Role != PlayerRole.Collector) continue;
                sawCollector = true;
                if (item.Kind == ItemKind.Boost && !inventory.HasBoost || item.Kind == ItemKind.Revive && !inventory.HasRevive) return true;
            }
            return !sawCollector;
        }
    }
}
