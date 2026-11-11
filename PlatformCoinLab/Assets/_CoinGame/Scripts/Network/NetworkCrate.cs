using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("_maxHealth")]
        private int _maxHealth = 30;
        [SerializeField]
        private NetworkItemPickup _pickupPrefab;
        [SerializeField]
        private ItemData[] _dropTable;

        [SerializeField]
        private Transform _lid;
        private readonly NetworkVariable<int> _health = new(30);
        private readonly NetworkVariable<CrateState> _state = new(CrateState.Closed);
        private readonly NetworkVariable<int> _openedHits = new(0);
        private Quaternion _closedRotation;
        private Quaternion _openedRotation;
        private float _lidOpenStarted;
        private float _destroyAt;

        private int MaxHealth => _data != null ? _data.MaxHealth : _maxHealth;
        private int OpenedHitCount => _data != null ? _data.OpenedHitCount : 2;

        public void ApplyWeaponHitServer(WeaponKind kind, int damage, ulong attacker)
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
                    _state.Value = CrateState.Destroyed;
                    _destroyAt = Time.time + (_data != null ? _data.DestroyDuration : 0.2f);
                }
                return;
            }

            _health.Value -= Mathf.Max(1, kind == WeaponKind.Sword ? damage * 2 : damage);
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

        public override void OnNetworkDespawn()
        {
            _state.OnValueChanged -= OnStateChanged;
        }

        private void SetupLid()
        {
            _lid ??= transform.Find("1_chest_top");
            if (_lid == null) return;
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
            if (_lid == null) return;
            if (state == CrateState.Closed)
            {
                _lid.localRotation = _closedRotation;
                return;
            }
            if (state == CrateState.Opened)
            {
                _lidOpenStarted = Time.time;
                if (instant) _lid.localRotation = _openedRotation;
                return;
            }
            _lid.localRotation = _openedRotation;
        }

        private void DropServer()
        {
            if (_dropTable != null && _dropTable.Length > 0)
            {
                int total = 0;
                foreach (ItemData entry in _dropTable)
                {
                    if (entry != null && CanDropItemServer(entry))
                    {
                        total += Mathf.Max(0, entry.Weight);
                    }
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
                    Vector3 offset = _data != null ? _data.DropOffset : new Vector3(0f, 0.8f, 0.6f);
                    NetworkItemPickup pickup = Instantiate(_pickupPrefab, transform.TransformPoint(offset), Quaternion.identity);
                    pickup.InitializeServer(item);
                    pickup.NetworkObject.Spawn();
                }
            }
            NetworkObject.Despawn();
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
                if (stats == null || inventory == null || stats.Role != PlayerRole.Collector) continue;
                sawCollector = true;
                if (item.Kind == ItemKind.Boost && !inventory.HasBoost || item.Kind == ItemKind.Revive && !inventory.HasRevive) return true;
            }
            return !sawCollector;
        }
    }
}
