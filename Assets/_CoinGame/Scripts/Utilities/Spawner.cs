using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using CoinGame.Network;
using CoinGame.Managers;
using CoinGame.Session;

namespace CoinGame.Utilities
{
    public class Spawner : MonoBehaviour
    {
        public enum SpawnType
        {
            BaseObject,
            NetworkObject
        }

        [SerializeField]
        private SpawnType _spawnType;
        [SerializeField]
        private GameObject _spawnPrefab;
        [SerializeField]
        private Transform _spawnParent;
        [SerializeField]
        private bool _onAwakeSpawn;
        [SerializeField]
        private bool _syncSpawnCountWithStageSession;
        [SerializeField]
        private bool _isDefaultPlayerSpawner;

        [Space(10f)]
        [SerializeField]
        private bool _isLoop = false;
        [SerializeField]
        [Range(1, 100)]
        private int _spawnCount = 10;
        [SerializeField]
        private float _spawnFirstDelay = 1f;
        [SerializeField]
        private float _spawnDelay = 1f;
        [SerializeField]
        private Vector3 _spawnArea = new(5f, 5f, 5f);
        [SerializeField]
        private bool _drawArea = false;
        [SerializeField]
        private float _checkRadius = 1f;
        [SerializeField]
        private bool _drawCheckRadius = false;
        [SerializeField]
        private LayerMask _blockingLayerMask;

        [SerializeField]
        public bool _canUseObjectPool;

        private NetworkGameObjectPool _networkObjectPool;
        private GameObjectPool _objectPool;
        private bool _poolHandlerRegistered;
        private NetworkObject _spawnNetworkParent;
        private List<CancellationToken> _spawnedTokens;
        private CancellationTokenSource _spawnCts;
        private Dictionary<ulong, CancellationTokenSource> _dicSpawnPlayerCts;
        private const int MAX_ITER = 50;

        private void Awake()
        {
            if(_spawnParent && _spawnType == SpawnType.NetworkObject)
            {
                if(!_spawnParent.TryGetComponent<NetworkObject>(out _spawnNetworkParent))
                {
                    Debug.LogError("SpawnParent에는 NetworkObject 컴포넌트가 필요합니다.");
                    return;
                }
            }
            if (_canUseObjectPool && _spawnPrefab)
            {
                if (_spawnType == SpawnType.NetworkObject)
                {
                    _networkObjectPool = new NetworkGameObjectPool(_spawnPrefab, _spawnCount, _spawnCount, _spawnParent);
                    RegisterPoolHandlerAsync(destroyCancellationToken).Forget();
                }
                else
                {
                    _objectPool = new GameObjectPool(_spawnPrefab, _spawnCount, _spawnCount, _spawnParent);
                }
            }
            if (_isDefaultPlayerSpawner)
            {
                return;
            }

            _spawnCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            if (_isLoop)
            {
                _spawnedTokens = new();
                SpawnWhenDespawned_Loop(_spawnCts.Token).Forget();
            }
            if (_onAwakeSpawn)
            {
                if (!_syncSpawnCountWithStageSession)
                {
                    SpawnAsync(_spawnCts.Token, _spawnCount, _spawnFirstDelay).Forget();
                }
            }

        }

        private void OnEnable()
        {
            if (_syncSpawnCountWithStageSession)
            {
                GameManager.Instance.OnStageSessionChanged += OnStageSessionChanged;
            }
        }

        private void OnDisable()
        {
            if (_syncSpawnCountWithStageSession)
            {
                GameManager.Instance.OnStageSessionChanged -= OnStageSessionChanged;
            }
        }

        private void OnStageSessionChanged(StageSession stageSession, bool isStart)
        {
            SetSpawnCount(stageSession.RequiredCoins);
            if (isStart)
            {
                SpawnAsync(_spawnCts.Token, _spawnCount, _spawnFirstDelay).Forget();
            }
        }

        public void SetSpawnCount(int spawnCount)
        {
            _spawnCount = Mathf.Max(1, spawnCount);
        }

        private async UniTask RegisterPoolHandlerAsync(CancellationToken token)
        {
            try
            {
                await UniTask.WaitUntil(() => NetworkManager.Singleton != null, cancellationToken: token);
                EnsurePoolHandler();
            }
            catch (OperationCanceledException) { }
        }

