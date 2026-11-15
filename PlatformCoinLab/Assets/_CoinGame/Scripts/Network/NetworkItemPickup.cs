using Unity.Netcode;
using UnityEngine;

namespace CoinGame.Network
{
    using CoinGame.Gameplay.Items;
    using CoinGame.Utilities;

    public class NetworkItemPickup : NetworkBehaviour
    {
        [SerializeField] private ItemData _data;
        private readonly NetworkVariable<byte> _kind = new((byte)ItemKind.Ammo);
        private readonly NetworkVariable<bool> _pickupEnabled = new(true);
        private bool _serverPickupEnabled = true;
        private TextMesh _label;

        public void InitializeServer(ItemData data, bool pickupEnabled = true)
        {
            if (data == null)
            {
                return;
            }

            _data = data;
            _kind.Value = (byte)data.Kind;
            _serverPickupEnabled = pickupEnabled;
            _pickupEnabled.Value = pickupEnabled;
            ApplyPickupState(pickupEnabled);
        }

        public void EnablePickupServer()
        {
            if (IsServer)
            {
                _serverPickupEnabled = true;
                _pickupEnabled.Value = true;
                ApplyPickupState(true);
            }
        }

        public override void OnNetworkSpawn()
        {
            _pickupEnabled.OnValueChanged += OnPickupEnabledChanged;
            _kind.OnValueChanged += OnKindChanged;
            ApplyPickupState(_pickupEnabled.Value);
            CreateLabel();
            UpdateLabel((ItemKind)_kind.Value);
            if (_data == null || !TryGetComponent(out SphereCollider pickup))
            {
                return;
            }

            float scale = Mathf.Max(0.01f, _data.DropScale);
            transform.localScale *= scale;
            pickup.radius = _data.PickupRadius / scale;
        }

        public override void OnNetworkDespawn()
        {
            _pickupEnabled.OnValueChanged -= OnPickupEnabledChanged;
            _kind.OnValueChanged -= OnKindChanged;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || !_serverPickupEnabled || !_pickupEnabled.Value || _data == null ||
                !other.TryGetComponent(out NetworkPlayerStats stats))
            {
                return;
            }

            if (stats.TryAcquireItemServer(_data))
            {
                NetworkGameObjectPool.Despawn(gameObject);
            }
        }

        private void OnPickupEnabledChanged(bool previous, bool current)
        {
            ApplyPickupState(current);
        }

        private void OnKindChanged(byte previous, byte current)
        {
            UpdateLabel((ItemKind)current);
        }

        private void ApplyPickupState(bool enabled)
        {
            if (TryGetComponent(out SphereCollider pickup))
            {
                pickup.enabled = enabled;
            }
        }

        private void CreateLabel()
        {
            if (_label != null)
            {
                return;
            }

            GameObject labelObject = new("ItemLabel");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = Vector3.up * 0.8f;
            _label = labelObject.AddComponent<TextMesh>();
            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;
            _label.characterSize = 0.12f;
            _label.fontSize = 32;
            _label.color = Color.white;
        }

        private void UpdateLabel(ItemKind kind)
        {
            if (_label != null)
            {
                _label.text = kind.ToString();
            }
        }
    }
}
