using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace CoinGame.UI
{
    using CoinGame.Gameplay.Player;
    using CoinGame.Input;
    using CoinGame.Network;

    public sealed class InGameHudView : MonoBehaviour
    {
        [SerializeField]
        private bool _showPcKeys = true;
        private NetworkPlayerStats _stats;
        private NetworkInventory _inventory;
        private NetworkPlayerController _controller;
        [SerializeField]
        private TMP_Text _weaponText;
        [SerializeField]
        private TMP_Text _quickText;
        [SerializeField]
        private TMP_Text _statusText;
        [SerializeField]
        private TMP_Text _skillText;
        [SerializeField]
        private TMP_Text _reviveText;

        private void OnDestroy()
        {
            if (_stats != null)
            {
                _stats.HudStateChanged -= Refresh;
            }

            if (_inventory != null)
            {
                _inventory.Changed -= Refresh;
            }
        }
        private void Update()
        {
            BindLocal();
            if (_stats != null)
            {
                Refresh();
            }
        }
        private void BindLocal()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening ||
                NetworkManager.Singleton.SpawnManager == null)
            {
                return;
            }

            NetworkObject local = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (local == null || _stats != null && local == _stats.NetworkObject)
            {
                return;
            }

            if (_stats != null)
            {
                _stats.HudStateChanged -= Refresh;
            }

            if (_inventory != null)
            {
                _inventory.Changed -= Refresh;
            }
            _stats = local.GetComponent<NetworkPlayerStats>();
            _inventory = local.GetComponent<NetworkInventory>();
            _controller = local.GetComponent<NetworkPlayerController>();
            if (_stats == null || _inventory == null)
            {
                return;
            }
            _stats.HudStateChanged += Refresh;
            _inventory.Changed += Refresh;
        }
        private void Refresh()
        {
            if (_stats == null || _inventory == null)
            {
                return;
            }
            _weaponText.text = $"{_inventory.CurrentKind}  {_inventory.Magazine}/{_inventory.ReserveAmmo}";
            _quickText.text = $"{(ShowPcKeys ? "4 " : "")}Boost: {(_inventory.HasBoost ? "Ready" : "-")}  {(ShowPcKeys ? "5 " : "")}Revive: {(_inventory.HasRevive ? "Ready" : "-")}";
            _statusText.text = StatusText();
            _skillText.text = "";
            if (ShowPcKeys && _controller != null)
            {
                _skillText.text = $"O {_controller.PushCooldownRemaining:0.0}s  L {_controller.SkillCooldownRemaining:0.0}s";
            }
            _reviveText.text = _stats.LifeState == PlayerLifeState.Spectating && _stats.ReviveReadyAt > NetworkManager.Singleton.ServerTime.Time
                ? $"{(ShowPcKeys ? "5" : "Revive")} available in {_stats.ReviveReadyAt - NetworkManager.Singleton.ServerTime.Time:0.0}s" : "";
        }
        private string StatusText()
        {
            string text = "";
            if (_stats.IsInvulnerable)
            {
                text += $"Invulnerable {_stats.InvulnerableRemaining:0.0}s ";
            }

            if (_stats.IsStealthed)
            {
                text += $"Stealth {_stats.StealthRemaining:0.0}s ";
            }

            if (_stats.IsBoosted)
            {
                text += $"Boosted {_stats.BoostRemaining:0.0}s ";
            }

            if (_stats.IsCoinLocked)
            {
                text += $"Coin collection locked {_stats.CoinLockedRemaining:0.0}s";
            }
            return text;
        }

        private bool ShowPcKeys => _showPcKeys && MobileControlInput.Active == null;
    }
}
