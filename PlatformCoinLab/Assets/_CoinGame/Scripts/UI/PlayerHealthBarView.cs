using UnityEngine;
using UnityEngine.UI;

namespace CoinGame.UI
{
    using CoinGame.Network;

    public sealed class PlayerHealthBarView : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _rect;
        [SerializeField]
        private CanvasGroup _group;
        [SerializeField]
        private Slider _slider;
        private Transform _target;
        private NetworkPlayerStats _stats;
        private NetworkPlayerController _visibility;
        private RectTransform _root;
        private Camera _worldCamera;
        private Canvas _canvas;
        private PlayerHealthBarSettings _settings;
        private int _lastHealth;
        private float _visibleUntil;

        public void Bind(Transform target, NetworkPlayerStats stats, RectTransform root,
            Camera worldCamera, Canvas canvas, PlayerHealthBarSettings settings)
        {
            _target = target;
            _stats = stats;
            _visibility = stats.GetComponent<NetworkPlayerController>();
            _root = root;
            _worldCamera = worldCamera;
            _canvas = canvas;
            _settings = settings;
            _lastHealth = stats.Health;
            _slider.value = (float)stats.Health / Mathf.Max(1, stats.MaxHealth);
            _group.alpha = 0f;
        }

        private void LateUpdate()
        {
            // Player/scene teardown can destroy either side first.
            if (_target == null || !_stats.IsSpawned)
            {
                Destroy(gameObject);
                return;
            }

            if (_stats.Health < _lastHealth)
            {
                _visibleUntil = Time.unscaledTime + _settings.DamageVisibleSeconds;
            }

            _lastHealth = _stats.Health;
            float health = (float)_stats.Health / Mathf.Max(1, _stats.MaxHealth);
            _slider.value = Mathf.MoveTowards(_slider.value, health,
                Time.unscaledDeltaTime / _settings.HealthChangeSeconds);
            Vector3 screen = _worldCamera.WorldToScreenPoint(_target.position + _settings.WorldOffset);
            Camera uiCamera = null;
            if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = _canvas.worldCamera;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, uiCamera, out Vector2 position);
            _rect.anchoredPosition = position;
            bool visible = _stats.IsOwner || Time.unscaledTime < _visibleUntil;
            if (_visibility.IsHiddenFromLocal)
            {
                _group.alpha = 0f;
                return;
            }

            if (screen.z <= 0f)
            {
                visible = false;
            }

            float alpha = 0f;
            if (visible)
            {
                alpha = 1f;
            }

            _group.alpha = Mathf.MoveTowards(_group.alpha, alpha,
                Time.unscaledDeltaTime / _settings.FadeSeconds);
        }
    }
}
