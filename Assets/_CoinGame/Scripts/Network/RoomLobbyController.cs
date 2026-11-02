using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Services.Multiplayer.Components;
using UnityEngine;
using CoinGame.Managers;
using CoinGame.Player;

namespace CoinGame.Network
{
    public sealed class RoomLobbyController : MonoBehaviour
    {
        private const int DefaultMaxPlayers = 2;
        private const int MaxSupportedPlayers = 8;
        private const int StartCountdownSeconds = 10;

        public static RoomLobbyController Instance { get; private set; }

        [SerializeField]
        private RoomLobbyView _view;
        [SerializeField]
        private RoomStartCountdown _countdown;
        [SerializeField]
        private Canvas _outGameCanvas;
        [SerializeField]
        private NetworkRoundState _networkRoundStatePrefab;

        private SessionConnector _sessionConnector;
        private MultiplayerSession _multiplayerSession;
        private ISession _session;
        private bool _servicesReady;
        private bool _sessionReady;
        private bool _isHost;
        private bool _gameStarting;
        private int _maxCollectors = 1;
        private int _maxDisruptors = 1;
        private int _maxRounds = 5;
        private int _coinMultiplier = 2;

        public int MaxCollectors => _maxCollectors;
        public int MaxDisruptors => _maxDisruptors;
        public int MaxRounds => _maxRounds;
        public int CoinMultiplier => _coinMultiplier;
        public bool IsHost => _isHost;
        public bool IsSessionReady => _sessionReady;
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
            PrepareLobbySceneAsync().Forget();
            InitializeServicesAsync().Forget();
        }

