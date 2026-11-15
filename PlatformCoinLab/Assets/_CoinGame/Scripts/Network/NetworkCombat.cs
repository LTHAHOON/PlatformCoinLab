using Unity.Netcode;
using UnityEngine;

namespace CoinGame.Network
{
    using CoinGame.Gameplay.Combat;

    [RequireComponent(typeof(NetworkInventory), typeof(NetworkPlayerStats))]
    public class NetworkCombat : NetworkBehaviour
    {
        [SerializeField]
        private WeaponData _gun;
        [SerializeField]
        private WeaponData _fist;
        [SerializeField]
        private WeaponData _sword;
        [SerializeField]
        private Transform _weaponSocket;
        [SerializeField]
        private Transform _muzzle;
        [SerializeField]
        private GameObject _gunVisualPrefab;
        [SerializeField]
        private GameObject _swordVisualPrefab;
        [SerializeField]
        private NetworkProjectile _projectilePrefab;
        private NetworkInventory _inventory;
        private NetworkPlayerStats _stats;
        private GameObject _visual;
        private WeaponKind _visualKind;
        private double _nextAttack;
        private readonly NetworkVariable<int> _attackVersion = new(0);
        private readonly NetworkVariable<int> _reloadVersion = new(0);
        public Vector3 AimDirection { get; set; } = Vector3.forward;
        private void Awake()
        {
            _inventory = GetComponent<NetworkInventory>();
            _stats = GetComponent<NetworkPlayerStats>();
            if (_gun == null)
            {
                _gun = CreateDefault(WeaponKind.Gun, 12, 0.3f, 12f, 14f, 12, 60, 1f);
            }

            if (_fist == null)
            {
                _fist = CreateDefault(WeaponKind.Fist, 8, 0.45f, 3f, 0f, 0, 0, 1f);
            }

            if (_sword == null)
            {
                _sword = CreateDefault(WeaponKind.Sword, 20, 0.6f, 1.5f, 0f, 0, 0, 2f);
            }
        }
        public override void OnNetworkSpawn()
        {
            RefreshVisual();
        }
        private void Update()
        {
            if (IsSpawned && _visualKind != _inventory.CurrentKind)
            {
                RefreshVisual();
            }
        }
        private void RefreshVisual()
        {
            if (_visual != null)
            {
                Destroy(_visual);
            }

            _visualKind = _inventory.CurrentKind;
            if (_weaponSocket == null)
            {
                return;
            }

            if (_visualKind == WeaponKind.Fist)
            {
                return;
            }

            GameObject prefab = _visualKind == WeaponKind.Gun ? _gunVisualPrefab : _swordVisualPrefab;
            if (prefab == null)
            {
                return;
            }
            _visual = Instantiate(prefab, _weaponSocket);
            WeaponData data = _visualKind == WeaponKind.Gun ? _gun : _sword;
            _visual.transform.localPosition = data != null ? data.HandOffset : Vector3.zero;
            _visual.transform.localRotation = data != null ? Quaternion.Euler(data.HandRotation) : Quaternion.identity;
            _visual.transform.localScale = data != null ? data.HandScale : Vector3.one;
            if (_muzzle != null && data != null)
            {
                _muzzle.localPosition = data.MuzzleOffset;
            }
        }
        private WeaponData CreateDefault(WeaponKind kind, int damage, float interval, float range, float speed, int magazine, int reserve, float crate)
        {
            WeaponData data = ScriptableObject.CreateInstance<WeaponData>();
            data.Kind = kind;
            data.Damage = damage;
            data.AttackInterval = interval;
            data.Range = range;
            data.ProjectileSpeed = speed;
            data.MagazineSize = magazine;
            data.ReserveAmmo = reserve;
            data.CrateDamage = crate;
            return data;
        }
        public void RequestAttack()
        {
            if (!IsOwner)
            {
                return;
            }

            AttackRpc(AimDirection);
        }

        public void RequestReload()
        {
            if (!IsOwner)
            {
                return;
            }

            ReloadRpc();
        }

        public void RequestSwitch(int delta)
        {
            if (!IsOwner)
            {
                return;
            }

            SwitchRpc(delta);
        }

