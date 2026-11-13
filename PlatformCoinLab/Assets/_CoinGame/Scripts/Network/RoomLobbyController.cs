using System;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using Unity.Services.Multiplayer.Components;
using UnityEngine;

namespace CoinGame.Network
{
    using CoinGame.Managers;
    using CoinGame.Player;

    public sealed class RoomLobbyController : MonoBehaviour
    {
        private const int DefaultMaxPlayers = 2;
        public static RoomLobbyController Instance { get; private set; }

        [SerializeField]
        private RoomLobbyView _view;
        [SerializeField]
        private RoomStartCountdown _countdown;
        [SerializeField]
        private Canvas _outGameCanvas;
        [SerializeField]
        private NetworkRoundState _networkRoundStatePrefab;
        [SerializeField]
        private MultiplayerSession _multiplayerSession;
        [SerializeField]
        private SessionConnector _sessionConnector;
        [SerializeField]
        private string _contentServerHealthUrl = "https://localhost:5443/health";

        private RoomSessionService _sessionService;
        private ISession _session;
        private bool _servicesReady;
        private bool _sessionReady;
        private bool _isHost;
        private bool _gameStarting;
        private bool _roomActionInProgress;
        private bool _isShuttingDown;
        private RoomLobbyContentService _contentService;
        private RoomSettings _roomSettings;

        public static bool IsDevelopmentQuickStartActive { get; set; }
        public bool IsRoomFull
        {
            get
            {
                if (!_sessionReady || _session == null || _session.MaxPlayers <= 0 || !NetworkManager.Singleton)
                {
                    return false;
                }

                int networkPlayerCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
                return _session.PlayerCount >= _session.MaxPlayers || networkPlayerCount >= _session.MaxPlayers;
            }
        }

        private void Awake()
        {
            Instance = this;
            _sessionService = new RoomSessionService(_multiplayerSession, _sessionConnector);
            _sessionService.Connected += OnSessionConnected;
            _sessionService.Failed += OnSessionFailed;
            _contentService = new RoomLobbyContentService(_contentServerHealthUrl);
        }

        private void OnEnable()
        {
            if (_view != null)
            {
                _view.CreateRequested += CreateRoom;
                _view.JoinRequested += JoinRoom;
                _view.Initialize();
            }
        }

        private void Start()
        {
            InitializeServicesAsync(
                ResolveAuthenticationProfile(),
                IsDevelopmentQuickStartActive && DevQuickStart.LogAuthentication).Forget();
        }

        private string ResolveAuthenticationProfile()
        {
            if (IsDevelopmentQuickStartActive)
            {
                return DevQuickStart.DevelopmentAuthProfile;
            }

#if UNITY_EDITOR
            return DevQuickStart.DevelopmentAuthProfile;
#else
            return "Profile";
#endif
        }

        public async UniTask<bool> StartDevelopmentSessionAsync(RoomSettings settings, bool createAndJoin)
        {
            if (!TryBeginRoomAction())
            {
                return false;
            }

            try
            {
                await UniTask.WaitUntil(() => _servicesReady, cancellationToken: destroyCancellationToken);
                if (!await _contentService.IsAvailableAsync(destroyCancellationToken))
                {
                    _roomActionInProgress = false;
                    return false;
                }

                _roomSettings = settings;
                _view.SetPanelVisible(false);
                ConnectSessionAsync(settings, createAndJoin).Forget();
                return true;
            }
            catch (OperationCanceledException)
            {
                _roomActionInProgress = false;
                return false;
            }
        }

        public void HideLobbyUi()
        {
            _view.SetPanelVisible(false);
            if (_outGameCanvas != null)
            {
                _outGameCanvas.gameObject.SetActive(false);
            }
        }

        public void ShutdownDevelopmentSession()
        {
            if (IsDevelopmentQuickStartActive)
            {
                _sessionService?.DisposeAsync().Forget();
            }
        }

        private void Update()
        {
            if (!_sessionReady || _session == null || _gameStarting)
            {
                return;
            }

            NetworkRoundState state = NetworkRoundState.Instance;
            if (IsRoomFull && state != null && state.IsSpawned)
            {
                if (_isHost)
                {
                    state.ConfigureLobbyServer(
                        SessionMaxPlayers > 0 ? SessionMaxPlayers : DefaultMaxPlayers,
                        _roomSettings.MaxCollectors,
                        _roomSettings.MaxDisruptors,
                        IsDevelopmentQuickStartActive ? 0f : 10f);
                }

                if (state.LoadGame)
                {
                    StartGameAsync().Forget();
                    return;
                }

                if (state.LobbyStartTime > 0d)
                {
                    int remaining = Mathf.CeilToInt((float)(state.LobbyStartTime - NetworkManager.Singleton.ServerTime.Time));
                    if (_countdown != null)
                    {
                        _countdown.ShowRemainingSeconds(remaining);
                    }
                    _view.SetStatus("All players are ready. Waiting for the synchronized start.");
                }
            }
            else if (_countdown != null && _countdown.IsRunning)
            {
                _countdown.Cancel();
                _view.SetStatus("Waiting for all players to join.");
            }
        }

