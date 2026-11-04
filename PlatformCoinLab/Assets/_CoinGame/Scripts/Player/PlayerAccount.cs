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
        private static PlayerAccount _instance;

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
            if (_instance != null && _instance != this)
            {
                Debug.LogError("PlayerAccount는 한 씬에 하나만 존재해야 합니다.", this);
                enabled = false;
                return;
            }

            _instance = this;
            if (_id == Guid.Empty)
            {
                _id = Guid.NewGuid();
            }

            if (_playerAccountView != null)
            {
                _playerAccountView.SetUserNameText(_id.ToString());
            }
            else
            {
                Debug.LogWarning("PlayerAccountView가 연결되지 않았습니다.", this);
            }

            IsOnline = false;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                IsOnline = false;
            }
        }

        public static Guid ID => _id;
    }
}

