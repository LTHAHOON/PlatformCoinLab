using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace CoinGame.Network
{
    using CoinGame.Player;

    public sealed class NetworkWorldHealthBar : MonoBehaviour
    {
        [SerializeField]
        private Vector3 _worldOffset = new(0f, 2.25f, 0f);
        [SerializeField]
        private Vector3 _viewOffset;
        [SerializeField]
        private Vector2 _screenOffset;
        private NetworkPlayerStats _stats;
        private Slider _slider;
        private Canvas _canvas;
        private void Awake()
        {
            _stats = GetComponent<NetworkPlayerStats>();
            GameObject root = new("WorldHealthBar");
            root.transform.SetParent(transform, false);
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
            Vector3 worldPosition = transform.position + _worldOffset;
            if (camera != null)
            {
                worldPosition += camera.transform.right * _viewOffset.x;
                worldPosition += camera.transform.up * _viewOffset.y;
                worldPosition += camera.transform.forward * _viewOffset.z;
                Vector3 screenPosition = camera.WorldToScreenPoint(worldPosition);
                screenPosition += new Vector3(_screenOffset.x, _screenOffset.y, 0f);
                worldPosition = camera.ScreenToWorldPoint(screenPosition);
            }

            _canvas.transform.SetPositionAndRotation(worldPosition, Quaternion.identity);

            _slider.value = (float)_stats.Health / Mathf.Max(1, _stats.MaxHealth);
            NetworkPlayerStats local = null;
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                NetworkObject localObject = Unity.Netcode.NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                if (localObject != null)
                {
                    local = localObject.GetComponent<NetworkPlayerStats>();
                }
            }
            _canvas.enabled = !_stats.IsStealthed || local == null || local.Role == _stats.Role;
        }
    }
}
