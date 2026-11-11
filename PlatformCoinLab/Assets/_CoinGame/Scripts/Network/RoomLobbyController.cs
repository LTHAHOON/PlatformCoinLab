using System;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using CoinGame.Managers;
using CoinGame.Player;

namespace CoinGame.Network
{
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
        private string _contentServerHealthUrl = "https://localhost:5443/health";

        private RoomSessionService _sessionService;
        private ISession _session;
        private bool _servicesReady;
        private bool _sessionReady;
        private bool _isHost;
        private bool _gameStarting;
        private bool _roomActionInProgress;
        private bool _isShuttingDown;
        private bool _lifecycleInitialized;
        private RoomLobbyContentService _contentService;
        private int _maxCollectors = 1;
        private int _maxDisruptors = 1;
        private int _maxRounds = 5;
        private int _coinMultiplier = 1;

        public int MaxCollectors => _maxCollectors;
        public int MaxDisruptors => _maxDisruptors;
        public int MaxRounds => _maxRounds;
        public int CoinMultiplier => _coinMultiplier;
        public bool IsHost => _isHost;
        public bool IsSessionReady => _sessionReady;
        public bool IsRoomActionInProgress => _roomActionInProgress;
        public static bool IsDevelopmentQuickStartActive { get; set; }
        public int SessionPlayerCount => _session?.PlayerCount ?? 0;
        public int SessionMaxPlayers => _session?.MaxPlayers ?? 0;
        public bool IsRoomFull
        {
            get
            {
                if (!_sessionReady || _session == null || _session.MaxPlayers <= 0)
                {
                    return false;
                }

                int networkPlayerCount = NetworkManager.Singleton != null
                    ? NetworkManager.Singleton.ConnectedClientsIds.Count
                    : 0;
                return _session.PlayerCount >= _session.MaxPlayers || networkPlayerCount >= _session.MaxPlayers;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _sessionService = new RoomSessionService();
            _sessionService.Connected += OnSessionConnected;
            _sessionService.Failed += OnSessionFailed;
            _contentService = new RoomLobbyContentService(_contentServerHealthUrl);
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null)
            {
                // Scene transitions are local and gated by the shared lobby countdown.
                networkManager.NetworkConfig.EnableSceneManagement = false;
            }

            if (_view == null)
            {
                _view = GetComponentInChildren<RoomLobbyView>(true);
            }
            if (_countdown == null)
            {
                _countdown = GetComponentInChildren<RoomStartCountdown>(true);
            }
        }

        private void OnEnable()
        {
            if (_view != null)
            {
                _view.CreateRequested += CreateRoom;
                _view.JoinRequested += JoinRoom;
                _view.Initialize();
            }
            if (_countdown != null)
            {
                _countdown.Completed += OnCountdownCompleted;
            }
        }

        private void Start()
        {
            InitializeRuntime();
        }

        public void InitializeRuntime()
        {
            if (_lifecycleInitialized)
            {
                return;
            }

            _lifecycleInitialized = true;
            PrepareLobbySceneAsync().Forget();
            InitializeServicesAsync(
                IsDevelopmentQuickStartActive ? DevQuickStart.DevelopmentAuthProfile : null,
                IsDevelopmentQuickStartActive && DevQuickStart.LogAuthentication).Forget();
        }

        private async UniTask PrepareLobbySceneAsync()
        {
            await UniTask.Yield(PlayerLoopTiming.Update);
            if (IsDevelopmentQuickStartActive)
            {
                return;
            }

            if (GameManager.Instance != null)
            {
                await GameManager.Instance.UnloadSceneAsync(GameScene.InGame);
            }
        }

        public async UniTask<bool> StartDevelopmentSessionAsync(RoomSettings settings, bool createOrJoin)
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

