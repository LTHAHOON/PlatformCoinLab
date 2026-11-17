using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoinGame.UI
{
    using CoinGame.Input;
    using CoinGame.Utilities;

    public sealed class MobileControlLayout : MonoBehaviour
    {
        [Serializable]
        private sealed class LayoutData
        {
            public Vector2[] Positions;
            public float Scale = 1f;
        }

        [SerializeField]
        private RectTransform _root;
        [SerializeField]
        private RectTransform[] _buttons;
        [SerializeField]
        private MobileControlsSettings _settings;
        [SerializeField]
        private MobileControlInput _input;
        [SerializeField]
        private FloatingJoystick _joystick;
        [SerializeField]
        private GameObject _panel;
        [SerializeField]
        private TMP_Text _editLabel;
        [SerializeField]
        private Slider _scaleSlider;
        private Vector2[] _defaults;
        private Vector2 _layoutSize;
        private const string SaveKey = "CoinGame.MobileControls.v1";

        private void Start()
        {
            _layoutSize = _root.rect.size;
            _defaults = new Vector2[_buttons.Length];
            for (int i = 0; i < _buttons.Length; i++)
            {
                _defaults[i] = _buttons[i].anchoredPosition;
            }

            _scaleSlider.SetValueWithoutNotify(1f);
            _scaleSlider.minValue = _settings.MinimumButtonScale;
            _scaleSlider.maxValue = _settings.MaximumButtonScale;
            _scaleSlider.SetValueWithoutNotify(1f);
            ApplyScale(1f);
            if (JsonSettingsStorage.TryLoad(SaveKey, 1, out LayoutData data, IsValid))
            {
                _scaleSlider.SetValueWithoutNotify(data.Scale);
                ApplyScale(data.Scale);
                for (int i = 0; i < _buttons.Length; i++)
                {
                    _buttons[i].anchoredPosition = Vector2.Scale(data.Positions[i], _root.rect.size);
                    Clamp(_buttons[i]);
                }
            }
        }

        private void LateUpdate()
        {
            Vector2 size = _root.rect.size;
            if (size == _layoutSize || size.x <= 0f || size.y <= 0f)
            {
                return;
            }

            foreach (RectTransform button in _buttons)
            {
                Vector2 position = button.anchoredPosition;
                button.anchoredPosition = new Vector2(position.x * size.x / _layoutSize.x, position.y * size.y / _layoutSize.y);
                Clamp(button);
            }

            _layoutSize = size;
        }

        private bool IsValid(LayoutData data)
        {
            if (data == null || data.Positions == null || data.Positions.Length != _buttons.Length ||
                !float.IsFinite(data.Scale) || data.Scale < _settings.MinimumButtonScale || data.Scale > _settings.MaximumButtonScale)
            {
                return false;
            }

            foreach (Vector2 position in data.Positions)
            {
                if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || position.x < -1f || position.x > 0f || position.y < 0f || position.y > 1f)
                {
                    return false;
                }
            }

            return true;
        }

        public void ToggleEditing()
        {
            IsEditing = !IsEditing;
            _panel.SetActive(IsEditing);
            _joystick.gameObject.SetActive(!IsEditing);
            _input.Clear();
            _editLabel.text = "배치";
            if (IsEditing)
            {
                _editLabel.text = "완료";
            }
            else
            {
                Save();
            }
        }

        public void Move(RectTransform button, Vector2 screenPoint, Camera camera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screenPoint, camera, out Vector2 point);
            button.localPosition = point;
            Clamp(button);
        }

        public void ApplyScale(float scale)
        {
            foreach (RectTransform button in _buttons)
            {
                button.localScale = Vector3.one * scale;
                Clamp(button);
            }
        }

        private void Clamp(RectTransform button)
        {
            Vector2 half = button.rect.size * button.localScale.x * 0.5f;
            Vector2 point = button.localPosition;
            point.x = Mathf.Clamp(point.x, _root.rect.xMin + half.x, _root.rect.xMax - half.x);
            point.y = Mathf.Clamp(point.y, _root.rect.yMin + half.y, _root.rect.yMax - half.y);
            button.localPosition = point;
        }

        public void Save()
        {
            LayoutData data = new LayoutData { Positions = new Vector2[_buttons.Length], Scale = _scaleSlider.value };
            for (int i = 0; i < _buttons.Length; i++)
            {
                Vector2 position = _buttons[i].anchoredPosition;
                data.Positions[i] = new Vector2(position.x / _root.rect.width, position.y / _root.rect.height);
            }

            JsonSettingsStorage.Save(SaveKey, 1, data);
        }

        public void ResetLayout()
        {
            _scaleSlider.SetValueWithoutNotify(1f);
            for (int i = 0; i < _buttons.Length; i++)
            {
                _buttons[i].anchoredPosition = _defaults[i];
                _buttons[i].localScale = Vector3.one;
            }

            Save();
        }

        public bool IsEditing { get; private set; }
    }
}
