using Unity.Netcode;
using UnityEngine;

namespace CoinGame.Network
{
    using CoinGame.Gameplay.Combat;
    using CoinGame.Gameplay.Items;
    using CoinGame.Player;


    public class NetworkCrate : NetworkBehaviour
    {
        private enum CrateState : byte
        {
            Closed,
            Opened,
            Destroyed,
        }

        [SerializeField]
        private CrateData _data;
        [SerializeField]
        private NetworkItemPickup _pickupPrefab;
        [SerializeField]
        private NetworkItemPickup _ammoPickupPrefab;
        [SerializeField]
        private NetworkItemPickup _boostPickupPrefab;
        [SerializeField]
        private NetworkItemPickup _healthPickupPrefab;
        [SerializeField]
        private NetworkItemPickup _invulnerablePickupPrefab;
        [SerializeField]
        private NetworkItemPickup _randomPickupPrefab;
        [SerializeField]
        private NetworkItemPickup _revivePickupPrefab;
        [SerializeField]
        private NetworkItemPickup _stealthPickupPrefab;
        [SerializeField]
        private ItemData[] _dropTable;

        [SerializeField]
        private Transform _lid;
        [SerializeField]
        private bool _showHealthDebug = true;
        private readonly NetworkVariable<int> _health = new(30);
        private readonly NetworkVariable<CrateState> _state = new(CrateState.Closed);
        private readonly NetworkVariable<int> _openedHits = new(0);
        private Quaternion _closedRotation;
        private Quaternion _openedRotation;
        private float _lidOpenStarted;
        private float _destroyAt;
        private NetworkItemPickup _pendingPickup;


        public void ApplyWeaponHitServer(int crateDamage, ulong attacker)
        {
            if (!IsServer || _state.Value == CrateState.Destroyed)
            {
                return;
            }
            if (_state.Value == CrateState.Opened)
            {
                _openedHits.Value++;
                if (_openedHits.Value >= OpenedHitCount)
                {
                    if (_pendingPickup != null)
                    {
                        _pendingPickup.EnablePickupServer();
                        _pendingPickup = null;
                    }
                    _state.Value = CrateState.Destroyed;
                    _destroyAt = Time.time + (_data != null ? _data.DestroyDuration : 0.2f);
                }
                return;
            }

            int previousHealth = _health.Value;
            _health.Value -= Mathf.Max(1, crateDamage);
            if (Debug.isDebugBuild)
            {
                Debug.Log($"[CrateHit] {name}: {previousHealth} -> {_health.Value}", this);
            }
            if (_health.Value <= 0)
            {
                _state.Value = CrateState.Opened;
                DropServer();
            }
        }
        
        public override void OnNetworkSpawn() 
        { 
            if (IsServer)
            {
                _health.Value = MaxHealth;
                _state.Value = CrateState.Closed;
                _openedHits.Value = 0;
            } 
            SetupLid();
            _state.OnValueChanged += OnStateChanged;
            ApplyState(_state.Value, true);
        }

        private void Update()
        {
            if (_state.Value == CrateState.Destroyed && (!IsServer || Time.time >= _destroyAt))
            {
                if (IsServer && NetworkObject.IsSpawned)
                {
                    NetworkObject.Despawn();
                }
                return;
            }
            if (_state.Value == CrateState.Opened && _lid != null)
            {
                float duration = _data != null ? _data.LidOpenDuration : 0.35f;
                float t = Mathf.Clamp01((Time.time - _lidOpenStarted) / Mathf.Max(0.01f, duration));
                _lid.localRotation = Quaternion.Slerp(_closedRotation, _openedRotation, t);
            }
        }

        private void OnGUI()
        {
            if (!_showHealthDebug || !IsSpawned || Camera.main == null)
            {
                return;
            }

            Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
            if (screen.z <= 0f)
            {
                return;
            }

            Rect rect = new Rect(screen.x - 40f, Screen.height - screen.y - 18f, 80f, 18f);
            GUI.Label(rect, $"Crate {_health.Value}/{MaxHealth}");
        }

        public override void OnNetworkDespawn()
        {
            _state.OnValueChanged -= OnStateChanged;
        }

        private void SetupLid()
        {
            if (_lid == null)
            {
                return;
            }

            _closedRotation = _lid.localRotation;
            Vector3 axis = _data != null ? _data.LidRotationAxis : Vector3.right;
            float angle = _data != null ? _data.LidOpenAngle : 105f;
            _openedRotation = _closedRotation * Quaternion.AngleAxis(angle, axis.normalized);
        }

        private void OnStateChanged(CrateState previous, CrateState current)
        {
            ApplyState(current, false);
        }

