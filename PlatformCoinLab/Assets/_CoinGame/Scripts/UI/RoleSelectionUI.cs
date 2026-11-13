using Unity.Netcode;
using UnityEngine;

namespace CoinGame.UI
{
    using CoinGame.Network;
    using CoinGame.Player;

    public class RoleSelectionUI : MonoBehaviour
    {
        private NetworkPlayerStats _localStats;
        private float _nextLocalStatsLookup;

        private void Update()
        {
            if (Time.unscaledTime < _nextLocalStatsLookup)
            {
                return;
            }

            _nextLocalStatsLookup = Time.unscaledTime + 0.25f;
            if ((_localStats == null || !_localStats.IsSpawned) && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
            {
                NetworkObject localObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                if (localObject != null)
                {
                    _localStats = localObject.GetComponent<NetworkPlayerStats>();
                }
            }
        }

        private void OnGUI()
        {
            if (_localStats == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                return;
            }

            if (NetworkRoundState.Instance == null || !NetworkRoundState.Instance.IsRoleSelectionOpen ||
                _localStats.Role != PlayerRole.Unselected)
            {
                return;
            }

            float width = Mathf.Min(Screen.width * 0.9f, 560f);
            float height = Mathf.Min(Screen.height * 0.32f, 250f);
            float x = (Screen.width - width) * 0.5f;
            float y = Mathf.Max(24f, (Screen.height - height) * 0.5f);
            Rect panel = new(x, y, width, height);
            GUI.Box(panel, "Choose your role");
            GUI.Label(new Rect(panel.x + 24f, panel.y + 42f, width - 48f, 30f), "All players are ready.");
            GUI.Label(new Rect(panel.x + 24f, panel.y + 70f, width - 48f, 30f), "Select Collector or Disruptor.");

            float buttonWidth = (width - 72f) * 0.5f;
            float buttonY = panel.y + height - 78f;
            if (GUI.Button(new Rect(panel.x + 24f, buttonY, buttonWidth, 54f), "Collector"))
            {
                _localStats.SelectRole(PlayerRole.Collector);
            }
            if (GUI.Button(new Rect(panel.x + 48f + buttonWidth, buttonY, buttonWidth, 54f), "Disruptor"))
            {
                _localStats.SelectRole(PlayerRole.Disruptor);
            }
        }
    }
}
