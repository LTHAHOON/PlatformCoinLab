using System;
using UnityEngine;

namespace CoinGame.Player
{
    using CoinGame.UI;

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
                if (OnlineStatusChanged != null)
                {
                    OnlineStatusChanged.Invoke(value);
                }
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogError("PlayerAccount??????肉???롪돌筌?鈺곕똻???곷튊 ??몃빍??", this);
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
                Debug.LogWarning("PlayerAccountView揶쎛 ?怨뚭퍙??? ??녿릭??щ빍??", this);
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

