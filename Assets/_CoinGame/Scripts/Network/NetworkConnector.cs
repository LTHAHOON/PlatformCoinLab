using Unity.Netcode;
using UnityEngine;

using CoinGame.Player;
using CoinGame.Utilities;
using CoinGame.UI;

namespace CoinGame.Network
{
    public class NetworkConnector : MonoBehaviour
    {
        [SerializeField]
        private Spawner _defaultPlayerSpawner;
        [SerializeField]
        private bool _autoConnectOnStart = true;

        private void OnConnectedCallBack(ulong clientId)
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsServer || _defaultPlayerSpawner == null)
            {
                return;
            }
            if (!networkManager.ConnectedClients.ContainsKey(clientId))
            {
                return;
            }

            _defaultPlayerSpawner.SpawnPlayer(clientId);
        }

        private void Start()
        {
            gameObject.AddComponent<RoleSelectionUI>();
            if (_autoConnectOnStart)
            {
                if (Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor)
                {
                    ConnectHost();
                }
                else
                {
                    ConnectClient();
                }
            }
        }

        private void OnDisconnectedCallBack(ulong clientId)
        {
            if (_defaultPlayerSpawner == null)
            {
                return;
            }

            _defaultPlayerSpawner.CancelSpawnPlayer(clientId);
        }

        public void ConnectHost()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || networkManager.IsListening || networkManager.IsClient)
            {
                Debug.Log("서버가 이미 열려있거나 NetworkManager가 NULL입니다.");
                return;
            }

            networkManager.OnClientConnectedCallback -= OnConnectedCallBack;
            networkManager.OnClientDisconnectCallback -= OnDisconnectedCallBack;
            networkManager.OnClientConnectedCallback += OnConnectedCallBack;
            networkManager.OnClientDisconnectCallback += OnDisconnectedCallBack;

            PlayerAccount.IsOnline = networkManager.StartHost();
            if (!PlayerAccount.IsOnline)
            {
                Debug.Log("서버가 이미 열려있습니다.");
                return;
            }

        }

        public void ConnectClient()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || networkManager.IsListening || networkManager.IsServer)
            {
                Debug.Log("이미 클라이언트 접속한 상태이거나 서버가 열려있지 않은 상태이거나 서버 소유권을 가지고 있습니다.");
                return;
            }

            PlayerAccount.IsOnline = networkManager.StartClient();
            if (!PlayerAccount.IsOnline)
            {
                Debug.Log("서버가 열려있지 않습니다.");
            }
        }

        public void DisConnectClient()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening || networkManager.IsServer)
            {
                Debug.Log("클라이언트 접속하지 않은 상태입니다.");
                return;
            }

            networkManager.Shutdown();
            PlayerAccount.IsOnline = false;
        }

        private void OnDestroy()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback -= OnConnectedCallBack;
                networkManager.OnClientDisconnectCallback -= OnDisconnectedCallBack;
            }

        }
    }


}