        private async UniTask InitializeServicesAsync(string authenticationProfile, bool logPlayerId)
        {
            await _sessionService.InitializeServicesAsync(
                destroyCancellationToken, authenticationProfile, logPlayerId);
            _servicesReady = _sessionService.IsServicesReady;
            if (_servicesReady)
            {
                _view.SetStatus("Create and join a room, or join with a Room ID.");
                _view.SetButtonsInteractable(true);
            }
        }

        private void CreateRoom()
        {
            CreateRoomAsync().Forget();
        }

        private void JoinRoom()
        {
            JoinRoomAsync().Forget();
        }

        private async UniTask CreateRoomAsync()
        {
            if (!TryBeginRoomAction() || !_view)
            {
                return;
            }

            try
            {
                if (!_servicesReady)
                {
                    _view.SetStatus("Please wait for services to initialize.");
                    return;
                }

                if (!await _contentService.IsAvailableAsync(destroyCancellationToken))
                {
                    return;
                }

                if (RoomSettingsValidator.TryRead(_view, out RoomSettings settings))
                {
                    _roomSettings = settings;
                    ConnectSessionAsync(settings, true).Forget();
                }
            }
            finally
            {
                _roomActionInProgress = false;
            }
        }

        private async UniTask JoinRoomAsync()
        {
            if (!TryBeginRoomAction() || !_view)
            {
                return;
            }

            try
            {
                if (!_servicesReady)
                {
                    _view.SetStatus("Please wait for services to initialize.");
                    return;
                }

                string roomId = _view.JoinRoomId;
                if (string.IsNullOrWhiteSpace(roomId))
                {
                    _view.SetStatus("Enter a Room ID to join.");
                    return;
                }

                if (!await _contentService.IsAvailableAsync(destroyCancellationToken))
                {
                    return;
                }

                _view.SetPanelVisible(false);
                ConnectSessionAsync(new RoomSettings(roomId, string.Empty, DefaultMaxPlayers, 1, 1, 5, 1), false).Forget();
            }
            finally
            {
                _roomActionInProgress = false;
            }
        }

        private bool TryBeginRoomAction()
        {
            if (_roomActionInProgress || _sessionReady || _gameStarting)
            {
                return false;
            }

            _roomActionInProgress = true;
            return true;
        }

        private async UniTask ConnectSessionAsync(RoomSettings settings, bool isCreate)
        {
            if (!_servicesReady || _sessionReady || !_view)
            {
                return;
            }

            _view.SetButtonsInteractable(false);
            _view.SetStatus(isCreate ? "Creating room..." : "Joining room...");

            await _sessionService.ConnectAsync(settings, isCreate, destroyCancellationToken);
        }

        private void OnSessionConnected(ISession session)
        {
            if (_isShuttingDown)
            {
                return;
            }

            _session = session;
            _sessionReady = true;
            _isHost = session.IsHost;
            _roomActionInProgress = false;
            ReadSessionProperties(session);
            CreateNetworkRoundStateAsync().Forget();
            WaitForNetworkAsync(session).Forget();
        }

        private async UniTask CreateNetworkRoundStateAsync()
        {
            if(!NetworkRoundState.Instance || _networkRoundStatePrefab)
            {
                return;
            }
            try
            {
                await UniTask.WaitUntil(() => NetworkManager.Singleton.IsListening, cancellationToken: destroyCancellationToken);

                //이때 NetworkRoundState Prefab 생성
                if (_isHost)
                {
                    NetworkRoundState state = Instantiate(_networkRoundStatePrefab);
                    NetworkObject networkObject = state.GetComponent<NetworkObject>();
                    if (!networkObject)
                    {
                        Destroy(state.gameObject);
                        throw new InvalidOperationException("NetworkRoundState prefab requires a NetworkObject component.");
                    }

                    networkObject.Spawn();
                }

                await UniTask.WaitUntil(() => NetworkRoundState.Instance.IsSpawned, cancellationToken: destroyCancellationToken);

                if (_isHost)
                {
                    NetworkRoundState.Instance.ConfigureServer(
                        _roomSettings.MaxRounds,
                        _roomSettings.CoinMultiplier);

                    NetworkRoundState.Instance.ConfigureLobbyServer(
                        SessionMaxPlayers > 0 ? SessionMaxPlayers : DefaultMaxPlayers,
                        _roomSettings.MaxCollectors,
                        _roomSettings.MaxDisruptors,
                        IsDevelopmentQuickStartActive ? 0f : 10f);
                }
                NetworkRoundState.Instance.ReportReadyRpc();
            }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                OnSessionFailed($"Network state setup failed: {exception.Message}");
            }
        }

