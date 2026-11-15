using System;
using Unity.Netcode;
using UnityEngine;

namespace CoinGame.Network
{
    using CoinGame.Gameplay.Combat;
    using CoinGame.Player;
    using CoinGame.Gameplay.Player;

    public class NetworkInventory : NetworkBehaviour
    {
        [SerializeField]
        private RoleLoadoutData _roleLoadout;
        private readonly NetworkVariable<byte> _current = new((byte)WeaponKind.Fist);
        private readonly NetworkVariable<bool> _hasGun = new(false);
        private readonly NetworkVariable<bool> _hasSword = new(false);
        private readonly NetworkVariable<int> _magazine = new(0);
        private readonly NetworkVariable<int> _reserve = new(0);
        private readonly NetworkVariable<bool> _boost = new(false);
        private readonly NetworkVariable<bool> _revive = new(false);
        public event Action Changed;
        private int _lastSignature = int.MinValue;

        private void Update()
        {
            int signature = (_current.Value << 24) ^ (_magazine.Value << 8) ^ _reserve.Value;
            signature ^= _hasGun.Value ? 43 : 0;
            signature ^= _hasSword.Value ? 47 : 0;
            signature ^= _boost.Value ? 17 : 0;
            signature ^= _revive.Value ? 31 : 0;
            if (signature == _lastSignature) return;
            _lastSignature = signature;
            if (Changed != null)
            {
                Changed.Invoke();
            }
        }

        public override void OnNetworkSpawn()
        {
        }

        public void SetRoleLoadoutServer(PlayerRole role)
        {
            if (!IsServer)
            {
                return;
            }

            WeaponKind startingWeapon = role == PlayerRole.Disruptor
                ? (_roleLoadout != null ? _roleLoadout.DisruptorStartingWeapon : WeaponKind.Fist)
                : (_roleLoadout != null ? _roleLoadout.CollectorStartingWeapon : WeaponKind.Fist);
            _hasGun.Value = false;
            _hasSword.Value = false;
            _current.Value = (byte)WeaponKind.Fist;
            _magazine.Value = 0;
            _reserve.Value = 0;
        }

        public void SetWeaponServer(WeaponKind kind)
        {
            if (!IsServer)
            {
                return;
            }

            if (kind == WeaponKind.Gun)
            {
                _hasGun.Value = true;
                _current.Value = (byte)WeaponKind.Gun;
                _magazine.Value = 12;
                _reserve.Value = 60;
                return;
            }

            if (kind == WeaponKind.Sword)
            {
                _hasSword.Value = true;
                _current.Value = (byte)WeaponKind.Sword;
            }
        }

        public void SelectWeaponServer(WeaponKind kind)
        {
            if (!IsServer)
            {
                return;
            }

            if (kind == WeaponKind.Fist ||
                kind == WeaponKind.Gun && _hasGun.Value ||
                kind == WeaponKind.Sword && _hasSword.Value)
            {
                _current.Value = (byte)kind;
            }
        }

        public void ResetRoundWeaponsServer()
        {
            if (!IsServer)
            {
                return;
            }

            _current.Value = (byte)WeaponKind.Fist;
            if (_hasGun.Value)
            {
                _magazine.Value = 12;
                _reserve.Value = 60;
            }
            else
            {
                _magazine.Value = 0;
                _reserve.Value = 0;
            }
        }

        public void SwitchServer(int delta)
        {
            if (!IsServer)
            {
                return;
            }

            if (delta > 0)
            {
                if (CurrentKind == WeaponKind.Fist && _hasGun.Value)
                {
                    _current.Value = (byte)WeaponKind.Gun;
                }
                else if (_hasSword.Value)
                {
                    _current.Value = (byte)WeaponKind.Sword;
                }
                else
                {
                    _current.Value = (byte)WeaponKind.Fist;
                }
                return;
            }

            if (CurrentKind == WeaponKind.Sword && _hasGun.Value)
            {
                _current.Value = (byte)WeaponKind.Gun;
            }
            else
            {
                _current.Value = (byte)WeaponKind.Fist;
            }
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

        public WeaponKind CurrentKind => (WeaponKind)_current.Value;
        public int Magazine => _magazine.Value;
        public int ReserveAmmo => _reserve.Value;
        public bool HasBoost => _boost.Value;
        public bool HasRevive => _revive.Value;
        public bool HasGun => _hasGun.Value;
        public bool HasSword => _hasSword.Value;
        public int GetBasicDamage(PlayerRole role)
        {
            if (_roleLoadout == null)
            {
                return role == PlayerRole.Disruptor ? 12 : 8;
            }

            return role == PlayerRole.Disruptor
                ? _roleLoadout.DisruptorBasicDamage
                : _roleLoadout.CollectorBasicDamage;
        }

    }
}
