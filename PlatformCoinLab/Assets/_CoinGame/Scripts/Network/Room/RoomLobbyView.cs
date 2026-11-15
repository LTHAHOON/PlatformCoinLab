using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoinGame.Network
{
    public sealed class RoomLobbyView : MonoBehaviour
    {
        [SerializeField]
        private GameObject _panel;
        [SerializeField]
        private TMP_InputField _roomNameInput;
        [SerializeField]
        private TMP_InputField _roomIdInput;
        [SerializeField]
        private TMP_InputField _joinRoomIdInput;
        [SerializeField]
        private TMP_InputField _maxPlayersInput;
        [SerializeField]
        private TMP_InputField _maxCollectorsInput;
        [SerializeField]
        private TMP_InputField _maxDisruptorsInput;
        [SerializeField]
        private Button _createButton;
        [SerializeField]
        private Button _joinButton;
        [SerializeField]
        private Button _optionsButton;
        [SerializeField]
        private GameObject _optionsPanel;
        [SerializeField]
        private TMP_InputField _maxRoundsInput;
        [SerializeField]
        private TMP_InputField _coinMultiplierInput;
        [SerializeField]
        private TMP_Text _statusText;
        [SerializeField]
        private TMP_Text _countdownText;

        public event Action CreateRequested;
        public event Action JoinRequested;


        public void Initialize()
        {
            _createButton.onClick.RemoveListener(OnCreateClicked);
            _createButton.onClick.AddListener(OnCreateClicked);

            _joinButton.onClick.RemoveListener(OnJoinClicked);
            _joinButton.onClick.AddListener(OnJoinClicked);

            _optionsButton.onClick.RemoveListener(ToggleOptions);
            _optionsButton.onClick.AddListener(ToggleOptions);

            if (_optionsPanel != null)
            {
                _optionsPanel.SetActive(false);
            }

            SetPanelVisible(true);
            SetButtonsInteractable(false);
            SetCountdown(string.Empty);
        }

        public void SetPanelVisible(bool visible)
        {
            _panel.SetActive(visible);
        }

        public void SetButtonsInteractable(bool interactable)
        {
            _createButton.interactable = interactable;
            _joinButton.interactable = interactable;
        }

        public void SetStatus(string message)
        {
            _statusText.text = message;
        }

        public void SetCountdown(string message)
        {
            _countdownText.text = message;
            _countdownText.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
        }

        public void CopyCreateRoomId(string roomId)
        {
            _roomIdInput.text = roomId;
        }

        private void OnCreateClicked()
        {
            if (CreateRequested != null)
            {
                CreateRequested.Invoke();
            }
        }

        private void OnJoinClicked()
        {
            if (_roomIdInput != null && _joinRoomIdInput != null)
            {
                _roomIdInput.text = _joinRoomIdInput.text;
            }
            if (JoinRequested != null)
            {
                JoinRequested.Invoke();
            }
        }

        private void ToggleOptions()
        {
            _optionsPanel.SetActive(!_optionsPanel.activeSelf);
        }

        private static string ReadInput(TMP_InputField input)
        {
            if (!input)
            {
                return string.Empty;
            }

            return input.text.Trim();
        }

        public string RoomName => ReadInput(_roomNameInput);
        public string RoomId => ReadInput(_roomIdInput);
        public string JoinRoomId => ReadInput(_joinRoomIdInput);
        public string MaxPlayers => ReadInput(_maxPlayersInput);
        public string MaxCollectors => ReadInput(_maxCollectorsInput);
        public string MaxDisruptors => ReadInput(_maxDisruptorsInput);
        public string MaxRounds => ReadInput(_maxRoundsInput);
        public string CoinMultiplier => ReadInput(_coinMultiplierInput);

    }
}
