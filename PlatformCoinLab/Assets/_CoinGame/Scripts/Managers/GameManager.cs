using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoinGame.Managers
{
    using CoinGame.Session;
    using CoinGame.Stage;
    using CoinGame.Utilities;
    using CoinGame.Network;
    using UnityEngine.AddressableAssets;

    [Serializable]
    public enum GameScene : short
    {
        OutGame = 0,
        InGame = 1,
    }

    public class GameManager : MonoBehaviour
    {
        public const float RoundBannerFadeInDuration = 0.35f;

        [SerializeField]
        private AssetReferenceT<StageConfig> _stageConfigRef;
        [SerializeField]
        private SceneReference _inGameSceneRef;
        [SerializeField]
        private SceneReference _outGameSceneRef;
        [SerializeField]
        private float _roundStartDelay = 1f;
        [SerializeField]
        private NetworkRoundState _networkRoundState;

        private StageConfig _stageConfig;
        private StageSession _stageSession;
        private int _curRound = 1;
        private float _remainingTime;
        private double _timerEndTime;
        private CancellationTokenSource _timerCts;
        private CancellationTokenSource _roundStartCts;
        private bool _isRoundStarted;
        private bool _isRoundStarting;
        private bool _networkRoundStateBound;

        public static GameManager Instance {get; private set;}
        public event Action<StageSession, bool> OnStageSessionChanged;
        public event Action<float> OnStageTimerChanged;

        private async UniTaskVoid Awake()
        {
            _stageConfig = await _stageConfigRef.LoadAssetAsync();
            if(!_stageConfig)
            {
                Debug.LogError("Error: StageConfig를 넣으십시오.");
                return;
            }
            if(Instance == null)
            {
                Instance = this;
            }

            if (_networkRoundState == null)
            {
                _networkRoundState = FindAnyObjectByType<NetworkRoundState>();
            }
        }

        private void Update()
        {
            TryBindNetworkRoundState();
        }

        private void TryBindNetworkRoundState()
        {
            if (_networkRoundStateBound && (_networkRoundState == null || !_networkRoundState.IsSpawned))
            {
                _networkRoundStateBound = false;
                _networkRoundState = null;
            }

            if (_networkRoundStateBound)
            {
                return;
            }

            if (_networkRoundState == null)
            {
                _networkRoundState = FindAnyObjectByType<NetworkRoundState>();
            }

            if (_networkRoundState == null || !_networkRoundState.IsSpawned)
            {
                return;
            }

            _networkRoundState.StateChanged += OnNetworkRoundStateChanged;
            _networkRoundState.RoundFinished += OnNetworkRoundFinished;
            _networkRoundState.MatchFinished += OnNetworkMatchFinished;
            _networkRoundStateBound = true;
            OnNetworkRoundStateChanged(
                _networkRoundState.Round,
                _networkRoundState.TargetCoins,
                _networkRoundState.RemainingCoins,
                _networkRoundState.RemainingSeconds,
                _networkRoundState.IsRoundActive);
        }

        public void StartRound()
        {
            if (_networkRoundState != null && _networkRoundState.IsSpawned)
            {
                if (_networkRoundState.IsServer)
                {
                    _networkRoundState.StartRoundServer(_roundStartDelay);
                }
                return;
            }

            if (_isRoundStarted)
            {
                return;
            }
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.LogWarning("네트워크 연결이 완료된 뒤 라운드를 시작할 수 있습니다.", this);
                return;
            }

            _isRoundStarted = true;
            _isRoundStarting = true;
            _roundStartCts?.Cancel();
            _roundStartCts?.Dispose();
            _roundStartCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            StartRoundAsync(_roundStartCts.Token).Forget();
        }

        public async UniTask StartGameAsync()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost)
            {
                Debug.LogWarning("방장만 게임을 시작할 수 있습니다.", this);
                return;
            }

            await LoadSceneAsync(GameScene.InGame);
            await UniTask.NextFrame(cancellationToken: destroyCancellationToken);
            StartRound();
        }

        private async UniTask StartRoundAsync(CancellationToken token)
        {
            try
            {
                OnStageSessionChanged?.Invoke(_stageSession, true);
                await UniTask.Delay(TimeSpan.FromSeconds(_roundStartDelay), cancellationToken: token);
                token.ThrowIfCancellationRequested();
                _isRoundStarting = false;

                _timerCts?.Cancel();
                _timerCts?.Dispose();
                _timerCts = null;

                _stageSession = new(_curRound, _stageConfig);
                _remainingTime = _stageSession.TimeLimit;
                _timerEndTime = Time.timeAsDouble + _remainingTime;

                OnStageTimerChanged?.Invoke(_remainingTime);

                _timerCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                UpdateTimerAsync(_timerCts.Token).Forget();
            }
            catch (OperationCanceledException) { }
            finally
            {
                _isRoundStarting = false;
            }
        }

        public void NextRound()
        {
            if (_networkRoundState != null && _networkRoundState.IsSpawned)
            {
                if (_networkRoundState.IsServer)
                {
                    _networkRoundState.StartRoundServer(_roundStartDelay);
                }
                return;
            }

            ++_curRound;
            StartRound();
        }

        public void CollectStageCoin()
        {
            if (_networkRoundState != null && _networkRoundState.IsSpawned)
            {
                if (_networkRoundState.IsServer)
                {
                    _networkRoundState.CollectCoinServer();
                }
                return;
            }

            _stageSession.CollectCoin();
            OnStageSessionChanged?.Invoke(_stageSession, false);

            if (_stageSession.IsCleared)
            {
                NextRound();
                return;
            }
        }

        private async UniTaskVoid UpdateTimerAsync(CancellationToken token)
        {
            try
            {
                while (_remainingTime > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: token);
                    _remainingTime = Mathf.Max(0f, (float)(_timerEndTime - Time.timeAsDouble));
                    OnStageTimerChanged?.Invoke(_remainingTime);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                if(!token.IsCancellationRequested)
                {
                    Debug.Log("시간을 초과했습니다.");
                }
            }
        }

        public async UniTask LoadSceneAsync(GameScene gameScene)
        {
            SceneReference sceneRefToLoad = GetSceneReference(gameScene);
            if (SceneManager.GetSceneByPath(sceneRefToLoad.ScenePath).isLoaded)
            {
                return;
            }
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneRefToLoad.ScenePath, LoadSceneMode.Additive);
            if (loadOperation == null)
            {
                Debug.LogError($"Cannot load scene at path '{sceneRefToLoad.ScenePath}'. Check that the scene exists and is included in Build Settings.", this);
                return;
            }

            await loadOperation;
        }

        public async UniTask UnloadSceneAsync(GameScene gameScene)
        {
            SceneReference sceneRefToUnload = GetSceneReference(gameScene);
            if (!SceneManager.GetSceneByPath(sceneRefToUnload.ScenePath).isLoaded)
            {
                return;
            }

            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(sceneRefToUnload.ScenePath);
            if (unloadOperation == null)
            {
                Debug.LogError($"Cannot unload scene at path '{sceneRefToUnload.ScenePath}'.", this);
                return;
            }

            await unloadOperation;
        }

        private SceneReference GetSceneReference(GameScene gameScene)
        {
            return gameScene switch
            {
                GameScene.OutGame => _outGameSceneRef,
                GameScene.InGame => _inGameSceneRef,
                _ => throw new ArgumentOutOfRangeException(nameof(gameScene), gameScene, null),
            };
        }

        private void OnNetworkRoundStateChanged(int round, int targetCoins, int remainingCoins, float remainingTime, bool isActive)
        {
            if (!isActive)
            {
                if (round > 0 && targetCoins > 0 && remainingTime > 0f &&
                    _stageSession.Round != round)
                {
                    _curRound = round;
                    _stageSession = new StageSession(round, targetCoins, remainingTime, targetCoins - remainingCoins);
                    _isRoundStarting = true;
                    OnStageSessionChanged?.Invoke(_stageSession, true);
                }

                OnStageTimerChanged?.Invoke(remainingTime);
                return;
            }

            _curRound = round;
            _stageSession = new StageSession(round, targetCoins, remainingTime, targetCoins - remainingCoins);
            _isRoundStarted = true;
            _isRoundStarting = false;
            OnStageSessionChanged?.Invoke(_stageSession, true);
            OnStageTimerChanged?.Invoke(remainingTime);
        }

        private void OnNetworkRoundFinished(bool collectorWon)
        {
            OnStageTimerChanged?.Invoke(0f);
        }

        private void OnNetworkMatchFinished(bool collectorWon)
        {
            NetworkPlayerStats localStats = NetworkManager.Singleton?.SpawnManager.GetLocalPlayerObject()?.GetComponent<NetworkPlayerStats>();
            if (localStats == null)
            {
                return;
            }

            PlayerPrefsStorage.SetPlayerSessionInt(PlayerSessionKey.SCORE, localStats.Score);
            PlayerPrefsStorage.SetPlayerSessionInt(PlayerSessionKey.DISTRUPT, localStats.DisruptionScore);
            PlayerPrefsStorage.SaveAll();
        }

        private void OnDestroy()
        {
            if (_networkRoundStateBound && _networkRoundState != null)
            {
                _networkRoundState.StateChanged -= OnNetworkRoundStateChanged;
                _networkRoundState.RoundFinished -= OnNetworkRoundFinished;
                _networkRoundState.MatchFinished -= OnNetworkMatchFinished;
            }
            _roundStartCts?.Cancel();
            _roundStartCts?.Dispose();
            _timerCts?.Cancel();
            _timerCts?.Dispose();
        }

        public StageSession CurrentStageSession => _stageSession;
        public float RemainingTime => _remainingTime;
        public bool IsRoundStarted => _isRoundStarted;
        public float RoundStartDelay => _roundStartDelay;
        public bool CanPlayersMove => !IsInGameSceneLoaded || (_networkRoundState != null && _networkRoundState.IsSpawned
            ? _networkRoundState.IsRoundActive
            : _isRoundStarted && !_isRoundStarting);

        private bool IsInGameSceneLoaded => _inGameSceneRef != null &&
            !string.IsNullOrEmpty(_inGameSceneRef.ScenePath) &&
            SceneManager.GetSceneByPath(_inGameSceneRef.ScenePath).isLoaded;
    }
}
