using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoinGame.UI
{
    using CoinGame.Player;

    public sealed class PlayerNamePopup : MonoBehaviour
    {
        [SerializeField]
        private GameObject _panel;
        [SerializeField]
        private TMP_InputField _input;
        [SerializeField]
        private Button _confirm;
        [SerializeField]
        private PlayerNameSettings _settings;

        private void OnEnable()
        {
            _panel.SetActive(true);
            _input.characterLimit = _settings.MaxLength;
            _input.text = LocalPlayerProfile.DisplayName;
            _input.onValueChanged.AddListener(Validate);
            _input.onSubmit.AddListener(Submit);
            _confirm.onClick.AddListener(Confirm);
            Validate(_input.text);
        }

        private void Start()
        {
            _input.Select();
            _input.ActivateInputField();
        }

        private void OnDisable()
        {
            _input.onValueChanged.RemoveListener(Validate);
            _input.onSubmit.RemoveListener(Submit);
            _confirm.onClick.RemoveListener(Confirm);
        }

        private void Validate(string value)
        {
            _confirm.interactable = _settings.Normalize(value).Length > 0;
        }

        private void Submit(string value)
        {
            Confirm();
        }

        private void Confirm()
        {
            string name = _settings.Normalize(_input.text);
            if (name.Length == 0)
            {
                return;
            }

            LocalPlayerProfile.SaveName(name);
            _panel.SetActive(false);
        }
    }
}
