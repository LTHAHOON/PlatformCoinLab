using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using CoinGame.Stage;
using CoinGame.Player;
using CoinGame.Utilities;

namespace CoinGame.Network
{
    public sealed class NetworkRoundState : NetworkBehaviour
    {
        public static NetworkRoundState Instance { get; private set; }

        [SerializeField] private StageConfig _stageConfig;

        private readonly NetworkVariable<int> _round = new(0);
        private readonly NetworkVariable<int> _maxRounds = new(5);
        private readonly NetworkVariable<int> _targetCoins = new(0);
        private readonly NetworkVariable<int> _remainingCoins = new(0);
        private readonly NetworkVariable<int> _spawnCoins = new(0);
        private readonly NetworkVariable<int> _remainingSeconds = new(0);
        private readonly NetworkVariable<int> _coinMultiplier = new(2);
        private readonly NetworkVariable<bool> _roundActive = new(false);
        private readonly NetworkVariable<bool> _collectorWon = new(false);
        private readonly NetworkVariable<int> _resultVersion = new(0);
        private readonly NetworkVariable<double> _lobbyStartTime = new(0d);
        private readonly NetworkVariable<bool> _loadGame = new(false);

        private readonly System.Collections.Generic.HashSet<ulong> _readyClients = new();
        private readonly System.Collections.Generic.HashSet<ulong> _loadedClients = new();
        private readonly System.Collections.Generic.HashSet<ulong> _positionedClients = new();
        private int _expectedPlayers;
        private int _configuredCollectors;
        private int _configuredDisruptors;
        private bool _relocationSent;
        private bool _roundStartedAfterLobby;

        private CancellationTokenSource _roundCts;
        private double _nextTickTime;

        public int Round => _round.Value;
        public int MaxRounds => _maxRounds.Value;
        public int TargetCoins => _targetCoins.Value;
        public int RemainingCoins => _remainingCoins.Value;
        public int SpawnCoins => _spawnCoins.Value;
        public int RemainingSeconds => _remainingSeconds.Value;
        public int CoinMultiplier => _coinMultiplier.Value;
        public bool IsRoundActive => _roundActive.Value;
        public double LobbyStartTime => _lobbyStartTime.Value;
        public bool LoadGame => _loadGame.Value;

        public event Action<int, int, int, float, bool> StateChanged;
        public event Action<bool> RoundFinished;

        private void Awake()
        {
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            _round.OnValueChanged += OnNetworkValueChanged;
            _maxRounds.OnValueChanged += OnNetworkValueChanged;
            _targetCoins.OnValueChanged += OnNetworkValueChanged;
            _remainingCoins.OnValueChanged += OnNetworkValueChanged;
            _remainingSeconds.OnValueChanged += OnNetworkValueChanged;
            _roundActive.OnValueChanged += OnNetworkValueChanged;
            _resultVersion.OnValueChanged += OnResultVersionChanged;
            NotifyStateChanged();
        }

        public override void OnNetworkDespawn()
        {
            _roundCts?.Cancel();
            _roundCts?.Dispose();
            _roundCts = null;
            _round.OnValueChanged -= OnNetworkValueChanged;
            _maxRounds.OnValueChanged -= OnNetworkValueChanged;
            _targetCoins.OnValueChanged -= OnNetworkValueChanged;
            _remainingCoins.OnValueChanged -= OnNetworkValueChanged;
            _remainingSeconds.OnValueChanged -= OnNetworkValueChanged;
            _roundActive.OnValueChanged -= OnNetworkValueChanged;
            _resultVersion.OnValueChanged -= OnResultVersionChanged;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!IsServer || NetworkManager.Singleton == null)
            {
                return;
            }

            if (!_loadGame.Value)
            {
                UpdateLobbyServer();
                return;
            }

            if (!_relocationSent && EveryoneReady(_loadedClients))
            {
                foreach (Spawner spawner in FindObjectsByType<Spawner>(FindObjectsSortMode.None))
                {
                    if (!spawner.IsDefaultPlayerSpawner) continue;
                    _relocationSent = spawner.TryRelocatePlayers();
                    break;
                }
            }
            // RelocateForGameRpc is sent to every client and the round itself has a
            // one-second server delay, so do not block the round on a second RPC
            // acknowledgement from each owner.
            if (_relocationSent && !_roundStartedAfterLobby)
            {
                _roundStartedAfterLobby = true;
                StartRoundServer();
            }

            if (!_roundActive.Value)
            {
                return;
            }

            if (NetworkManager.Singleton.ServerTime.Time < _nextTickTime)
            {
                return;
            }

            _nextTickTime = NetworkManager.Singleton.ServerTime.Time + 1d;
            _remainingSeconds.Value = Mathf.Max(0, _remainingSeconds.Value - 1);
            if (_remainingSeconds.Value <= 0)
            {
                FinishRoundServer(false).Forget();
            }
        }

