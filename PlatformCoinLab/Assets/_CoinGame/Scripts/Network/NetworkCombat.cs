using Unity.Netcode;
using UnityEngine;
using CoinGame.Gameplay.Combat;

namespace CoinGame.Network
{
    [RequireComponent(typeof(NetworkInventory), typeof(NetworkPlayerStats))]
    public class NetworkCombat : NetworkBehaviour
    {
        [SerializeField] private WeaponData _gun;
        [SerializeField] private WeaponData _sword;
        [SerializeField] private Transform _weaponSocket;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private GameObject _gunVisualPrefab;
        [SerializeField] private GameObject _swordVisualPrefab;
        [SerializeField] private NetworkProjectile _projectilePrefab;
        private NetworkInventory _inventory;
        private NetworkPlayerStats _stats;
        private GameObject _visual;
        private WeaponKind _visualKind;
        private double _nextAttack;
        public Vector3 AimDirection { get; set; } = Vector3.forward;
        private void Awake()
        {
            _inventory = GetComponent<NetworkInventory>();
            _stats = GetComponent<NetworkPlayerStats>();
            if (_gun == null) _gun = CreateDefault(WeaponKind.Gun, 12, 0.3f, 12f, 14f, 12, 60, 1f);
            if (_sword == null) _sword = CreateDefault(WeaponKind.Sword, 20, 0.6f, 1.5f, 0f, 0, 0, 2f);
        }
        public override void OnNetworkSpawn()
        {
            RefreshVisual();
        }
        private void Update()
        {
            if (IsSpawned && _visualKind != _inventory.CurrentKind) RefreshVisual();
        }
        private void RefreshVisual()
        {
            if (_visual != null) Destroy(_visual);
            _visualKind = _inventory.CurrentKind;
            if (_weaponSocket == null || _visualKind == WeaponKind.None) return;
            GameObject prefab = _visualKind == WeaponKind.Gun ? _gunVisualPrefab : _swordVisualPrefab;
            if (prefab == null) return;
            _visual = Instantiate(prefab, _weaponSocket);
            WeaponData data = _visualKind == WeaponKind.Gun ? _gun : _sword;
            _visual.transform.localPosition = data != null ? data.HandOffset : Vector3.zero;
            _visual.transform.localRotation = data != null ? Quaternion.Euler(data.HandRotation) : Quaternion.identity;
            _visual.transform.localScale = data != null ? data.HandScale : Vector3.one;
            if (_muzzle != null && data != null) _muzzle.localPosition = data.MuzzleOffset;
        }
        private WeaponData CreateDefault(WeaponKind kind, int damage, float interval, float range, float speed, int magazine, int reserve, float crate)
        {
            WeaponData data = ScriptableObject.CreateInstance<WeaponData>();
            data.Kind = kind; data.Damage = damage; data.AttackInterval = interval; data.Range = range;
            data.ProjectileSpeed = speed; data.MagazineSize = magazine; data.ReserveAmmo = reserve; data.CrateDamage = crate;
            return data;
        }
        public void RequestAttack() { if (IsOwner) AttackRpc(AimDirection); }
        public void RequestReload() { if (IsOwner) ReloadRpc(); }
        public void RequestSwitch(int delta) { if (IsOwner) SwitchRpc(delta); }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] private void AttackRpc(Vector3 direction)
        {
            if (!IsServer || _stats == null || !_stats.IsAlive || _stats.IsInvulnerable || NetworkManager.Singleton.ServerTime.Time < _nextAttack) return;
            WeaponKind kind = _inventory.CurrentKind;
            WeaponData data = kind == WeaponKind.Sword ? _sword : _gun;
            if (data == null || kind == WeaponKind.None) return;
            _nextAttack = NetworkManager.Singleton.ServerTime.Time + data.AttackInterval;
            if (kind == WeaponKind.Gun && !_inventory.TryConsumeAmmoServer()) return;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = transform.forward;
            direction.Normalize();
            if (kind == WeaponKind.Sword) HitSword(direction, data); else HitGun(direction, data);
            _stats.ClearStealthServer();
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] private void ReloadRpc() { _inventory.ReloadServer(_gun != null ? _gun.MagazineSize : 12); }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] private void SwitchRpc(int delta) { _inventory.SwitchServer(delta); }
        private void HitGun(Vector3 direction, WeaponData data)
        {
            if (_projectilePrefab != null)
            {
                Vector3 origin = _muzzle != null ? _muzzle.position : transform.position + Vector3.up;
                NetworkProjectile projectile = Instantiate(_projectilePrefab, origin, Quaternion.LookRotation(direction));
                projectile.NetworkObject.Spawn();
                projectile.InitializeServer(direction, data.ProjectileSpeed, data.Range, data.Damage, OwnerClientId);
                return;
            }
            if (Physics.Raycast(transform.position + Vector3.up, direction, out RaycastHit hit, data.Range)) ResolveHit(hit.collider, data.Damage, WeaponKind.Gun, direction);
        }
        private void HitSword(Vector3 direction, WeaponData data)
        {
            foreach (Collider hit in Physics.OverlapSphere(transform.position + direction * data.Range * 0.5f, data.Range))
            {
                Vector3 to = hit.transform.position - transform.position; to.y = 0f;
                if (to.sqrMagnitude > data.Range * data.Range || Vector3.Angle(direction, to) > data.ArcAngle * 0.5f) continue;
                ResolveHit(hit, data.Damage, WeaponKind.Sword, direction);
            }
        }
        private void ResolveHit(Collider hit, int damage, WeaponKind kind, Vector3 direction)
        {
            if (hit.TryGetComponent(out NetworkPlayerStats target))
            {
                if (target == _stats || target.Role == _stats.Role) return;
                target.ApplyDamageServer(new DamageInfo(OwnerClientId, damage, DamageType.Weapon, direction * 2f));
            }
            if (hit.TryGetComponent(out NetworkCrate crate)) crate.ApplyWeaponHitServer(kind, damage, OwnerClientId);
        }
    }
}
