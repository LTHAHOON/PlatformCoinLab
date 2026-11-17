using Unity.Netcode;
using UnityEngine;

namespace CoinGame.UI
{
    using CoinGame.Gameplay.Combat;
    using CoinGame.Input;
    using CoinGame.Managers;
    using CoinGame.Network;
    using CoinGame.Player;
    using CoinGame.Stage;

    public sealed class MobileControlsView : MonoBehaviour
    {
        [SerializeField]
        private bool _previewInEditor;
        [SerializeField]
        private MobileActionButton[] _buttons;
        [SerializeField]
        private MobileControlInput _input;
        [SerializeField]
        private FloatingJoystick _joystick;
        [SerializeField]
        private MobileControlLayout _layout;
        [SerializeField]
        private RoleAbilityConfig _abilities;
        private NetworkPlayerStats _stats;
        private NetworkInventory _inventory;
        private NetworkPlayerController _controller;

        private void Awake()
        {
            if (!Application.isMobilePlatform && !(Application.isEditor && _previewInEditor))
            {
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            NetworkManager manager = NetworkManager.Singleton;
            NetworkObject local = null;
            if (manager != null && manager.IsListening)
            {
                local = manager.SpawnManager.GetLocalPlayerObject();
            }

            if (local == null)
            {
                _stats = null;
            }
            else if (_stats == null || _stats.NetworkObject != local)
            {
                _stats = local.GetComponent<NetworkPlayerStats>();
                _inventory = local.GetComponent<NetworkInventory>();
                _controller = local.GetComponent<NetworkPlayerController>();
            }

            bool alive = _stats != null && _stats.IsAlive;
            bool disruptor = _stats != null && _stats.Role == PlayerRole.Disruptor;
            bool playing = GameManager.Instance != null && GameManager.Instance.CanPlayersMove;
            if (!alive || !playing || _layout.IsEditing)
            {
                _joystick.ResetPointer();
            }

            foreach (MobileActionButton button in _buttons)
            {
                bool roleSkill = button.Action == MobileAction.Push || button.Action == MobileAction.Skill;
                button.gameObject.SetActive(!roleSkill || disruptor || _layout.IsEditing);
                RefreshButton(button, alive && playing);
            }
        }

        private void RefreshButton(MobileActionButton button, bool alive)
        {
            if (_stats == null)
            {
                button.Refresh(false, false);
                return;
            }

            switch (button.Action)
            {
                case MobileAction.Attack:
                    button.Refresh(alive, false);
                    break;
                case MobileAction.Reload:
                    button.Refresh(alive && _inventory.CurrentKind == WeaponKind.Gun && _inventory.ReserveAmmo > 0, false, detail: _inventory.Magazine + "/" + _inventory.ReserveAmmo);
                    break;
                case MobileAction.Fist:
                    button.Refresh(alive, _inventory.CurrentKind == WeaponKind.Fist);
                    break;
                case MobileAction.Gun:
                    button.Refresh(alive && _inventory.HasGun, _inventory.CurrentKind == WeaponKind.Gun);
                    break;
                case MobileAction.Sword:
                    button.Refresh(alive && _inventory.HasSword, _inventory.CurrentKind == WeaponKind.Sword);
                    break;
                case MobileAction.Boost:
                    button.Refresh(alive && _inventory.HasBoost, false);
                    break;
                case MobileAction.Revive:
                    float remaining = Mathf.Max(0f, (float)(_stats.ReviveReadyAt - NetworkManager.Singleton.ServerTime.Time));
                    bool canRevive = !_stats.IsAlive && _inventory.HasRevive && remaining > 0f;
                    string detail = "";
                    if (canRevive)
                    {
                        detail = "지금! " + remaining.ToString("0.0") + "s";
                    }

                    button.Refresh(canRevive, canRevive, detail: detail);
                    break;
                case MobileAction.Push:
                    button.Refresh(alive && _controller.PushCooldownRemaining <= 0f, false, _controller.PushCooldownRemaining, _abilities.PushCooldown);
                    break;
                case MobileAction.Skill:
                    button.Refresh(alive && _controller.SkillCooldownRemaining <= 0f, false, _controller.SkillCooldownRemaining, Mathf.Max(_abilities.LockCooldown, _abilities.ItemBlockCooldown));
                    break;
            }
        }
    }
}
