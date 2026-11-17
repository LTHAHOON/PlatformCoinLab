using System;
using UnityEngine;

namespace CoinGame.Player
{
    public static class LocalPlayerProfile
    {
        private static string _displayName;
        public static event Action NameChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            _displayName = null;
            NameChanged = null;
        }

        public static void SaveName(string name)
        {
            _displayName = name;
            PlayerPrefs.SetString(PlayerProfileStorage.NameKey, name);
            PlayerPrefs.Save();
            NameChanged?.Invoke();
        }

        public static string DisplayName
        {
            get
            {
                if (_displayName == null)
                {
                    _displayName = PlayerPrefs.GetString(PlayerProfileStorage.NameKey, string.Empty);
                }

                return _displayName;
            }
        }
    }
}
