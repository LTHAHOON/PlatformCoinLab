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
                Debug.LogError("Error: StageConfig???節뚯몵?????");
                return;
            }
            if(Instance == null)
            {
                Instance = this;
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
                Debug.LogWarning("??쎈뱜??곌쾿 ?怨뚭퍙???袁⑥┷??????깆뒲??? ??뽰삂??????됰뮸??덈뼄.", this);
                return;
            }

            _isRoundStarted = true;
            _isRoundStarting = true;
            if (_roundStartCts != null)
            {
                _roundStartCts.Cancel();
            }
            if (_roundStartCts != null)
            {
                _roundStartCts.Dispose();
            }
            _roundStartCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            StartRoundAsync(_roundStartCts.Token).Forget();
        }

        public async UniTask StartGameAsync()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost)
            {
                Debug.LogWarning("獄쎻뫗?ｏ쭕?野껊슣?????뽰삂??????됰뮸??덈뼄.", this);
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
                if (OnStageSessionChanged != null)
                {
                    OnStageSessionChanged.Invoke(_stageSession, true);
                }
                await UniTask.Delay(TimeSpan.FromSeconds(_roundStartDelay), cancellationToken: token);
                token.ThrowIfCancellationRequested();
                _isRoundStarting = false;

                if (_timerCts != null)
            {
                _timerCts.Cancel();
            }
                if (_timerCts != null)
            {
                _timerCts.Dispose();
            }
                _timerCts = null;

                _stageSession = new(_curRound, _stageConfig);
                _remainingTime = _stageSession.TimeLimit;
                _timerEndTime = Time.timeAsDouble + _remainingTime;

                if (OnStageTimerChanged != null)

                {

                    OnStageTimerChanged.Invoke(_remainingTime);

                }

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
            if (OnStageSessionChanged != null)
            {
                OnStageSessionChanged.Invoke(_stageSession, false);
            }

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
                    if (OnStageTimerChanged != null)
                    {
                        OnStageTimerChanged.Invoke(_remainingTime);
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                if(!token.IsCancellationRequested)
                {
                    Debug.Log("??볦퍢???λ뜃???됰뮸??덈뼄.");
                }
            }
        }

        public async UniTask LoadSceneAsync(GameScene gameScene)
        {
            SceneReference sceneRefToLoad = GetSceneReference(gameScene);
            if (SceneManager.GetSceneByPath(sceneRefToLoad.ScenePath).isLoaded)
            {
                if (gameScene == GameScene.InGame)
                {
                    ConfigureInGameSceneServices();
                }
                return;
            }
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneRefToLoad.ScenePath, LoadSceneMode.Additive);
            if (loadOperation == null)
            {
                Debug.LogError($"Cannot load scene at path '{sceneRefToLoad.ScenePath}'. Check that the scene exists and is included in Build Settings.", this);
                return;
            }

            await loadOperation;
            if (gameScene == GameScene.InGame)
            {
                ConfigureInGameSceneServices();
            }
        }

        public void ConfigureInGameSceneServices()
        {
            if (_inGameSceneRef == null || string.IsNullOrEmpty(_inGameSceneRef.ScenePath))
            {
                return;
            }

            Scene inGameScene = SceneManager.GetSceneByPath(_inGameSceneRef.ScenePath);
            if (!inGameScene.isLoaded)
            {
                return;
            }

            bool persistentLoaded = _outGameSceneRef != null &&
                SceneManager.GetSceneByPath(_outGameSceneRef.ScenePath).isLoaded;
            if (!persistentLoaded)
            {
                return;
            }

            Scene persistentScene = SceneManager.GetSceneByPath(_outGameSceneRef.ScenePath);
            foreach (GameObject root in persistentScene.GetRootGameObjects())
            {
                if (root.name == "Main Camera")
                {
                    root.SetActive(false);
                }
            }

            foreach (GameObject root in inGameScene.GetRootGameObjects())
            {
                if (root.name == "EventSystem")
                {
                    root.SetActive(false);
                }
            }
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
            if (gameScene == GameScene.InGame)
            {
                RestoreOutGameSceneServices();
            }
        }

        private void RestoreOutGameSceneServices()
        {
            if (_outGameSceneRef == null || string.IsNullOrEmpty(_outGameSceneRef.ScenePath))
            {
                return;
            }

            Scene persistentScene = SceneManager.GetSceneByPath(_outGameSceneRef.ScenePath);
            if (!persistentScene.isLoaded)
            {
                return;
            }

            foreach (GameObject root in persistentScene.GetRootGameObjects())
            {
                if (root.name == "Main Camera")
                {
                    root.SetActive(true);
                }
            }
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
                    if (OnStageSessionChanged != null)
                    {
                        OnStageSessionChanged.Invoke(_stageSession, true);
                    }
                }

                if (OnStageTimerChanged != null)

                {

                    OnStageTimerChanged.Invoke(remainingTime);

                }
                return;
            }

            _curRound = round;
            _stageSession = new StageSession(round, targetCoins, remainingTime, targetCoins - remainingCoins);
            _isRoundStarted = true;
            _isRoundStarting = false;
            if (OnStageSessionChanged != null)
            {
                OnStageSessionChanged.Invoke(_stageSession, true);
            }
            if (OnStageTimerChanged != null)
            {
                OnStageTimerChanged.Invoke(remainingTime);
            }
        }

        private void OnNetworkRoundFinished(bool collectorWon)
        {
            if (OnStageTimerChanged != null)
            {
                OnStageTimerChanged.Invoke(0f);
            }
        }

        private void OnNetworkMatchFinished(bool collectorWon)
        {
            NetworkPlayerStats localStats = null;
            if (NetworkManager.Singleton != null)
            {
                NetworkObject localObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                if (localObject != null)
                {
                    localStats = localObject.GetComponent<NetworkPlayerStats>();
                }
            }
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
            if (_roundStartCts != null)
            {
                _roundStartCts.Cancel();
            }
            if (_roundStartCts != null)
            {
                _roundStartCts.Dispose();
            }
            if (_timerCts != null)
            {
                _timerCts.Cancel();
            }
            if (_timerCts != null)
            {
                _timerCts.Dispose();
            }
        }

        public bool CanPlayersMove => !IsInGameSceneLoaded || (_networkRoundState != null && _networkRoundState.IsSpawned
            ? _networkRoundState.IsRoundActive
            : _isRoundStarted && !_isRoundStarting);

        public bool IsInGameSceneLoaded => _inGameSceneRef != null &&
            !string.IsNullOrEmpty(_inGameSceneRef.ScenePath) &&
            SceneManager.GetSceneByPath(_inGameSceneRef.ScenePath).isLoaded;

        public StageSession CurrentStageSession => _stageSession;
        public float RemainingTime => _remainingTime;
        public bool IsRoundStarted => _isRoundStarted;
        public float RoundStartDelay => _roundStartDelay;

    }
}