        public void ConfigureLobbyServer(int expectedPlayers, int collectors, int disruptors)
        {
            if (!IsServer) return;
            _expectedPlayers = expectedPlayers;
            _configuredCollectors = collectors;
            _configuredDisruptors = disruptors;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ReportReadyRpc(RpcParams rpcParams = default)
        {
            _readyClients.Add(rpcParams.Receive.SenderClientId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ReportSceneLoadedRpc(RpcParams rpcParams = default)
        {
            if (_loadGame.Value)
            {
                _loadedClients.Add(rpcParams.Receive.SenderClientId);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ReportPositionedRpc(RpcParams rpcParams = default)
        {
            if (_relocationSent)
            {
                _positionedClients.Add(rpcParams.Receive.SenderClientId);
            }
        }

        private bool EveryoneReady(System.Collections.Generic.HashSet<ulong> clients)
        {
            if (_expectedPlayers <= 0 || NetworkManager.Singleton.ConnectedClientsIds.Count != _expectedPlayers)
            {
                return false;
            }
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (!clients.Contains(client.ClientId) || client.PlayerObject == null) return false;
            }
            return true;
        }

        private void UpdateLobbyServer()
        {
            if (!EveryoneReady(_readyClients))
            {
                _lobbyStartTime.Value = 0d;
                return;
            }
            if (_lobbyStartTime.Value <= 0d)
            {
                _lobbyStartTime.Value = NetworkManager.ServerTime.Time + 10d;
                return;
            }
            if (NetworkManager.ServerTime.Time < _lobbyStartTime.Value) return;

            AssignMissingRolesServer(_configuredCollectors, _configuredDisruptors);
            _loadGame.Value = true;
        }

        public void ConfigureServer(int maxRounds, int coinMultiplier)
        {
            if (!IsServer)
            {
                return;
            }

            _maxRounds.Value = Mathf.Clamp(maxRounds, 1, 20);
            _coinMultiplier.Value = Mathf.Clamp(coinMultiplier, 1, 5);
        }

        public void StartRoundServer()
        {
            if (!IsServer || _stageConfig == null || _roundActive.Value)
            {
                return;
            }

            int nextRound = _round.Value + 1;
            if (nextRound > _maxRounds.Value)
            {
                return;
            }

            StartRoundAfterDelayAsync(nextRound).Forget();
        }

        private async UniTaskVoid StartRoundAfterDelayAsync(int round)
        {
            _roundCts?.Cancel();
            _roundCts?.Dispose();
            _roundCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: _roundCts.Token);
                if (!IsServer)
                {
                    return;
                }

                _round.Value = round;
                foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    client.PlayerObject?.GetComponent<NetworkPlayerStats>()?.ResetRoundDisruptorStatsServer();
                }
                _targetCoins.Value = _stageConfig.GetRequiredCoins(round);
                _remainingCoins.Value = _targetCoins.Value;
                _spawnCoins.Value = _stageConfig.GetSpawnCoinCount(round, _coinMultiplier.Value);
                _remainingSeconds.Value = Mathf.CeilToInt(_stageConfig.GetTimeLimit(round));
                _roundActive.Value = true;
                _nextTickTime = NetworkManager.Singleton.ServerTime.Time + 1d;
                NotifyStateChanged();
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void CollectCoinServer()
        {
            if (!IsServer || !_roundActive.Value || _remainingCoins.Value <= 0)
            {
                return;
            }

            _remainingCoins.Value--;
            if (_remainingCoins.Value <= 0)
            {
                FinishRoundServer(true).Forget();
            }
        }

        private async UniTaskVoid FinishRoundServer(bool collectorWon)
        {
            if (!IsServer || !_roundActive.Value)
            {
                return;
            }

            _roundActive.Value = false;
            _collectorWon.Value = collectorWon;
            _resultVersion.Value++;
            NotifyStateChanged();

            if (_round.Value < _maxRounds.Value)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(2f), cancellationToken: destroyCancellationToken);
                StartRoundServer();
            }
        }

        public void AssignMissingRolesServer(int maxCollectors, int maxDisruptors)
        {
            if (!IsServer || NetworkManager.Singleton == null)
            {
                return;
            }

            var unselected = new System.Collections.Generic.List<NetworkPlayerStats>();
            int collectors = 0;
            int disruptors = 0;
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                NetworkPlayerStats stats = client.PlayerObject != null ? client.PlayerObject.GetComponent<NetworkPlayerStats>() : null;
                if (stats == null)
                {
                    continue;
                }
                if (stats.Role == PlayerRole.Collector) collectors++;
                else if (stats.Role == PlayerRole.Disruptor) disruptors++;
                else unselected.Add(stats);
            }

            for (int i = unselected.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (unselected[i], unselected[swapIndex]) = (unselected[swapIndex], unselected[i]);
            }

            foreach (NetworkPlayerStats stats in unselected)
            {
                if (collectors < maxCollectors)
                {
                    stats.AssignRoleServer(PlayerRole.Collector);
                    collectors++;
                }
                else if (disruptors < maxDisruptors)
                {
                    stats.AssignRoleServer(PlayerRole.Disruptor);
                    disruptors++;
                }
            }
        }

        private void OnNetworkValueChanged<T>(T previous, T current)
        {
            NotifyStateChanged();
        }

        private void OnResultVersionChanged(int previous, int current)
        {
            RoundFinished?.Invoke(_collectorWon.Value);
        }

        private void NotifyStateChanged()
        {
            float time = _remainingSeconds.Value;
            StateChanged?.Invoke(_round.Value, _targetCoins.Value, _remainingCoins.Value, time, _roundActive.Value);
        }
    }
}
