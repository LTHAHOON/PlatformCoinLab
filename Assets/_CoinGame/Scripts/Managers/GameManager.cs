using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoinGame.Managers
{
    using CoinGame.Session;
    using CoinGame.Stage;
    using CoinGame.Utilities;

    [Serializable]
    public enum GameScene : short
    {
        OutGame = 0,
        InGame = 1,
    }

    public class GameManager : MonoBehaviour
    {
        [SerializeField]
        private StageConfig _stageConfig;
        [SerializeField]
        private SceneReference _inGameSceneRef;
        [SerializeField]
        private SceneReference _outGameSceneRef;
        [SerializeField]
        private float _roundStartDelay = 1f;
        
        private StageSession _stageSession;
        private int _curRound = 1;
        private float _remainingTime;
        private double _timerEndTime;
        private CancellationTokenSource _timerCts;
        private CancellationTokenSource _roundStartCts;

        public static GameManager Instance {get; private set;}
        public event Action<StageSession, bool> OnStageSessionChanged;
        public event Action<float> OnStageTimerChanged;

        private void Awake()
        {
            if(!_stageConfig)
            {
                Debug.LogError("Error: StageConfig를 넣으십시오.");
                return;
            }
            if(Instance == null)
            {
                Instance = this;
            }
            StartRound();
        }

        public void StartRound()
        {
            _roundStartCts?.Cancel();
            _roundStartCts?.Dispose();
            _roundStartCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            StartRoundAsync(_roundStartCts.Token).Forget();
        }

        private async UniTask StartRoundAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_roundStartDelay), cancellationToken: token);
                token.ThrowIfCancellationRequested();

                _timerCts?.Cancel();
                _timerCts?.Dispose();
                _timerCts = null;

                _stageSession = new(_curRound, _stageConfig);
                _remainingTime = _stageSession.TimeLimit;
                _timerEndTime = Time.timeAsDouble + _remainingTime;
                OnStageSessionChanged?.Invoke(_stageSession, true);
                OnStageTimerChanged?.Invoke(_remainingTime);

                _timerCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                UpdateTimerAsync(_timerCts.Token).Forget();
            }
            catch (OperationCanceledException) { }
        }

        public void NextRound()
        {
            ++_curRound;
            StartRound();
        }

        public void CollectStageCoin()
        {
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

        public StageSession CurrentStageSession => _stageSession;
        public float RemainingTime => _remainingTime;

        private void OnDestroy()
        {
            _roundStartCts?.Cancel();
            _roundStartCts?.Dispose();
            _timerCts?.Cancel();
            _timerCts?.Dispose();
        }
    }
}
