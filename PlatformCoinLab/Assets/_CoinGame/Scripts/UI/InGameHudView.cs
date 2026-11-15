using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace CoinGame.UI
{
    using CoinGame.Gameplay.Combat;
    using CoinGame.Gameplay.Player;
    using CoinGame.Network;

    public sealed class InGameHudView : MonoBehaviour
    {
        [SerializeField]
        private bool _showPcKeys = true;
        private NetworkPlayerStats _stats;
        private NetworkInventory _inventory;
        private NetworkPlayerController _controller;
        private Slider _health;
        private Image _healthFill;
        private TMP_Text _healthText;
        private TMP_Text _weaponText;
        private TMP_Text _quickText;
        private TMP_Text _statusText;
        private TMP_Text _skillText;
        private TMP_Text _reviveText;

        private void Start()
        {
            Build();
        }
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
        private void Build()
        {
            RectTransform panel = MakePanel("InGame_HUD", new Vector2(24f, -850f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            _health = MakeSlider(panel, "HealthBar", new Vector2(180f, 18f));
            _healthText = MakeText(panel, "HealthText", "HP", 16);
            _weaponText = MakeText(panel, "WeaponText", "Weapon", 16);
            _quickText = MakeText(panel, "QuickText", "4 Boost | 5 Revive", 14);
            _statusText = MakeText(panel, "StatusText", "", 14);
            _skillText = MakeText(panel, "SkillText", "", 14);
            _reviveText = MakeText(panel, "ReviveText", "", 18);
            ApplySafeArea(panel);
        }
        private void Refresh()
        {
            if (_stats == null || _inventory == null)
            {
                return;
            }
            _health.maxValue = Mathf.Max(1, _stats.MaxHealth);
            _health.value = _stats.Health;
            _healthText.text = $"Health {_stats.Health}/{_stats.MaxHealth}";
            _weaponText.text = $"{_inventory.CurrentKind}  {_inventory.Magazine}/{_inventory.ReserveAmmo}";
            _quickText.text = $"{(_showPcKeys ? "4 " : "")}Boost: {(_inventory.HasBoost ? "Ready" : "-")}  {(_showPcKeys ? "5 " : "")}Revive: {(_inventory.HasRevive ? "Ready" : "-")}";
            _statusText.text = StatusText();
            _skillText.text = _controller == null ? "" : $"O {_controller.PushCooldownRemaining:0.0}s  L {_controller.SkillCooldownRemaining:0.0}s";
            _reviveText.text = _stats.LifeState == PlayerLifeState.Spectating && _stats.ReviveReadyAt > NetworkManager.Singleton.ServerTime.Time
                ? $"{(_showPcKeys ? "5" : "Revive")} available in {_stats.ReviveReadyAt - NetworkManager.Singleton.ServerTime.Time:0.0}s" : "";
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
        private RectTransform MakePanel(string name, Vector2 offset, Vector2 min, Vector2 max)
        {
            GameObject go = new(name); go.transform.SetParent(transform, false);
            RectTransform rect = go.AddComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = offset; rect.sizeDelta = new Vector2(260f, 240f);
            return rect;
        }
        private TMP_Text MakeText(RectTransform parent, string name, string value, int size)
        {
            GameObject go = new(name); go.transform.SetParent(parent, false); TMP_Text text = go.AddComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = size; text.color = Color.white; text.rectTransform.sizeDelta = new Vector2(260f, 26f); text.rectTransform.anchoredPosition = new Vector2(0f, -parent.childCount * 26f); return text;
        }
        private Slider MakeSlider(RectTransform parent, string name, Vector2 size)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            RectTransform root = go.AddComponent<RectTransform>();
            root.sizeDelta = size;
            Image background = go.AddComponent<Image>();
            background.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            Slider slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.targetGraphic = background;

            GameObject fill = new("Fill");
            fill.transform.SetParent(go.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            _healthFill = fill.AddComponent<Image>();
            _healthFill.color = Color.green;
            slider.fillRect = fillRect;
            return slider;
        }
        private void ApplySafeArea(RectTransform rect)
        {
            Rect safe = Screen.safeArea; rect.offsetMin = new Vector2(Mathf.Max(rect.offsetMin.x, safe.xMin), rect.offsetMin.y); rect.offsetMax = new Vector2(rect.offsetMax.x, -Mathf.Max(0f, Screen.height - safe.yMax));
        }
    }
}