        private void ApplyState(CrateState state, bool instant)
        {
            if (_lid == null)
            {
                return;
            }
            if (state == CrateState.Closed)
            {
                _lid.localRotation = _closedRotation;
                return;
            }
            if (state == CrateState.Opened)
            {
                _lidOpenStarted = Time.time;
                if (instant)
                {
                    _lid.localRotation = _openedRotation;
                }
                return;
            }
            _lid.localRotation = _openedRotation;
        }

        private void DropServer()
        {
            if (_dropTable != null && _dropTable.Length > 0)
            {
                ItemData guaranteed = FindGuaranteedAttackItemServer();
                if (guaranteed != null)
                {
                    SpawnPickupServer(guaranteed);
                    if (NetworkRoundState.Instance != null)
                    {
                        NetworkRoundState.Instance.RegisterRoundOneAttackItemDropServer();
                    }
                    return;
                }

                int total = 0;
                foreach (ItemData entry in _dropTable)
                {
                    if (entry != null && CanDropItemServer(entry))
                    {
                        total += Mathf.Max(0, entry.Weight);
                    }
                }
                
                if (total <= 0)
                {
                    return;
                }

                int roll = Random.Range(0, total);
                ItemData item = null;
                foreach (ItemData entry in _dropTable)
                {
                    if (entry == null || !CanDropItemServer(entry))
                    {
                        continue;
                    }
                    roll -= Mathf.Max(0, entry.Weight);
                    if (roll < 0)
                    {
                        item = entry;
                        break;
                    }
                }
                if (item != null && GetPickupPrefab(item.Kind) != null)
                {
                    SpawnPickupServer(item);
                }
            }
        }

        private ItemData FindGuaranteedAttackItemServer()
        {
            if (NetworkRoundState.Instance == null || !NetworkRoundState.Instance.ShouldGuaranteeRoundOneAttackItemServer())
            {
                return null;
            }

            foreach (ItemData item in _dropTable)
            {
                if (item != null && CanDropItemServer(item) && (item.Kind == ItemKind.Gun || item.Kind == ItemKind.Sword))
                {
                    return item;
                }
            }

            return null;
        }

        private void SpawnPickupServer(ItemData item)
        {
            NetworkItemPickup pickupPrefab = GetPickupPrefab(item.Kind);
            if (pickupPrefab == null)
            {
                return;
            }

            Vector3 offset = _data != null ? _data.DropOffset : new Vector3(0f, 1.6f, 0.4f);
            Vector3 position = transform.position + transform.rotation * offset;
            NetworkItemPickup pickup = Instantiate(pickupPrefab, position, Quaternion.identity);
            pickup.InitializeServer(item, false);
            pickup.NetworkObject.Spawn();
            _pendingPickup = pickup;
        }

        private NetworkItemPickup GetPickupPrefab(ItemKind kind)
        {
            if (kind == ItemKind.Ammo && _ammoPickupPrefab != null)
            {
                return _ammoPickupPrefab;
            }
            if (kind == ItemKind.Boost && _boostPickupPrefab != null)
            {
                return _boostPickupPrefab;
            }
            if (kind == ItemKind.Health && _healthPickupPrefab != null)
            {
                return _healthPickupPrefab;
            }
            if (kind == ItemKind.Invulnerable && _invulnerablePickupPrefab != null)
            {
                return _invulnerablePickupPrefab;
            }
            if (kind == ItemKind.Random && _randomPickupPrefab != null)
            {
                return _randomPickupPrefab;
            }
            if (kind == ItemKind.Revive && _revivePickupPrefab != null)
            {
                return _revivePickupPrefab;
            }
            if (kind == ItemKind.Stealth && _stealthPickupPrefab != null)
            {
                return _stealthPickupPrefab;
            }

            return _pickupPrefab;
        }

        private bool CanDropItemServer(ItemData item)
        {
            if (item.Kind != ItemKind.Boost && item.Kind != ItemKind.Revive)
            {
                return true;
            }

            bool sawCollector = false;
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                NetworkPlayerStats stats = client.PlayerObject != null ? client.PlayerObject.GetComponent<NetworkPlayerStats>() : null;
                NetworkInventory inventory = client.PlayerObject != null ? client.PlayerObject.GetComponent<NetworkInventory>() : null;
                if (stats == null || inventory == null || stats.Role != PlayerRole.Collector)
                {
                    continue;
                }
                sawCollector = true;
                if ((item.Kind == ItemKind.Boost && !inventory.HasBoost) ||
                    (item.Kind == ItemKind.Revive && !inventory.HasRevive))
                {
                    return true;
                }
            }
            return !sawCollector;
        }

        private int MaxHealth => _data != null ? _data.MaxHealth : 1;
        private int OpenedHitCount => _data != null ? _data.OpenedHitCount : 1;
        public int CurrentHealth => _health.Value;

    }
}