        private async UniTask WaitForNetworkAsync(ISession session)
        {
            try
            {
                await UniTask.WaitUntil(() => NetworkManager.Singleton.IsConnectedClient, cancellationToken: destroyCancellationToken);
                PlayerAccount.IsOnline = true;
                if (_view != null)
                {
                    _view.SetPanelVisible(false);
                }
                if (_view != null)
                {
                    _view.SetStatus(_isHost ? $"Room created: {session.Id}\nWaiting for players." : "Connected to room. Waiting for the host to start the game.");
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                OnSessionFailed($"Network connection failed: {exception.Message}");
            }
        }

        private void OnSessionFailed(string message)
        {
            if (_isShuttingDown)
            {
                return;
            }

            _sessionReady = false;
            _isHost = false;
            _session = null;
            _roomActionInProgress = false;
            _view.SetButtonsInteractable(true);
            _view.SetPanelVisible(true);
            _view.SetStatus($"Room connection failed: {message}");
        }

        private void ReadSessionProperties(ISession session)
        {
            int maxCollectors = _roomSettings.MaxCollectors;
            int maxDisruptors = _roomSettings.MaxDisruptors;
            int maxRounds = _roomSettings.MaxRounds;
            int coinMultiplier = _roomSettings.CoinMultiplier;

            if (session.Properties.TryGetValue("collectors", out SessionProperty collectors) &&
                int.TryParse(collectors.Value, out int sessionCollectors))
            {
                maxCollectors = sessionCollectors;
            }
            if (session.Properties.TryGetValue("disruptors", out SessionProperty disruptors) &&
                int.TryParse(disruptors.Value, out int sessionDisruptors))
            {
                maxDisruptors = sessionDisruptors;
            }
            if (session.Properties.TryGetValue("maxRounds", out SessionProperty maxRoundsProperty) &&
                int.TryParse(maxRoundsProperty.Value, out int sessionRounds))
            {
                maxRounds = Mathf.Clamp(sessionRounds, 1, 20);
            }
            if (session.Properties.TryGetValue("coinMultiplier", out SessionProperty multiplierProperty) &&
                int.TryParse(multiplierProperty.Value, out int sessionMultiplier))
            {
                coinMultiplier = Mathf.Clamp(sessionMultiplier, 1, 5);
            }

            _roomSettings = new RoomSettings(
                _roomSettings.RoomId,
                _roomSettings.RoomName,
                _roomSettings.MaxPlayers,
                maxCollectors,
                maxDisruptors,
                maxRounds,
                coinMultiplier);
        }

        private async UniTask StartGameAsync()
        {
            _gameStarting = true;
            _view.SetPanelVisible(false);
            if (_outGameCanvas != null)
            {
                _outGameCanvas.gameObject.SetActive(false);
            }

            if (GameManager.Instance == null)
            {
                _gameStarting = false;
                _view.SetPanelVisible(true);
                _view.SetStatus("Game manager is unavailable.");
                return;
            }

            await GameManager.Instance.LoadSceneAsync(GameScene.InGame);
            if (NetworkRoundState.Instance != null && NetworkRoundState.Instance.IsSpawned)
            {
                NetworkRoundState.Instance.ReportSceneLoadedRpc();
                return;
            }

            GameManager.Instance.StartRound();
        }

        private void OnDisable()
        {
            if (_view != null)
            {
                _view.CreateRequested -= CreateRoom;
                _view.JoinRequested -= JoinRoom;
            }
        }

        private void OnDestroy()
        {
            _isShuttingDown = true;
            if (Instance == this)
            {
                Instance = null;
            }
            if (_sessionService == null)
            {
                return;
            }
            _sessionService.Connected -= OnSessionConnected;
            _sessionService.Failed -= OnSessionFailed;
            _sessionService.Dispose();
        }

        public RoomSettings Settings => _roomSettings;
        public bool IsHost => _isHost;
        public bool IsSessionReady => _sessionReady;
        public bool IsRoomActionInProgress => _roomActionInProgress;
        public int SessionPlayerCount => _session != null ? _session.PlayerCount : 0;
        public int SessionMaxPlayers => _session != null ? _session.MaxPlayers : 0;

    }
}