        private void EnsurePoolHandler()
        {
            if (_networkObjectPool == null || _poolHandlerRegistered)
            {
                return;
            }

            _poolHandlerRegistered = NetworkManager.Singleton.PrefabHandler.AddHandler(_spawnPrefab, new PooledNetworkPrefabHandler(_networkObjectPool));
            if (!_poolHandlerRegistered)
            {
                throw new InvalidOperationException("Pool 핸들러가 등록되지 못했습니다.");
            }
        }

        private void OnValidate()
        {
            if (_isDefaultPlayerSpawner)
            {
                if (_spawnType != SpawnType.NetworkObject)
                {
                    Debug.LogWarning("Warning: DefaultPlayerSpawner로 사용할 경우 SpawnType을 NetworkObject로 지정하십시오.");
                }
                if (!FindAnyObjectByType<NetworkConnector>())
                {
                    Debug.LogWarning("Warning: DefaultPlayerSpawner로 사용할 경우 NetworkConnector가 필요합니다.");
                }
            }
        }

        public void OnDestroy()
        {
            if (_poolHandlerRegistered && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.PrefabHandler.RemoveHandler(_spawnPrefab);
            }
            _networkObjectPool?.Dispose();
            if (_spawnCts is not null)
            {
                _spawnCts.Cancel();
                _spawnCts.Dispose();
                _spawnCts = null;
            }
            if (_dicSpawnPlayerCts != null)
            {
                foreach (CancellationTokenSource cts in _dicSpawnPlayerCts.Values)
                {
                    cts?.Cancel();
                    cts?.Dispose();
                }
                _dicSpawnPlayerCts.Clear();
            }
        }

        public void Spawn()
        {
            _spawnCts ??= CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            SpawnAsync(_spawnCts.Token, _spawnCount, _spawnDelay).Forget();
        }

        public void SpawnPlayer(ulong clientId)
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsServer || !_isDefaultPlayerSpawner)
            {
                return;
            }
            if (!networkManager.ConnectedClients.ContainsKey(clientId))
            {
                return;
            }
            if (!_spawnPrefab.TryGetComponent(out NetworkObject networkObject))
            {
                Debug.LogError("ERROR: SpawnPrefab에 NetworkObject가 포함되지않습니다.");
                return;
            }

