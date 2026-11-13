using UnityEngine;

namespace CoinGame.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaLayout : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastArea;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (_lastArea == Screen.safeArea)
            {
                return;
            }

            Apply();
        }

        private void Apply()
        {
            _lastArea = Screen.safeArea;
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.offsetMin = new Vector2(_lastArea.xMin, _lastArea.yMin);
            _rect.offsetMax = new Vector2(_lastArea.xMax - Screen.width, _lastArea.yMax - Screen.height);
        }
    }
}
