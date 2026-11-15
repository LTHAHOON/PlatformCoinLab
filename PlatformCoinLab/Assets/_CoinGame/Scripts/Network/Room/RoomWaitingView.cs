using TMPro;
using UnityEngine;

namespace CoinGame.Network
{
    public sealed class RoomWaitingView : MonoBehaviour
    {
        [SerializeField]
        private RoomStartCountdown _countdown;
        [SerializeField]
        private TMP_Text _statusText;

        private void Awake()
        {
            if (RoomLobbyController.Instance != null)
            {
                RoomLobbyController.Instance.BindWaitingView(this);
            }
        }

        private void OnDestroy()
        {
            if (RoomLobbyController.Instance != null)
            {
                RoomLobbyController.Instance.UnbindWaitingView(this);
            }
        }

        public void ShowRemainingSeconds(int seconds)
        {
            _countdown.ShowRemainingSeconds(seconds);
        }

        public void Cancel()
        {
            _countdown.Cancel();
        }

        public void SetStatus(string message)
        {
            _statusText.text = message;
        }
    }
}