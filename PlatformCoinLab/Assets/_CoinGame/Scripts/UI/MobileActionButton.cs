using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoinGame.UI
{
    using CoinGame.Input;

    public sealed class MobileActionButton : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField]
        private MobileControlInput _input;
        [SerializeField]
        private MobileAction _action;
        [SerializeField]
        private MobileControlLayout _layout;
        [SerializeField]
        private CanvasGroup _group;
        [SerializeField]
        private Image _face;
        [SerializeField]
        private Image _cooldown;
        [SerializeField]
        private TMP_Text _detail;
        [SerializeField]
        private Color _normal = new Color(0.08f, 0.13f, 0.22f, 0.9f);
        [SerializeField]
        private Color _selected = new Color(0.57f, 0.37f, 0.1f, 0.95f);
        private bool _available;

        public void OnPointerDown(PointerEventData data)
        {
            if (!_layout.IsEditing && _available)
            {
                _input.Press(_action);
            }
        }

        public void OnDrag(PointerEventData data)
        {
            if (_layout.IsEditing)
            {
                _layout.Move((RectTransform)transform, data.position, data.pressEventCamera);
            }
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (_layout.IsEditing)
            {
                _layout.Save();
            }
        }

        public void Refresh(bool available, bool selected, float remaining = 0f, float duration = 1f, string detail = "")
        {
            _available = available;
            _group.alpha = 0.4f;
            if (available || _layout.IsEditing)
            {
                _group.alpha = 1f;
            }

            _face.color = _normal;
            if (selected)
            {
                _face.color = _selected;
            }

            _cooldown.fillAmount = Mathf.Clamp01(remaining / Mathf.Max(duration, 0.01f));
            _detail.text = detail;
            if (remaining > 0f)
            {
                _detail.text = remaining.ToString("0.0") + "s";
            }
        }

        public MobileAction Action => _action;
    }
}
