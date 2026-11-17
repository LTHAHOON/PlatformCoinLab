using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace CoinGame.UI
{
    using CoinGame.Network;

    public class PlayerAccountView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _userNameText;

        private readonly StringBuilder _sb = new();

        private void Update()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || !NetworkManager.Singleton.IsClient)
            {
                return;
            }

            NetworkObject player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (player == null)
            {
                return;
            }

            string name = player.GetComponent<NetworkPlayerIdentity>().DisplayName;
            if (!string.IsNullOrEmpty(name) && _userNameText.text != name)
            {
                SetUserNameText(name);
            }
        }

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
