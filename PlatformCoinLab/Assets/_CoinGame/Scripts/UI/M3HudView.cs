using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using CoinGame.Gameplay.Combat;
using CoinGame.Gameplay.Player;
using CoinGame.Network;

namespace CoinGame.UI
{
    public class M3HudView : MonoBehaviour
    {
        [SerializeField] private bool _showPcKeys = true;
        private NetworkPlayerStats _stats;
        private NetworkInventory _inventory;
        private NetworkPlayerController _controller;
        private Slider _health;
        private TMP_Text _healthText;
        private TMP_Text _weaponText;
        private TMP_Text _quickText;
        private TMP_Text _statusText;
        private TMP_Text _skillText;
        private TMP_Text _reviveText;

        private void Start() { Build(); }
        private void OnDestroy()
        {
            if (_stats != null) _stats.HudStateChanged -= Refresh;
            if (_inventory != null) _inventory.Changed -= Refresh;
        }
        private void Update()
        {
            BindLocal();
            if (_stats != null) Refresh();
        }
        private void BindLocal()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || NetworkManager.Singleton.SpawnManager == null) return;
            NetworkObject local = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (local == null || local == _stats?.NetworkObject) return;
            if (_stats != null) _stats.HudStateChanged -= Refresh;
            if (_inventory != null) _inventory.Changed -= Refresh;
            _stats = local.GetComponent<NetworkPlayerStats>();
            _inventory = local.GetComponent<NetworkInventory>();
            _controller = local.GetComponent<NetworkPlayerController>();
            if (_stats == null || _inventory == null) return;
            _stats.HudStateChanged += Refresh;
            _inventory.Changed += Refresh;
        }
        private void Build()
        {
            RectTransform panel = MakePanel("M3_HUD", new Vector2(24f, -96f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            _health = MakeSlider(panel, "HealthBar", new Vector2(180f, 18f));
            _healthText = MakeText(panel, "HealthText", "HP", 16);
            _weaponText = MakeText(panel, "WeaponText", "Weapon", 16);
            _quickText = MakeText(panel, "QuickText", "1 Boost | 2 Revive", 14);
            _statusText = MakeText(panel, "StatusText", "", 14);
            _skillText = MakeText(panel, "SkillText", "", 14);
            _reviveText = MakeText(panel, "ReviveText", "", 18);
            ApplySafeArea(panel);
        }
        private void Refresh()
        {
            if (_stats == null || _inventory == null) return;
            _health.maxValue = Mathf.Max(1, _stats.MaxHealth);
            _health.value = _stats.Health;
            _healthText.text = $"HP {_stats.Health}/{_stats.MaxHealth}";
            _weaponText.text = $"{_inventory.CurrentKind}  {_inventory.Magazine}/{_inventory.ReserveAmmo}";
            _quickText.text = $"{(_showPcKeys ? "1 " : "")}Boost: {(_inventory.HasBoost ? "Ready" : "-")}  {(_showPcKeys ? "2 " : "")}Revive: {(_inventory.HasRevive ? "Ready" : "-")}";
            _statusText.text = StatusText();
            _skillText.text = _controller == null ? "" : $"O {_controller.PushCooldownRemaining:0.0}s  L {_controller.SkillCooldownRemaining:0.0}s";
            _reviveText.text = _stats.LifeState == PlayerLifeState.Spectating && _stats.ReviveReadyAt > NetworkManager.Singleton.ServerTime.Time
                ? $"{(_showPcKeys ? "2" : "Revive")} 부활 {_stats.ReviveReadyAt - NetworkManager.Singleton.ServerTime.Time:0.0}s" : "";
        }
        private string StatusText()
        {
            string text = "";
            if (_stats.IsInvulnerable) text += $"무적 {_stats.InvulnerableRemaining:0.0}s ";
            if (_stats.IsStealthed) text += $"은신 {_stats.StealthRemaining:0.0}s ";
            if (_stats.IsBoosted) text += $"부스트 {_stats.BoostRemaining:0.0}s ";
            if (_stats.IsItemBlocked) text += $"습득불가 {_stats.ItemBlockedRemaining:0.0}s";
            return text;
        }
        private RectTransform MakePanel(string name, Vector2 offset, Vector2 min, Vector2 max)
        {
            GameObject go = new(name); go.transform.SetParent(transform, false);
            RectTransform rect = go.AddComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = offset; rect.sizeDelta = new Vector2(260f, 180f);
            return rect;
        }
        private TMP_Text MakeText(RectTransform parent, string name, string value, int size)
        {
            GameObject go = new(name); go.transform.SetParent(parent, false); TMP_Text text = go.AddComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = size; text.color = Color.white; text.rectTransform.sizeDelta = new Vector2(260f, 26f); text.rectTransform.anchoredPosition = new Vector2(0f, -parent.childCount * 26f); return text;
        }
        private Slider MakeSlider(RectTransform parent, string name, Vector2 size)
        {
            GameObject go = new(name); go.transform.SetParent(parent, false); Slider slider = go.AddComponent<Slider>(); slider.minValue = 0f; slider.maxValue = 100f; slider.fillRect = null; slider.targetGraphic = go.AddComponent<Image>(); slider.targetGraphic.color = Color.green; slider.GetComponent<RectTransform>().sizeDelta = size; return slider;
        }
        private void ApplySafeArea(RectTransform rect)
        {
            Rect safe = Screen.safeArea; rect.offsetMin = new Vector2(Mathf.Max(rect.offsetMin.x, safe.xMin), rect.offsetMin.y); rect.offsetMax = new Vector2(rect.offsetMax.x, -Mathf.Max(0f, Screen.height - safe.yMax));
        }
    }
}
