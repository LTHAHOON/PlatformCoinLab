using Unity.Netcode;
using UnityEngine;


namespace CoinGame.Network
{
    using CoinGame.Player;
    using CoinGame.Utilities;
    using CoinGame.UI;

    public class NetworkConnector : MonoBehaviour
    {
        public static NetworkConnector Instance { get; private set; }

        [SerializeField]
        private NetworkSpawner _defaultPlayerSpawner;

        public bool TryRelocatePlayers()
        {
            return _defaultPlayerSpawner != null && _defaultPlayerSpawner.TryRelocatePlayers();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnConnectedCallBack(ulong clientId)
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                return;
            }
            if (clientId == networkManager.LocalClientId)
            {
                PlayerAccount.IsOnline = true;
            }
            if (!networkManager.IsServer || _defaultPlayerSpawner == null)
            {
                return;
            }
            if (!networkManager.ConnectedClients.ContainsKey(clientId))
            {
                return;
            }
            if (networkManager.ConnectedClients[clientId].PlayerObject != null)
            {
                return;
            }

            _defaultPlayerSpawner.SpawnPlayer(clientId);
        }

        private void Start()
        {
            if (GetComponent<RoleSelectionUI>() == null)
            {
                gameObject.AddComponent<RoleSelectionUI>();
            }
            SubscribeNetworkCallbacks();
        }

        private void SubscribeNetworkCallbacks()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                return;
            }

            networkManager.OnClientConnectedCallback -= OnConnectedCallBack;
            networkManager.OnClientDisconnectCallback -= OnDisconnectedCallBack;
            networkManager.OnClientConnectedCallback += OnConnectedCallBack;
            networkManager.OnClientDisconnectCallback += OnDisconnectedCallBack;
        }

        private void OnDisconnectedCallBack(ulong clientId)
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null && clientId == networkManager.LocalClientId)
            {
                PlayerAccount.IsOnline = false;
            }
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
                Debug.Log("?쒕쾭媛 ?대? ?대젮?덇굅??NetworkManager媛 NULL?낅땲??");
                return;
            }

            SubscribeNetworkCallbacks();

            PlayerAccount.IsOnline = networkManager.StartHost();
            if (!PlayerAccount.IsOnline)
            {
                Debug.Log("?쒕쾭媛 ?대? ?대젮?덉뒿?덈떎.");
                return;
            }

        }

        public void ConnectClient()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || networkManager.IsListening || networkManager.IsServer)
            {
                Debug.Log("?대? ?대씪?댁뼵???묒냽???곹깭?닿굅???쒕쾭媛 ?대젮?덉? ?딆? ?곹깭?닿굅???쒕쾭 ?뚯쑀沅뚯쓣 媛吏怨??덉뒿?덈떎.");
                return;
            }

            SubscribeNetworkCallbacks();
            PlayerAccount.IsOnline = networkManager.StartClient();
            if (!PlayerAccount.IsOnline)
            {
                Debug.Log("?쒕쾭媛 ?대젮?덉? ?딆뒿?덈떎.");
            }
        }

        public void DisConnectClient()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening || networkManager.IsServer)
            {
                Debug.Log("?대씪?댁뼵???묒냽?섏? ?딆? ?곹깭?낅땲??");
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

            if (Instance == this)
            {
                Instance = null;
            }

        }
    }


}