            _dicSpawnPlayerCts ??= new();
            if (!_dicSpawnPlayerCts.ContainsKey(clientId))
            {
                _dicSpawnPlayerCts.Add(clientId, CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken));
            }
            SpawnPlayerAsync(_dicSpawnPlayerCts[clientId].Token, networkObject, clientId).Forget();
        }

        public void CancelSpawnPlayer(ulong clientId)
        {
            if (_dicSpawnPlayerCts == null || !_dicSpawnPlayerCts.ContainsKey(clientId))
            {
                return;
            }
            _dicSpawnPlayerCts[clientId]?.Cancel();
            _dicSpawnPlayerCts[clientId]?.Dispose();
            _dicSpawnPlayerCts.Remove(clientId);
        }

        private async UniTask SpawnPlayerAsync(CancellationToken token, NetworkObject networkObject, ulong clientId)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_spawnDelay), cancellationToken: token);
                token.ThrowIfCancellationRequested();
                await SpawnAsyncForNetwork(token, networkObject, clientId);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        private async UniTask SpawnWhenDespawned_Loop(CancellationToken token)
        {
            try
            {
                while (_isLoop)
                {
                    token.ThrowIfCancellationRequested();
                    if (_spawnedTokens.Count <= 0f)
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update);
                        continue;
                    }

                    int canceledCount = 0;
                    foreach (CancellationToken spawnedToken in _spawnedTokens)
                    {
                        if (spawnedToken.IsCancellationRequested)
                        {
                            ++canceledCount;
                        }
                    }

                    if (canceledCount > 0)
                    {
                        _spawnedTokens.RemoveAll(x => x.IsCancellationRequested);
                        await SpawnAsync(token, canceledCount, _spawnDelay);
                    }
                    await UniTask.Yield(PlayerLoopTiming.Update);

                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        private async UniTask<Vector3> GetSpawnPosition(CancellationToken token)
        {
            int curIter = 0;
            try
            {
                while (curIter < MAX_ITER)
                {
                    token.ThrowIfCancellationRequested();
                    ++curIter;

                    Vector3 position = transform.position;
                    float xRandomDelta = UnityEngine.Random.Range(-_spawnArea.x / 2f, _spawnArea.x / 2f);
                    float yRandomDelta = UnityEngine.Random.Range(-_spawnArea.y / 2f, _spawnArea.y / 2f);
                    float zRandomDelta = UnityEngine.Random.Range(-_spawnArea.z / 2f, _spawnArea.z / 2f);
                    position += new Vector3(xRandomDelta, yRandomDelta, zRandomDelta);

                    if (!Physics.CheckSphere(position, _checkRadius, _blockingLayerMask))
                    {
                        return position;
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
            return transform.position;
        }

        private async UniTask<CancellationToken?> SpawnAsyncForNetwork(CancellationToken token, NetworkObject networkObject, ulong? clientId = null)
        {
            try
            {
                await UniTask.WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening, cancellationToken: token);
                if (!NetworkManager.Singleton.IsServer)
                {
                    return null;
                }
                Vector3 spawnPosition = await GetSpawnPosition(token);
                token.ThrowIfCancellationRequested();

                if (_spawnNetworkParent != null)
                {
                    await UniTask.WaitUntil(() => _spawnNetworkParent.IsSpawned, cancellationToken: token);
                }

                EnsurePoolHandler();
                NetworkObject newNetworkObject = null;
                if (_networkObjectPool != null)
                {
                    GameObject newObject = _networkObjectPool.Get();
                    if (newObject)
                    {
                        newObject.TryGetComponent(out newNetworkObject);
                    }
                }
                else
                {
                    newNetworkObject = Instantiate(networkObject);
                }

                if (newNetworkObject == null)
                {
                    throw new NullReferenceException();
                }

                newNetworkObject.transform.position = spawnPosition;
                if (clientId.HasValue)
                {
                    token.ThrowIfCancellationRequested();
                    newNetworkObject.SpawnAsPlayerObject(clientId.Value);
                }
                else
                {
                    token.ThrowIfCancellationRequested();
                    if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || !NetworkManager.Singleton.IsServer)
                    {
                        return null;
                    }
                    newNetworkObject.Spawn();
                }
                if (_spawnNetworkParent)
                {
                    if (!newNetworkObject.TrySetParent(_spawnNetworkParent))
                    {
                        Debug.LogWarning($"Failed to parent spawned NetworkObject '{newNetworkObject.name}' to '{_spawnNetworkParent.name}'.", this);
                    }
                }
                return _networkObjectPool != null ? _networkObjectPool.GetDespawnToken(newNetworkObject.gameObject) : newNetworkObject.destroyCancellationToken;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
            return null;
        }

        private async UniTask SpawnAsync(CancellationToken token, int spawnCount, float spawnDelay)
        {
            try
            {
                if (!_spawnPrefab)
                {
                    Debug.LogError("ERROR: SpawnPrefab이 Null입니다.");
                    return;
                }
                await UniTask.Delay(TimeSpan.FromSeconds(spawnDelay), cancellationToken: token);

                int curSpawnCount = 0;
                while (curSpawnCount < spawnCount)
                {
                    ++curSpawnCount;
                    CancellationToken spawnedToken = default;
                    bool spawnSucceeded = false;
                    switch (_spawnType)
                    {
                        case SpawnType.BaseObject:
                            Vector3 spawnPosition = await GetSpawnPosition(token);
                            token.ThrowIfCancellationRequested();
                            GameObject newSpawnObject = _objectPool != null ? _objectPool.Get() : Instantiate(_spawnPrefab, _spawnParent);
                            newSpawnObject.transform.position = spawnPosition;
                            spawnedToken = newSpawnObject.GetCancellationTokenOnDestroy();
                            spawnSucceeded = true;
                            break;
                        case SpawnType.NetworkObject:
                            if (!_spawnPrefab.TryGetComponent(out NetworkObject networkObject))
                            {
                                Debug.LogError("ERROR: SpawnPrefab은 NetworkObject 컴포넌트를 가지고있지 않습니다.");
                                return;
                            }
                            var spawnedTokenRef = await SpawnAsyncForNetwork(token, networkObject);
                            if (spawnedTokenRef.HasValue)
                            {
                                spawnedToken = spawnedTokenRef.Value;
                                spawnSucceeded = true;
                            }
                            break;
                    }
                    if (_isLoop && spawnSucceeded)
                    {
                        _spawnedTokens.Add(spawnedToken);
                    }
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
            finally
            {
                if (!_isLoop)
                {
                    _spawnCts?.Dispose();
                    _spawnCts = null;
                }
            }
        }


        private void OnDrawGizmos()
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

    }
}
