using UnityEngine;

namespace CoinGame.Utilities
{
    public class SafeArea : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _root;
        private Rect safeArea;
        private Vector2 minAnchor;
        private Vector2 maxAnchor;
        private Vector2 _screenSize;

        private void Awake()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            if (safeArea != Screen.safeArea || _screenSize.x != Screen.width || _screenSize.y != Screen.height)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            //safeArea를 받아서 min 앵커와 max 앵커에 Position 부여
            //픽셀로 반환되니 앵커에 넣기 위해서는 비율로 변환 필요
            safeArea = Screen.safeArea;
            _screenSize = new Vector2(Screen.width, Screen.height);
            minAnchor = safeArea.position;
            maxAnchor = minAnchor + safeArea.size;

            //인스펙터 프로퍼티에 집어넣을 수 있게 비율로 변환 및 할당
            minAnchor.x /= Screen.width;
            minAnchor.y /= Screen.height;
            maxAnchor.x /= Screen.width;
            maxAnchor.y /= Screen.height;

            _root.anchorMin = minAnchor;
            _root.anchorMax = maxAnchor;
        }
    }
}
