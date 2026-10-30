using Unity.Netcode;
using UnityEngine;
using CoinGame.Network;
using CoinGame.Player;

namespace CoinGame.UI
{
    public class RoleSelectionUI : MonoBehaviour
    {
        private NetworkPlayerStats _localStats;

        private void Update()
        {
            if (_localStats == null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
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

            if (_localStats.MatchPlayerCount < 2 || _localStats.Role != PlayerRole.Unselected)
            {
                return;
            }

            const float width = 360f;
            const float height = 210f;
            Rect panel = new((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(panel, "역할 선택");
            GUI.Label(new Rect(panel.x + 24f, panel.y + 38f, width - 48f, 30f), "두 플레이어가 접속했습니다.");
            GUI.Label(new Rect(panel.x + 24f, panel.y + 66f, width - 48f, 30f), "한 명은 수집가, 한 명은 방해자입니다.");

            if (GUI.Button(new Rect(panel.x + 24f, panel.y + 112f, 145f, 58f), "Coin 수집가"))
            {
                _localStats.SelectRole(PlayerRole.Collector);
            }
            if (GUI.Button(new Rect(panel.x + 191f, panel.y + 112f, 145f, 58f), "방해자"))
            {
                _localStats.SelectRole(PlayerRole.Disruptor);
            }
        }
    }
}
