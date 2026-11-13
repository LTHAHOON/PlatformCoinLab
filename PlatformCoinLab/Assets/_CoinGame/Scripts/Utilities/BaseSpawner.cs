using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

namespace CoinGame.Utilities
{
    using CoinGame.Managers;
    using CoinGame.Session;

    // Kept together to preserve serialized fields and Unity lifecycle callbacks.
    public class BaseSpawner : MonoBehaviour
    {
        [SerializeField]
        protected AssetReferenceGameObject _spawnPrefabReference;

        [SerializeField]
        [HideInInspector]
        [FormerlySerializedAs("_spawnPrefab")]
        protected GameObject _legacySpawnPrefab;

        [SerializeField]
        protected Transform _spawnParent;

        [SerializeField]
        protected bool _onAwakeSpawn;

        [SerializeField]
        protected bool _syncSpawnCountWithStageSession;

        [SerializeField]
        protected bool _isDefaultPlayerSpawner;

        [SerializeField]
        protected bool _isLoop;

        [SerializeField]
        [Range(1, 100)]
        protected int _spawnCount = 10;

        [SerializeField]
        protected float _spawnFirstDelay = 1f;

        [SerializeField]
        protected float _spawnDelay = 1f;

        [SerializeField]
        protected Vector3 _spawnArea = new(5f, 5f, 5f);

        [SerializeField]
        protected bool _drawArea;

        [SerializeField]
        protected float _checkRadius = 1f;

        [SerializeField]
        protected bool _drawCheckRadius;

        [SerializeField]
        protected LayerMask _blockingLayerMask;

        [SerializeField]
        protected bool _canUseObjectPool;

        protected GameObject _spawnPrefab;
        protected GameObjectPool _objectPool;
        protected CancellationTokenSource _spawnCts;
        protected List<CancellationToken> _spawnedTokens;
        protected AsyncOperationHandle<GameObject> _spawnPrefabHandle;
        protected UniTask _spawnPrefabInitializationTask;
        protected const int MaxIterations = 50;
        private bool _stageSessionBound;
        private int _lastSpawnedStageRound = -1;


        protected virtual void Awake()
        {
            _spawnPrefabInitializationTask = InitializeSpawnPrefabAsync(destroyCancellationToken).Preserve();
            _spawnPrefabInitializationTask.Forget();
        }

