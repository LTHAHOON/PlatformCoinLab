using System;
using UnityEngine;

namespace CoinGame.Managers
{
    public sealed class ApplicationLifecycleService : MonoBehaviour
    {
        public static event Action<bool> FocusChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            GameObject root = new("ApplicationLifecycleService");
            root.AddComponent<ApplicationLifecycleService>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                PlayerPrefs.Save();
            }

            FocusChanged?.Invoke(!paused);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                PlayerPrefs.Save();
            }

            FocusChanged?.Invoke(focused);
        }
    }
}
