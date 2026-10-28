using System.Text;
using TMPro;
using UnityEngine;

namespace CoinGame.UI
{
    public class PlayerAccountView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _playerIdText;
        [SerializeField]
        private TMP_Text _onlineStatusText;

        private readonly StringBuilder _sb = new();

        public void SetPlayerIdText(string playerId)
        {
            _sb.Clear();
            _sb.Append("Player ID: ");
            _sb.Append(playerId);
            _playerIdText.SetText(_sb);
        }

        public void SetOnlineStatusText(bool isOnline)
        {
            _sb.Clear();
            _sb.Append("Online Status: ");
            if (isOnline)
            {
                _sb.Append("<color=Green>Online</color>");
            }
            else
            {
                _sb.Append("<color=red>Offline</color>");
            }
            _onlineStatusText.SetText(_sb);
        }
    }

}