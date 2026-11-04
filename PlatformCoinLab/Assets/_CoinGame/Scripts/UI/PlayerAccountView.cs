using System.Text;
using TMPro;
using UnityEngine;

namespace CoinGame.UI
{
    public class PlayerAccountView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _userNameText;

        private readonly StringBuilder _sb = new();

        public void SetUserNameText(string userName)
        {
            if (_userNameText == null)
            {
                return;
            }

            _sb.Append(userName);
            _userNameText.SetText(_sb);
            _sb.Clear();
        }
    }

}