        public void RequestSelectWeapon(WeaponKind kind)
        {
            if (!IsOwner)
            {
                return;
            }

            SelectWeaponRpc(kind);
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void AttackRpc(Vector3 direction)
        {
            if (!IsServer || _stats == null || !_stats.IsAlive || _stats.IsInvulnerable ||
                NetworkManager.Singleton.ServerTime.Time < _nextAttack)
            {
                return;
            }

            WeaponKind kind = _inventory.CurrentKind;
            WeaponData data = GetWeaponData(kind);
            if (data == null)
            {
                return;
            }

            _nextAttack = NetworkManager.Singleton.ServerTime.Time + data.AttackInterval;
            if (kind == WeaponKind.Gun && !_inventory.TryConsumeAmmoServer())
            {
                return;
            }

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = transform.forward;
            }

            direction.Normalize();
            if (kind == WeaponKind.Sword || kind == WeaponKind.Fist)
            {
                HitMelee(direction, data, kind, GetPlayerDamage(kind, data));
            }
            else
            {
                HitGun(direction, data);
            }
            _attackVersion.Value++;
            _stats.ClearStealthServer();
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] private void ReloadRpc() { _inventory.ReloadServer(_gun != null ? _gun.MagazineSize : 12); _reloadVersion.Value++; }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] private void SwitchRpc(int delta) { _inventory.SwitchServer(delta); }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] private void SelectWeaponRpc(WeaponKind kind) { _inventory.SelectWeaponServer(kind); }
        private void HitGun(Vector3 direction, WeaponData data)
        {
            if (_projectilePrefab != null)
            {
                Vector3 origin = _muzzle != null ? _muzzle.position : transform.position + Vector3.up;
                NetworkProjectile projectile = Instantiate(_projectilePrefab, origin, Quaternion.LookRotation(direction));
                projectile.NetworkObject.Spawn();
                projectile.InitializeServer(direction, data.ProjectileSpeed, data.Range, data.Damage,
                    GetCrateDamage(data), OwnerClientId);
                return;
            }
            if (Physics.Raycast(transform.position + Vector3.up, direction, out RaycastHit hit, data.Range))
            {
                ResolveHit(hit.collider, data.Damage, GetCrateDamage(data), direction);
            }
        }
        private void HitMelee(Vector3 direction, WeaponData data, WeaponKind kind, int playerDamage)
        {
            if (TryHitCrateRaycast(direction, data))
            {
                return;
            }

            foreach (Collider hit in Physics.OverlapSphere(transform.position + direction * data.Range * 0.5f, data.Range))
            {
                Vector3 to = hit.transform.position - transform.position; to.y = 0f;
                if (to.sqrMagnitude > data.Range * data.Range ||
                    Vector3.Angle(direction, to) > data.ArcAngle * 0.5f)
                {
                    continue;
                }
                ResolveHit(hit, playerDamage, GetCrateDamage(data), direction);
            }
        }

        private bool TryHitCrateRaycast(Vector3 direction, WeaponData data)
        {
            Vector3 origin = transform.position + Vector3.up * 0.8f;
            if (!Physics.Raycast(origin, direction, out RaycastHit hit, data.Range,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (!hit.collider.TryGetComponent(out NetworkCrate crate))
            {
                return false;
            }

            crate.ApplyWeaponHitServer(GetCrateDamage(data), OwnerClientId);
            return true;
        }

        private WeaponData GetWeaponData(WeaponKind kind)
        {
            if (kind == WeaponKind.Fist)
            {
                return _fist;
            }

            return kind == WeaponKind.Sword ? _sword : _gun;
        }
        private void ResolveHit(Collider hit, int damage, int crateDamage, Vector3 direction)
        {
            if (hit.TryGetComponent(out NetworkPlayerStats target))
            {
                if (target == _stats || target.Role == _stats.Role)
                {
                    return;
                }
                target.ApplyDamageServer(new DamageInfo(OwnerClientId, damage, DamageType.Weapon, direction * 2f));
            }
            if (hit.TryGetComponent(out NetworkCrate crate))
            {
                crate.ApplyWeaponHitServer(crateDamage, OwnerClientId);
            }
        }

        private int GetPlayerDamage(WeaponKind kind, WeaponData data)
        {
            if (kind == WeaponKind.Fist && _inventory != null)
            {
                return _inventory.GetBasicDamage(_stats.Role);
            }

            return data.Damage;
        }

        private int GetCrateDamage(WeaponData data)
        {
            return Mathf.Max(1, Mathf.RoundToInt(data.CrateDamage));
        }

        public int AttackVersion => _attackVersion.Value;
        public int ReloadVersion => _reloadVersion.Value;

    }
}
