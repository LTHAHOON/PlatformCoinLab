using System.Text;
using TMPro;
using UnityEngine;

namespace CoinGame.UI
{
    public class PlayerAccountView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _userNameText;
        [SerializeField]
        private TMP_Text _onlineStatusText;

        private readonly StringBuilder _sb = new();

        public void SetUserNameText(string userName)
        {
            _sb.Append(userName);
            _userNameText.SetText(_sb);
            _sb.Clear();
        }

        public void SetOnlineStatusText(bool isOnline)
        {
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
            _sb.Clear();
        }
    }

}