        private async UniTaskVoid PrepareLobbySceneAsync()
        {
            await UniTask.Yield(PlayerLoopTiming.Update);
            if (GameManager.Instance != null)
            {
                await GameManager.Instance.UnloadSceneAsync(GameScene.InGame);
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
                        _maxDisruptors);
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

        private async UniTaskVoid InitializeServicesAsync()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    await UnityServices.InitializeAsync();
                }
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                _servicesReady = true;
                _view?.SetStatus("Create a room or join with a Room ID.");
                _view?.SetButtonsInteractable(true);
            }
            catch (Exception exception)
            {
                _view?.SetStatus($"Service initialization failed: {exception.Message}");
                Debug.LogException(exception, this);
            }
        }

        private void CreateRoom()
        {
            if (!TryReadRoomSettings(out RoomSettings settings))
            {
                return;
            }
            ConnectSessionAsync(settings, true).Forget();
        }

        private void JoinRoom()
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

            // Hide the create/join form immediately while the client is connecting.
            _view?.SetPanelVisible(false);
            ConnectSessionAsync(new RoomSettings(roomId, string.Empty, DefaultMaxPlayers, 1, 1, 5, 2), false).Forget();
        }

        private async UniTaskVoid ConnectSessionAsync(RoomSettings settings, bool createOrJoin)
        {
            if (!_servicesReady || _sessionReady)
            {
                return;
            }

            _view?.SetButtonsInteractable(false);
            _view?.SetStatus(createOrJoin ? "Creating room..." : "Joining room...");

            try
            {
                await ResetNetworkManagerAsync();
            }
            catch (Exception exception)
            {
                OnSessionFailed($"Network reset failed: {exception.Message}");
                return;
            }

            if (_sessionConnector != null)
            {
                _sessionConnector.SuccessfulExecution.RemoveListener(OnSessionConnected);
                _sessionConnector.FailedExecution.RemoveListener(OnSessionFailed);
                Destroy(_sessionConnector);
                _sessionConnector = null;
            }
            if (_multiplayerSession != null)
            {
                Destroy(_multiplayerSession);
                _multiplayerSession = null;
            }

            _multiplayerSession = ScriptableObject.CreateInstance<MultiplayerSession>();
            _sessionConnector = ScriptableObject.CreateInstance<SessionConnector>();
            _sessionConnector.MultiplayerSession = _multiplayerSession;
            _sessionConnector.SuccessfulExecution.AddListener(OnSessionConnected);
            _sessionConnector.FailedExecution.AddListener(OnSessionFailed);

            if (createOrJoin)
            {
                Dictionary<string, SessionProperty> properties = new()
                {
                    ["collectors"] = new SessionProperty(settings.MaxCollectors.ToString(), VisibilityPropertyOptions.Public, PropertyIndex.String1),
                    ["disruptors"] = new SessionProperty(settings.MaxDisruptors.ToString(), VisibilityPropertyOptions.Public, PropertyIndex.String2),
                    ["maxRounds"] = new SessionProperty(settings.MaxRounds.ToString(), VisibilityPropertyOptions.Public, PropertyIndex.String3),
                    ["coinMultiplier"] = new SessionProperty(settings.CoinMultiplier.ToString(), VisibilityPropertyOptions.Public, PropertyIndex.String4),
                };
                SessionOptions sessionOptions = new()
                {
                    Name = settings.RoomName,
                    MaxPlayers = settings.MaxPlayers,
                    SessionProperties = properties,
                };
                _sessionConnector.WithCreateOrJoin(settings.RoomId)
                    .WithSessionOptions(sessionOptions)
                    .WithRelayNetworkOptions(RelayNetworkOptions.Default, RelayProtocol.Default)
                    .Execute();
            }
            else
            {
                _sessionConnector.WithJoinById(settings.RoomId).Execute();
            }

            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        private async UniTask ResetNetworkManagerAsync()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening)
            {
                return;
            }

            networkManager.Shutdown();
            await UniTask.WaitUntil(() => !networkManager.IsListening, cancellationToken: destroyCancellationToken);
        }

        private void OnSessionConnected(ISession session)
        {
            _session = session;
            _sessionReady = true;
            _isHost = session.IsHost;
            ReadSessionProperties(session);
            EnsureNetworkRoundStateAsync().Forget();
            WaitForNetworkAsync(session).Forget();
        }

        private async UniTaskVoid EnsureNetworkRoundStateAsync()
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
                        _maxDisruptors);
                }
                NetworkRoundState.Instance.ReportReadyRpc();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTaskVoid WaitForNetworkAsync(ISession session)
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
        }

        private void OnSessionFailed(string message)
        {
            _sessionReady = false;
            _isHost = false;
            _session = null;
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

        private async UniTaskVoid StartGameAsync()
        {
            _gameStarting = true;
            _view?.SetPanelVisible(false);
            if (_outGameCanvas != null)
            {
                _outGameCanvas.gameObject.SetActive(false);
            }

            if (GameManager.Instance == null)
            {
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
            settings = default;
            if (!_servicesReady || _view == null)
            {
                _view?.SetStatus("Please wait for services to initialize.");
                return false;
            }

            string roomId = _view.RoomId;
            string roomName = _view.RoomName;
            if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(roomName))
            {
                _view.SetStatus("Enter a room name and Room ID.");
                return false;
            }
            if (!int.TryParse(_view.MaxPlayers, out int maxPlayers) || maxPlayers < 1 || maxPlayers > MaxSupportedPlayers)
            {
                _view.SetStatus($"Max Players must be between 1 and {MaxSupportedPlayers}.");
                return false;
            }
            if (!int.TryParse(_view.MaxCollectors, out int maxCollectors) || maxCollectors < 0 || maxCollectors > maxPlayers)
            {
                _view.SetStatus("Check Max Collectors.");
                return false;
            }
            if (!int.TryParse(_view.MaxDisruptors, out int maxDisruptors) || maxDisruptors < 0 || maxDisruptors > maxPlayers)
            {
                _view.SetStatus("Check Max Disruptors.");
                return false;
            }
            if (maxCollectors + maxDisruptors != maxPlayers)
            {
                _view.SetStatus("Max Collectors + Max Disruptors must equal Max Players.");
                return false;
            }
            if (!int.TryParse(_view.MaxRounds, out int maxRounds) || maxRounds < 1 || maxRounds > 20)
            {
                _view.SetStatus("Max Rounds must be between 1 and 20.");
                return false;
            }
            if (!int.TryParse(_view.CoinMultiplier, out int coinMultiplier) || coinMultiplier < 1 || coinMultiplier > 5)
            {
                _view.SetStatus("Coin multiplier must be between 1 and 5.");
                return false;
            }

            _maxCollectors = maxCollectors;
            _maxDisruptors = maxDisruptors;
            _maxRounds = maxRounds;
            _coinMultiplier = coinMultiplier;
            settings = new RoomSettings(roomId, roomName, maxPlayers, maxCollectors, maxDisruptors, maxRounds, coinMultiplier);
            return true;
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
            if (Instance == this) Instance = null;
            ISession session = _multiplayerSession?.Session;
            if (session != null)
            {
                if (session.IsHost) session.AsHost().DeleteAsync();
                else session.LeaveAsync();
            }
            if (_sessionConnector != null)
            {
                _sessionConnector.SuccessfulExecution.RemoveListener(OnSessionConnected);
                _sessionConnector.FailedExecution.RemoveListener(OnSessionFailed);
                Destroy(_sessionConnector);
            }
            if (_multiplayerSession != null) Destroy(_multiplayerSession);
        }

        private readonly struct RoomSettings
        {
            public readonly string RoomId;
            public readonly string RoomName;
            public readonly int MaxPlayers;
            public readonly int MaxCollectors;
            public readonly int MaxDisruptors;
            public readonly int MaxRounds;
            public readonly int CoinMultiplier;

            public RoomSettings(string roomId, string roomName, int maxPlayers, int maxCollectors, int maxDisruptors, int maxRounds, int coinMultiplier)
            {
                RoomId = roomId;
                RoomName = roomName;
                MaxPlayers = maxPlayers;
                MaxCollectors = maxCollectors;
                MaxDisruptors = maxDisruptors;
                MaxRounds = maxRounds;
                CoinMultiplier = coinMultiplier;
            }
        }
    }
}