                ApplySettings(settings);
                _view?.SetPanelVisible(false);
                ConnectSessionAsync(settings, createOrJoin).Forget();
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
            _view?.SetPanelVisible(false);
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
                        _maxCollectors,
                        _maxDisruptors,
                        IsDevelopmentQuickStartActive ? 0f : 10f);
                }

                if (state.LoadGame)
                {
                    StartGameAsync().Forget();
                    return;
                }

                if (state.LobbyStartTime > 0d && NetworkManager.Singleton != null)
                {
                    int remaining = Mathf.CeilToInt((float)(state.LobbyStartTime - NetworkManager.Singleton.ServerTime.Time));
                    _countdown?.ShowRemainingSeconds(remaining);
                    _view?.SetStatus("All players are ready. Waiting for the synchronized start.");
                }
            }
            else if (_countdown != null && _countdown.IsRunning)
            {
                _countdown.Cancel();
                _view?.SetStatus("Waiting for all players to join.");
            }
        }

        private async UniTask InitializeServicesAsync(string authenticationProfile, bool logPlayerId)
        {
            await _sessionService.InitializeServicesAsync(
                destroyCancellationToken, authenticationProfile, logPlayerId);
            _servicesReady = _sessionService.IsServicesReady;
            if (_servicesReady)
            {
                _view?.SetStatus("Create a room or join with a Room ID.");
                _view?.SetButtonsInteractable(true);
            }
        }

        private void CreateRoom()
        {
            CreateRoomAsync().Forget();
        }

        private void ApplySettings(RoomSettings settings)
        {
            _maxCollectors = settings.MaxCollectors;
            _maxDisruptors = settings.MaxDisruptors;
            _maxRounds = settings.MaxRounds;
            _coinMultiplier = settings.CoinMultiplier;
        }

        private void JoinRoom()
        {
            JoinRoomAsync().Forget();
        }

        private async UniTask CreateRoomAsync()
        {
            if (!TryBeginRoomAction())
            {
                return;
            }

            try
            {
                if (!_servicesReady)
                {
                    _view?.SetStatus("Please wait for services to initialize.");
                    return;
                }

                if (!await _contentService.IsAvailableAsync(destroyCancellationToken))
                {
                    return;
                }

                if (TryReadRoomSettings(out RoomSettings settings))
                {
                    ApplySettings(settings);
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
            if (!TryBeginRoomAction())
            {
                return;
            }

            try
            {
                if (!_servicesReady)
                {
                    _view?.SetStatus("Please wait for services to initialize.");
                    return;
                }

                string roomId = _view != null ? _view.JoinRoomId : string.Empty;
                if (string.IsNullOrWhiteSpace(roomId))
                {
                    _view?.SetStatus("Enter a Room ID to join.");
                    return;
                }

                if (!await _contentService.IsAvailableAsync(destroyCancellationToken))
                {
                    return;
                }

                // Hide the create/join form immediately while the client is connecting.
                _view?.SetPanelVisible(false);
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
        private async UniTask ConnectSessionAsync(RoomSettings settings, bool createOrJoin)
        {
            if (!_servicesReady || _sessionReady)
            {
                return;
            }

            _view?.SetButtonsInteractable(false);
            _view?.SetStatus(createOrJoin ? "Creating room..." : "Joining room...");
            await _sessionService.ConnectAsync(settings, createOrJoin, destroyCancellationToken);
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
            EnsureNetworkRoundStateAsync().Forget();
            WaitForNetworkAsync(session).Forget();
        }

        private async UniTask EnsureNetworkRoundStateAsync()
        {
            try
            {
                await UniTask.WaitUntil(
                    () => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening,
                    cancellationToken: destroyCancellationToken);

                if (_isHost && NetworkRoundState.Instance == null && _networkRoundStatePrefab != null)
                {
                    NetworkRoundState state = Instantiate(_networkRoundStatePrefab);
                    NetworkObject networkObject = state.GetComponent<NetworkObject>();
                    if (networkObject == null)
                    {
                        Destroy(state.gameObject);
                        throw new InvalidOperationException("NetworkRoundState prefab requires a NetworkObject component.");
                    }

                    if (!networkObject.IsSpawned)
                    {
                        networkObject.Spawn();
                    }
                }

                await UniTask.WaitUntil(
                    () => NetworkRoundState.Instance != null && NetworkRoundState.Instance.IsSpawned,
                    cancellationToken: destroyCancellationToken);

                if (_isHost)
                {
                    NetworkRoundState.Instance.ConfigureServer(_maxRounds, _coinMultiplier);
                    NetworkRoundState.Instance.ConfigureLobbyServer(
                        SessionMaxPlayers > 0 ? SessionMaxPlayers : DefaultMaxPlayers,
                        _maxCollectors,
                        _maxDisruptors,
                        IsDevelopmentQuickStartActive ? 0f : 10f);
                }
                NetworkRoundState.Instance.ReportReadyRpc();
            }
            catch (OperationCanceledException)
            {
            }
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
                await UniTask.WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient, cancellationToken: destroyCancellationToken);
                PlayerAccount.IsOnline = true;
                _view?.SetPanelVisible(false);
                _view?.SetStatus(_isHost ? $"Room created: {session.Id}\nWaiting for players." : "Connected to room. Waiting for the host to start the game.");
            }
            catch (OperationCanceledException)
            {
            }
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
            _view?.SetButtonsInteractable(true);
            _view?.SetPanelVisible(true);
            _view?.SetStatus($"Room connection failed: {message}");
        }

        private void ReadSessionProperties(ISession session)
        {
            if (session.Properties.TryGetValue("collectors", out SessionProperty collectors) && int.TryParse(collectors.Value, out int maxCollectors))
            {
                _maxCollectors = maxCollectors;
            }
            if (session.Properties.TryGetValue("disruptors", out SessionProperty disruptors) && int.TryParse(disruptors.Value, out int maxDisruptors))
            {
                _maxDisruptors = maxDisruptors;
            }
            if (session.Properties.TryGetValue("maxRounds", out SessionProperty maxRounds) && int.TryParse(maxRounds.Value, out int rounds))
            {
                _maxRounds = Mathf.Clamp(rounds, 1, 20);
            }
            if (session.Properties.TryGetValue("coinMultiplier", out SessionProperty coinMultiplier) && int.TryParse(coinMultiplier.Value, out int multiplier))
            {
                _coinMultiplier = Mathf.Clamp(multiplier, 1, 5);
            }
        }

        private void OnCountdownCompleted()
        {
            if (!_sessionReady || !IsRoomFull || _gameStarting)
            {
                return;
            }
            if (_isHost)
            {
                NetworkRoundState.Instance?.AssignMissingRolesServer(_maxCollectors, _maxDisruptors);
            }
            StartGameAsync().Forget();
        }

        private async UniTask StartGameAsync()
        {
            _gameStarting = true;
            _view?.SetPanelVisible(false);
            if (_outGameCanvas != null)
            {
                _outGameCanvas.gameObject.SetActive(false);
            }

            if (GameManager.Instance == null)
            {
                _gameStarting = false;
                _view?.SetPanelVisible(true);
                _view?.SetStatus("Game manager is unavailable.");
                return;
            }

            await GameManager.Instance.LoadSceneAsync(GameScene.InGame);
            if (NetworkRoundState.Instance != null && NetworkRoundState.Instance.IsSpawned)
            {
                NetworkRoundState.Instance.ReportSceneLoadedRpc();
            }
            else
            {
                GameManager.Instance.StartRound();
            }
        }
        private bool TryReadRoomSettings(out RoomSettings settings)
        {
            return RoomSettingsValidator.TryRead(_view, out settings);
        }

        private void OnDisable()
        {
            if (_view != null)
            {
                _view.CreateRequested -= CreateRoom;
                _view.JoinRequested -= JoinRoom;
            }
            if (_countdown != null)
            {
                _countdown.Completed -= OnCountdownCompleted;
            }
        }

        private void OnDestroy()
        {
            _isShuttingDown = true;
            if (Instance == this) Instance = null;
            if (_sessionService == null)
            {
                return;
            }
            _sessionService.Connected -= OnSessionConnected;
            _sessionService.Failed -= OnSessionFailed;
            _sessionService.Dispose();
        }
    }
}
