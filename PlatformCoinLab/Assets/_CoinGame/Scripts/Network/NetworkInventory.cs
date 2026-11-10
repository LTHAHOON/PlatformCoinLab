using System;
using Unity.Netcode;
using UnityEngine;
using CoinGame.Gameplay.Combat;

namespace CoinGame.Network
{
    public class NetworkInventory : NetworkBehaviour
    {
        private readonly NetworkVariable<byte> _slot0 = new((byte)WeaponKind.None);
        private readonly NetworkVariable<byte> _slot1 = new((byte)WeaponKind.None);
        private readonly NetworkVariable<byte> _current = new(0);
        private readonly NetworkVariable<int> _magazine = new(0);
        private readonly NetworkVariable<int> _reserve = new(0);
        private readonly NetworkVariable<bool> _boost = new(false);
        private readonly NetworkVariable<bool> _revive = new(false);
        public WeaponKind CurrentKind => (WeaponKind)( _current.Value == 0 ? _slot0.Value : _slot1.Value);
        public int Magazine => _magazine.Value;
        public int ReserveAmmo => _reserve.Value;
        public bool HasBoost => _boost.Value;
        public bool HasRevive => _revive.Value;
        public event Action Changed;
        private int _lastSignature = int.MinValue;

        private void Update()
        {
            int signature = ((int)_slot0.Value << 24) ^ ((int)_slot1.Value << 16) ^ (_current.Value << 8) ^ _magazine.Value ^ _reserve.Value;
            signature ^= _boost.Value ? 17 : 0;
            signature ^= _revive.Value ? 31 : 0;
            if (signature == _lastSignature) return;
            _lastSignature = signature;
            Changed?.Invoke();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer && CurrentKind == WeaponKind.None)
            {
                _slot0.Value = (byte)WeaponKind.Gun;
                _magazine.Value = 12;
                _reserve.Value = 60;
            }
        }

        public void SetWeaponServer(WeaponKind kind)
        {
            if (!IsServer || kind == WeaponKind.None) return;
            if ((WeaponKind)_slot0.Value == kind || (WeaponKind)_slot1.Value == kind) return;
            if ((WeaponKind)_slot0.Value == WeaponKind.None) _slot0.Value = (byte)kind;
            else if ((WeaponKind)_slot1.Value == WeaponKind.None) _slot1.Value = (byte)kind;
        }

        public void SwitchServer(int delta)
        {
            if (!IsServer) return;
            int next = (_current.Value + (delta > 0 ? 1 : 255)) % 2;
            if ((WeaponKind)(next == 0 ? _slot0.Value : _slot1.Value) != WeaponKind.None) _current.Value = (byte)next;
        }

        public bool TryConsumeAmmoServer()
        {
            if (!IsServer || CurrentKind != WeaponKind.Gun || _magazine.Value <= 0) return false;
            _magazine.Value--;
            return true;
        }

        public void ReloadServer(int size)
        {
            if (!IsServer || CurrentKind != WeaponKind.Gun) return;
            int need = Mathf.Max(0, size - _magazine.Value);
            int add = Mathf.Min(need, _reserve.Value);
            _magazine.Value += add;
            _reserve.Value -= add;
        }

        public void AddAmmoServer(int amount) { if (IsServer) _reserve.Value = Mathf.Clamp(_reserve.Value + amount, 0, 60); }
        public bool GiveBoostServer() { if (!IsServer || _boost.Value) return false; _boost.Value = true; return true; }
        public bool ConsumeBoostServer() { if (!IsServer || !_boost.Value) return false; _boost.Value = false; return true; }
        public bool GiveReviveServer() { if (!IsServer || _revive.Value) return false; _revive.Value = true; return true; }
        public bool ConsumeReviveServer() { if (!IsServer || !_revive.Value) return false; _revive.Value = false; return true; }
    }
}