        private async UniTask InitializeSpawnPrefabAsync(CancellationToken token)
        {
            try
            {
                if (_spawnPrefabReference != null && _spawnPrefabReference.RuntimeKeyIsValid())
                {
                    _spawnPrefabHandle = _spawnPrefabReference.LoadAssetAsync();
                    _spawnPrefab = await _spawnPrefabHandle.ToUniTask(cancellationToken: token);
                }
                else
                {
                    _spawnPrefab = _legacySpawnPrefab;
                }

                if (!_spawnPrefab)
                {
                    Debug.LogError("SpawnPrefab is not assigned.", this);
                    return;
                }

                if (_canUseObjectPool)
                {
                    _objectPool = new GameObjectPool(_spawnPrefab, _spawnCount, _spawnCount, _spawnParent);
                }

                OnPrefabReady();
                if (_isDefaultPlayerSpawner)
                {
                    return;
                }

                _spawnCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                if (_isLoop)
                {
                    _spawnedTokens = new();
                    SpawnWhenDespawnedAsync(_spawnCts.Token).Forget();
                }

                if (_onAwakeSpawn && !_syncSpawnCountWithStageSession)
                {
                    SpawnAsync(_spawnCts.Token, _spawnCount, _spawnFirstDelay).Forget();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        protected virtual void OnPrefabReady()
        {
        }

        private void OnEnable()
        {
            TryBindStageSession();
        }

        private void Start()
        {
            TryBindStageSession();
            if (_syncSpawnCountWithStageSession && GameManager.Instance != null && GameManager.Instance.IsRoundStarted && GameManager.Instance.CurrentStageSession.Round > 0)
            {
                OnStageSessionChanged(GameManager.Instance.CurrentStageSession, true);
            }
        }

        private void Update()
        {
            TryBindStageSession();
        }

        private void TryBindStageSession()
        {
            if (!_syncSpawnCountWithStageSession || _stageSessionBound || GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.OnStageSessionChanged += OnStageSessionChanged;
            _stageSessionBound = true;
            if (GameManager.Instance.IsRoundStarted && GameManager.Instance.CurrentStageSession.Round > 0)
            {
                OnStageSessionChanged(GameManager.Instance.CurrentStageSession, true);
            }
        }

        private void OnDisable()
        {
            if (_stageSessionBound && GameManager.Instance != null)
            {
                GameManager.Instance.OnStageSessionChanged -= OnStageSessionChanged;
            }

            _stageSessionBound = false;
        }

        private void OnStageSessionChanged(StageSession stageSession, bool isStart)
        {
            SetSpawnCount(CoinGame.Network.NetworkRoundState.Instance != null ? CoinGame.Network.NetworkRoundState.Instance.SpawnCoins : stageSession.RequiredCoins);
            if (!isStart || _lastSpawnedStageRound == stageSession.Round)
            {
                return;
            }

            _lastSpawnedStageRound = stageSession.Round;
            _spawnCts ??= CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            SpawnAsync(_spawnCts.Token, _spawnCount, _spawnFirstDelay).Forget();
        }

        public void SetSpawnCount(int spawnCount)
        {
            _spawnCount = Mathf.Max(1, spawnCount);
        }

        public void Spawn()
        {
            _spawnCts ??= CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            SpawnAsync(_spawnCts.Token, _spawnCount, _spawnDelay).Forget();
        }

        protected virtual async UniTask<CancellationToken?> SpawnOneAsync(CancellationToken token)
        {
            Vector3 position = await GetSpawnPosition(token);
            GameObject instance = _objectPool != null ? _objectPool.Get() : Instantiate(_spawnPrefab, _spawnParent);
            instance.transform.position = position;
            return instance.GetCancellationTokenOnDestroy();
        }

        private async UniTask SpawnAsync(CancellationToken token, int count, float delay)
        {
            try
            {
                await _spawnPrefabInitializationTask;
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);
                for (int i = 0; i < count; i++)
                {
                    CancellationToken? spawnedToken = await SpawnOneAsync(token);
                    if (_isLoop && spawnedToken.HasValue)
                    {
                        _spawnedTokens.Add(spawnedToken.Value);
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError(exception.Message);
            }
            finally
            {
                if (!_isLoop)
                {
                    if (_spawnCts != null)
            {
                _spawnCts.Dispose();
            }
                    _spawnCts = null;
                }
            }
        }

        private async UniTask SpawnWhenDespawnedAsync(CancellationToken token)
        {
            try
            {
                while (_isLoop)
                {
                    token.ThrowIfCancellationRequested();
                    int count = _spawnedTokens.RemoveAll(x => x.IsCancellationRequested);
                    if (count > 0)
                    {
                        await SpawnAsync(token, count, _spawnDelay);
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        protected virtual async UniTask<Vector3> GetSpawnPosition(CancellationToken token)
        {
            for (int i = 0; i < MaxIterations; i++)
            {
                token.ThrowIfCancellationRequested();
                Vector3 position = transform.position + new Vector3(
                    UnityEngine.Random.Range(-_spawnArea.x / 2f, _spawnArea.x / 2f),
                    UnityEngine.Random.Range(-_spawnArea.y / 2f, _spawnArea.y / 2f),
                    UnityEngine.Random.Range(-_spawnArea.z / 2f, _spawnArea.z / 2f));
                if (!Physics.CheckSphere(position, _checkRadius, _blockingLayerMask))
                {
                    return position;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            return transform.position;
        }

        protected virtual void OnDrawGizmos()
        {
            if (_drawArea)
            {
                Gizmos.DrawWireCube(transform.position, _spawnArea);
            }

            if (_drawCheckRadius)
            {
                Gizmos.DrawWireSphere(transform.position, _checkRadius);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_objectPool != null)
            {
                _objectPool.Dispose();
            }
            if (_spawnPrefabHandle.IsValid())
            {
                Addressables.Release(_spawnPrefabHandle);
            }

            if (_spawnCts != null)
            {
                _spawnCts.Cancel();
            }
            if (_spawnCts != null)
            {
                _spawnCts.Dispose();
            }
        }

        public bool IsDefaultPlayerSpawner => _isDefaultPlayerSpawner;

    }
}
