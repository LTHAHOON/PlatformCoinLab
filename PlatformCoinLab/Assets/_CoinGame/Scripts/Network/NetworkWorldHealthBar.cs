using UnityEngine;
using UnityEngine.UI;
using CoinGame.Player;

namespace CoinGame.Network
{
    public class NetworkWorldHealthBar : MonoBehaviour
    {
        private NetworkPlayerStats _stats;
        private Slider _slider;
        private Canvas _canvas;
        private void Awake()
        {
            _stats = GetComponent<NetworkPlayerStats>();
            GameObject go = new("WorldHealthBar"); go.transform.SetParent(transform, false); go.transform.localPosition = Vector3.up * 1.35f;
            _canvas = go.AddComponent<Canvas>(); _canvas.renderMode = RenderMode.WorldSpace; _canvas.transform.localScale = Vector3.one * 0.01f;
            GameObject bar = new("Bar"); bar.transform.SetParent(go.transform, false); _slider = bar.AddComponent<Slider>(); _slider.minValue = 0f; _slider.maxValue = 1f; _slider.targetGraphic = bar.AddComponent<Image>(); _slider.targetGraphic.color = Color.green; bar.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 10f);
        }
        private void LateUpdate()
        {
            if (_stats == null || _canvas == null) return;
            Camera camera = Camera.main; if (camera != null) _canvas.transform.rotation = Quaternion.LookRotation(_canvas.transform.position - camera.transform.position);
            _slider.value = _stats.MaxHealth <= 0 ? 0f : (float)_stats.Health / _stats.MaxHealth;
            NetworkPlayerStats local = Unity.Netcode.NetworkManager.Singleton?.SpawnManager.GetLocalPlayerObject()?.GetComponent<NetworkPlayerStats>();
            _canvas.enabled = !_stats.IsStealthed || local == null || local.Role == _stats.Role;
        }
    }
}
