using System;
using UnityEngine;

namespace CoinGame.Utilities
{
    [Serializable]
    public class JsonSaveEnvelope<T>
    {
        public int Version;
        public T Data;
    }

    public static class JsonSettingsStorage
    {
        public static bool TryLoad<T>(string key, int version, out T value, Func<T, bool> validate = null)
        {
            value = default;
            string json = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }
            try
            {
                JsonSaveEnvelope<T> envelope = JsonUtility.FromJson<JsonSaveEnvelope<T>>(json);
                if (envelope == null)
                {
                    return false;
                }

                if (envelope.Version != version)
                {
                    return false;
                }

                if (validate != null && !validate(envelope.Data))
                {
                    return false;
                }
                value = envelope.Data;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"JSON 설정을 읽지 못했습니다: {exception.Message}");
                return false;
            }
        }

        public static void Save<T>(string key, int version, T value)
        {
            JsonSaveEnvelope<T> envelope = new() { Version = version, Data = value };
            PlayerPrefs.SetString(key, JsonUtility.ToJson(envelope));
            PlayerPrefs.Save();
        }
    }
}
