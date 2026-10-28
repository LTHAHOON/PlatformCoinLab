using System;
using UnityEngine;
using CoinGame.UI;

namespace CoinGame.Player
{
    public class PlayerAccount : MonoBehaviour
    {
        [SerializeField]
        private PlayerAccountView _playerAccountView;

        public static event Action<bool> OnlineStatusChanged;
        private static Guid _id;
        private static bool _isOnline;

        public static bool IsOnline
        {
            get => _isOnline;
            set
            {
                _isOnline = value;
                OnlineStatusChanged?.Invoke(value);
            }
        }

        private void Awake()
        {
            OnlineStatusChanged = _playerAccountView.SetOnlineStatusText;
            IsOnline = false;
            _id = Guid.NewGuid();
            _playerAccountView.SetPlayerIdText(_id.ToString());
        }

        public static Guid ID => _id;
    }
}

