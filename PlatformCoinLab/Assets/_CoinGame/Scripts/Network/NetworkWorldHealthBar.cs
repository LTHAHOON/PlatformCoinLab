using UnityEngine;
using UnityEngine.UI;
using CoinGame.Player;

namespace CoinGame.Network
{
    public sealed class NetworkWorldHealthBar : MonoBehaviour
    {
        private NetworkPlayerStats _stats;
        private Slider _slider;
        private Canvas _canvas;
        private void Awake()
        {
            _stats = GetComponent<NetworkPlayerStats>();
            GameObject root = new("WorldHealthBar");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.up * 2.25f;
            _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.transform.localScale = Vector3.one * 0.01f;

            GameObject bar = new("Bar");
            bar.transform.SetParent(root.transform, false);
            _slider = bar.AddComponent<Slider>();
            _slider.minValue = 0f;
            _slider.maxValue = 1f;
            _slider.targetGraphic = bar.AddComponent<Image>();
            _slider.targetGraphic.color = Color.green;
            bar.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 10f);
        }
        private void LateUpdate()
        {
            if (_stats == null || _canvas == null)
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera != null)
            {
                _canvas.transform.rotation = Quaternion.LookRotation(
                    _canvas.transform.position - camera.transform.position);
            }

            _slider.value = (float)_stats.Health / Mathf.Max(1, _stats.MaxHealth);
            NetworkPlayerStats local = Unity.Netcode.NetworkManager.Singleton?.SpawnManager.GetLocalPlayerObject()?.GetComponent<NetworkPlayerStats>();
            _canvas.enabled = !_stats.IsStealthed || local == null || local.Role == _stats.Role;
        }
    }
}
