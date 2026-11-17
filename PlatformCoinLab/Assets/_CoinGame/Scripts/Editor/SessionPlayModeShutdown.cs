using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEditor;

namespace CoinGame.Editor
{
    using CoinGame.Network;

    [InitializeOnLoad]
    public static class SessionPlayModeShutdown
    {
        static SessionPlayModeShutdown()
        {
            EditorApplication.playModeStateChanged += OnStateChanged;
        }

        private static void OnStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode)
            {
                return;
            }

            if (RoomLobbyController.Instance != null && RoomLobbyController.Instance.HasActiveSession)
            {
                RoomLobbyController.Instance.ShutdownSessionAsync(true).Forget();
            }

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                return;
            }

            // Discard queued gameplay messages before NGO's automatic quit
            // callback performs its normal synchronous transport cleanup.
            NetworkManager.Singleton.Shutdown(true);
        }
    }
}
