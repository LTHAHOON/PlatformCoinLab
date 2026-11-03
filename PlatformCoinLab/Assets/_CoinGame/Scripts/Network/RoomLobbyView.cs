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

        public string RoomName => _roomNameInput != null ? _roomNameInput.text.Trim() : string.Empty;
        public string RoomId => _roomIdInput != null ? _roomIdInput.text.Trim() : string.Empty;
        public string JoinRoomId => _joinRoomIdInput != null ? _joinRoomIdInput.text.Trim() : string.Empty;
        public string MaxPlayers => _maxPlayersInput != null ? _maxPlayersInput.text.Trim() : string.Empty;
        public string MaxCollectors => _maxCollectorsInput != null ? _maxCollectorsInput.text.Trim() : string.Empty;
        public string MaxDisruptors => _maxDisruptorsInput != null ? _maxDisruptorsInput.text.Trim() : string.Empty;
        public string MaxRounds => _maxRoundsInput != null ? _maxRoundsInput.text.Trim() : string.Empty;
        public string CoinMultiplier => _coinMultiplierInput != null ? _coinMultiplierInput.text.Trim() : string.Empty;

        public void Initialize()
        {
            if (_createButton != null)
            {
                _createButton.onClick.RemoveListener(OnCreateClicked);
                _createButton.onClick.AddListener(OnCreateClicked);
            }

            if (_joinButton != null)
            {
                _joinButton.onClick.RemoveListener(OnJoinClicked);
                _joinButton.onClick.AddListener(OnJoinClicked);
            }

            if (_optionsButton != null)
            {
                _optionsButton.onClick.RemoveListener(ToggleOptions);
                _optionsButton.onClick.AddListener(ToggleOptions);
            }

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
            if (_panel != null)
            {
                _panel.SetActive(visible);
            }
        }

        public void SetButtonsInteractable(bool interactable)
        {
            if (_createButton != null) _createButton.interactable = interactable;
            if (_joinButton != null) _joinButton.interactable = interactable;
        }

        public void SetStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }
        }

        public void SetCountdown(string message)
        {
            if (_countdownText != null)
            {
                _countdownText.text = message;
                _countdownText.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
            }
        }

        public void CopyCreateRoomId(string roomId)
        {
            if (_roomIdInput != null)
            {
                _roomIdInput.text = roomId;
            }
        }

        private void OnCreateClicked()
        {
            CreateRequested?.Invoke();
        }

        private void OnJoinClicked()
        {
            if (_roomIdInput != null && _joinRoomIdInput != null)
            {
                _roomIdInput.text = _joinRoomIdInput.text;
            }

            JoinRequested?.Invoke();
        }

        private void ToggleOptions()
        {
            if (_optionsPanel != null)
            {
                _optionsPanel.SetActive(!_optionsPanel.activeSelf);
            }
        }
    }
}
