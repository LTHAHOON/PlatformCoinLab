using UnityEngine;
using UnityEngine.EventSystems;

namespace CoinGame.UI
{
    using CoinGame.Input;

    public sealed class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
    {
        [SerializeField]
        private RectTransform _touchArea;
        [SerializeField]
        private RectTransform _visual;
        [SerializeField]
        private RectTransform _handle;
        [SerializeField]
        private CanvasGroup _group;
        [SerializeField]
        private MobileControlsSettings _settings;
        [SerializeField]
        private MobileControlInput _input;
        private int? _pointer;
        private Vector2 _origin;
        private float _visibility;

        private void Awake()
        {
            ResetPointer();
        }

        private void Update()
        {
            float target = 0f;
            float duration = _settings.HideDuration;
            if (_pointer.HasValue)
            {
                target = 1f;
                duration = _settings.ShowDuration;
            }

            _visibility = Mathf.MoveTowards(_visibility, target, Time.unscaledDeltaTime / duration);
            float eased = Mathf.SmoothStep(0f, 1f, _visibility);
            _group.alpha = eased * _settings.JoystickOpacity;
            _visual.localScale = Vector3.one * Mathf.Lerp(_settings.HiddenScale, 1f, eased);
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (_pointer.HasValue)
            {
                return;
            }

            _pointer = data.pointerId;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_touchArea, data.position, data.pressEventCamera, out _origin);
            _visual.anchoredPosition = _origin;
            _handle.anchoredPosition = Vector2.zero;
            _input.Move(Vector2.zero);
        }

        public void OnInitializePotentialDrag(PointerEventData data)
        {
            data.useDragThreshold = false;
        }

        public void OnDrag(PointerEventData data)
        {
            if (_pointer != data.pointerId)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_touchArea, data.position, data.pressEventCamera, out Vector2 point);
            Vector2 delta = Vector2.ClampMagnitude(point - _origin, _settings.JoystickRadius);
            _handle.anchoredPosition = delta;
            Vector2 value = delta / _settings.JoystickRadius;
            if (value.magnitude < _settings.DeadZone)
            {
                value = Vector2.zero;
            }

            _input.Move(value);
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (_pointer == data.pointerId)
            {
                _pointer = null;
                _handle.anchoredPosition = Vector2.zero;
                _input.Move(Vector2.zero);
            }
        }

        private void OnDisable()
        {
            ResetPointer();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                ResetPointer();
            }
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                ResetPointer();
            }
        }

        public void ResetPointer()
        {
            _pointer = null;
            _visibility = 0f;
            _group.alpha = 0f;
            _handle.anchoredPosition = Vector2.zero;
            _input.Move(Vector2.zero);
        }
    }
}
