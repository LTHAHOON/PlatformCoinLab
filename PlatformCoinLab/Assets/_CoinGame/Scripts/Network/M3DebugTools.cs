using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using CoinGame.Gameplay.Combat;

namespace CoinGame.Network
{
    public class M3DebugTools : NetworkBehaviour
    {
        [SerializeField] private bool _enabledInDevelopment = true;
        private NetworkInventory _inventory;
        private NetworkPlayerStats _stats;
        private void Awake() { _inventory = GetComponent<NetworkInventory>(); _stats = GetComponent<NetworkPlayerStats>(); }
        private void Update()
        {
            if (!IsOwner || !_enabledInDevelopment || !Debug.isDebugBuild || Keyboard.current == null) return;
            if (Keyboard.current.f1Key.wasPressedThisFrame) GiftRpc(1);
            if (Keyboard.current.f2Key.wasPressedThisFrame) GiftRpc(2);
            if (Keyboard.current.f3Key.wasPressedThisFrame) GiftRpc(3);
            if (Keyboard.current.f4Key.wasPressedThisFrame) GiftRpc(4);
            if (Keyboard.current.f5Key.wasPressedThisFrame) GiftRpc(5);
            if (Keyboard.current.f6Key.wasPressedThisFrame) GiftRpc(6);
            if (Keyboard.current.f7Key.wasPressedThisFrame) GiftRpc(7);
            if (Keyboard.current.f8Key.wasPressedThisFrame) GiftRpc(8);
            if (Keyboard.current.f9Key.wasPressedThisFrame) GiftRpc(9);
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void GiftRpc(int gift)
        {
            if (!IsServer || _inventory == null || _stats == null) return;
            switch (gift)
            {
                case 1: _inventory.SetWeaponServer(WeaponKind.Gun); break;
                case 2: _inventory.SetWeaponServer(WeaponKind.Sword); break;
                case 3: _inventory.AddAmmoServer(24); break;
                case 4: _inventory.GiveBoostServer(); break;
                case 5: _inventory.GiveReviveServer(); break;
                case 6: _stats.SetInvulnerableServer(NetworkManager.Singleton.ServerTime.Time + 4d); break;
                case 7: _stats.SetStealthServer(NetworkManager.Singleton.ServerTime.Time + 5d); break;
                case 8: _stats.RestoreHealthServer(30); break;
                case 9: _stats.GiveRandomDebugServer(); break;
            }
        }
    }
}
