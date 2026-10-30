using System;
using UnityEngine;

namespace CoinGame.Utilities
{
    public static class PlayerSessionKey
    {
        public const string SCORE = "CoinGame.Sessions.Score";
    }

    public static class PlayerPrefsStorage
    {
        public static void SaveAll()
        {
            PlayerPrefs.Save();
        }

        public static void SetPlayerSessionInt(string id, int value)
        {
            PlayerPrefs.SetInt(id, value);
        }

        public static void SetPlayerSessionFloat(string id, float value)
        {
            PlayerPrefs.SetFloat(id, value);
        }

        public static void SetPlayerSessionString(string id, string value)
        {
            PlayerPrefs.SetString(id, value);
        }

        public static int GetOrInitPlayerSessionInt(string id, int defaultValue = 0)
        {
            int value = PlayerPrefs.GetInt(id, defaultValue);
            return value;
        }

        public static float GetOrInitPlayerSessionFloat(string id, float defaultValue = 0f)
        {
            float value = PlayerPrefs.GetFloat(id, defaultValue);
            return value; 
        }

        public static string GetPlayerSessionString(string id, string defaultValue = "")
        {
            string value = PlayerPrefs.GetString(id, defaultValue);
            return value;
        }
    }
}
