using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using CoinGame.Player;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CoinGame.Network
{
    public sealed class DevQuickStart : MonoBehaviour
    {
        private const string PersistentScenePath =
            "Assets/_CoinGame/Scenes/Persistent_CoinGameScene.unity";

        [SerializeField]
        private bool _enabled = true;
        [SerializeField]
        private bool _soloTestMode = true;
        [SerializeField]
        private string _roomId = "coin-dev-quickstart";
        [SerializeField]
        private string _roomName = "Coin Dev Quick Start";
        [SerializeField]
        private int _maxRounds = 5;
        [SerializeField]
        private float _retrySeconds = 3f;
        [SerializeField]
        private bool _logAuthentication = true;

        private bool _isDirectLaunch;
        private bool _hasBegun;
        private CancellationTokenSource _startCts;

        public bool ShouldRun => _enabled && _isDirectLaunch;
        public static string DevelopmentAuthProfile =>
            Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor
                ? "CoinDevHost"
                : "CoinDevClient";
        public static bool LogAuthentication { get; private set; }

        private void Awake()
        {
#if !UNITY_EDITOR
            if (!Debug.isDebugBuild)
            {
                _enabled = false;
            }
#endif
            _isDirectLaunch = !SceneManager.GetSceneByPath(PersistentScenePath).isLoaded;
            LogAuthentication = _logAuthentication && _isDirectLaunch;
            RoomLobbyController.IsDevelopmentQuickStartActive = ShouldRun;
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (_isDirectLaunch && state == PlayModeStateChange.ExitingPlayMode)
            {
                RoomLobbyController.Instance?.ShutdownDevelopmentSession();
            }
        }
#endif

        public void Begin()
        {
            if (!ShouldRun || _hasBegun)
            {
                return;
            }

            _hasBegun = true;
            _startCts?.Cancel();
            _startCts?.Dispose();
            _startCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            RunAsync(_startCts.Token).Forget();
        }

        private async UniTaskVoid RunAsync(CancellationToken token)
        {
            try
            {
                await UniTask.WaitUntil(
                    () => RoomLobbyController.Instance != null,
                    cancellationToken: token);

                int maxPlayers = _soloTestMode ? 1 : 2;
                int maxDisruptors = _soloTestMode ? 0 : 1;
                string roomId = _soloTestMode ? $"{_roomId}-solo" : _roomId;
                RoomSettings settings = new(
                    roomId, _roomName, maxPlayers, 1, maxDisruptors,
                    Mathf.Clamp(_maxRounds, 1, 20), 1);
                bool createOrJoin = Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor;
                RoomLobbyController controller = RoomLobbyController.Instance;
                controller.HideLobbyUi();

                while (!controller.IsSessionReady)
                {
                    if (!controller.IsRoomActionInProgress)
                    {
                        await controller.StartDevelopmentSessionAsync(settings, createOrJoin);
                    }

                    float waitSeconds = controller.IsRoomActionInProgress
                        ? 1f
                        : Mathf.Max(1f, _retrySeconds);
                    await UniTask.Delay(TimeSpan.FromSeconds(waitSeconds), cancellationToken: token);
                }

                await SelectRoleWhenReadyAsync(createOrJoin, _soloTestMode, token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTask SelectRoleWhenReadyAsync(bool isHost, bool soloTestMode, CancellationToken token)
        {
            await UniTask.WaitUntil(
                () => NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient,
                cancellationToken: token);
            await UniTask.WaitUntil(
                () => NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null,
                cancellationToken: token);

            NetworkPlayerStats stats = NetworkManager.Singleton.SpawnManager
                .GetLocalPlayerObject().GetComponent<NetworkPlayerStats>();
            if (stats != null)
            {
                PlayerRole role = soloTestMode || isHost
                    ? PlayerRole.Collector
                    : PlayerRole.Disruptor;
                stats.SelectRole(role);
            }
        }

        private void OnDestroy()
        {
            _startCts?.Cancel();
            _startCts?.Dispose();
            if (_isDirectLaunch)
            {
                RoomLobbyController.IsDevelopmentQuickStartActive = false;
                LogAuthentication = false;
            }
        }
    }
}
