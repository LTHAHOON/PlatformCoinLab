using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace CoinGame.Network
{
    using CoinGame.Stage;
    using CoinGame.Player;
    using CoinGame.Managers;
    using CoinGame.Gameplay.Combat;
    using CoinGame.Gameplay.Stage;
    using CoinGame.Utilities;

    public sealed class NetworkRoundState : NetworkBehaviour
    {
        private enum ClientSyncPhase
        {
            LobbyReady,
            SceneLoaded,
            Positioned,
            Complete,
        }

        public static NetworkRoundState Instance { get; private set; }

        [SerializeField]
        private StageConfig _stageConfig;

        private readonly NetworkVariable<int> _round = new(0);
        private readonly NetworkVariable<int> _maxRounds = new(5);
        private readonly NetworkVariable<int> _targetCoins = new(0);
        private readonly NetworkVariable<int> _remainingCoins = new(0);
        private readonly NetworkVariable<int> _spawnCoins = new(0);
        private readonly NetworkVariable<int> _remainingSeconds = new(0);
        private readonly NetworkVariable<int> _coinMultiplier = new(1);
        private readonly NetworkVariable<bool> _roundActive = new(false);
        private readonly NetworkVariable<bool> _collectorWon = new(false);
        private readonly NetworkVariable<MatchWinner> _matchWinner = new(MatchWinner.None);
        private readonly NetworkVariable<int> _resultVersion = new(0);
        private readonly NetworkVariable<double> _lobbyStartTime = new(0d);
        private readonly NetworkVariable<bool> _loadGame = new(false);

        private readonly HashSet<ulong> _phaseReadyClients = new();
        private int _expectedPlayers;
        private int _configuredCollectors;
        private int _configuredDisruptors;
        private bool _relocationSent;
        private bool _roundStartedAfterLobby;
        private ClientSyncPhase _clientSyncPhase = ClientSyncPhase.LobbyReady;

        private CancellationTokenSource _roundCts;
        private double _nextTickTime;
        private float _roundStartDelay = 1f;
        private float _lobbyCountdownSeconds = 10f;
        private bool _isRoundStarting;
        private float _nextQuickLog;


        public event Action<int, int, int, float, bool> StateChanged;
        public event Action<bool> RoundFinished;
        public event Action<bool> MatchFinished;

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
            if (_roundCts != null)
            {
                _roundCts.Cancel();
                _roundCts.Dispose();
            }
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

            if (RoomLobbyController.IsDevelopmentQuickStartActive && Time.unscaledTime >= _nextQuickLog)
            {
                _nextQuickLog = Time.unscaledTime + 1f;
                Debug.Log($"[QuickRound] phase={_clientSyncPhase}, load={_loadGame.Value}, ready={_phaseReadyClients.Count}, clients={NetworkManager.Singleton.ConnectedClientsIds.Count}, round={_round.Value}, active={_roundActive.Value}", this);
            }

            if (!_loadGame.Value)
            {
                UpdateLobbyServer();
                return;
            }

            if (_clientSyncPhase == ClientSyncPhase.SceneLoaded && EveryoneReady(_phaseReadyClients))
            {
                BeginClientSyncPhase(ClientSyncPhase.Positioned);
                _relocationSent = NetworkConnector.Instance != null && NetworkConnector.Instance.TryRelocatePlayers();
                if (!_relocationSent)
                {
                    BeginClientSyncPhase(ClientSyncPhase.SceneLoaded);
                    foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
                    {
                        _phaseReadyClients.Add(client.ClientId);
                    }
                }
            }

            if (_relocationSent && !_roundStartedAfterLobby &&
                _clientSyncPhase == ClientSyncPhase.Positioned && EveryoneReady(_phaseReadyClients))
            {
                BeginClientSyncPhase(ClientSyncPhase.Complete);
                _roundStartedAfterLobby = true;
                float startDelay = GameManager.Instance != null
                    ? GameManager.Instance.RoundStartDelay
                    : _roundStartDelay;
                StartRoundServer(startDelay);
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
                ApplyTimeoutPenaltyServer();
                FinishRoundServer(false).Forget();
            }
        }

        public void ConfigureLobbyServer(int expectedPlayers, int collectors, int disruptors, float lobbyCountdownSeconds = 10f)
        {
            if (!IsServer)
            {
                return;
            }

            _expectedPlayers = expectedPlayers;
            _configuredCollectors = collectors;
            _configuredDisruptors = disruptors;
            _lobbyCountdownSeconds = Mathf.Max(0f, lobbyCountdownSeconds);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ReportReadyRpc(RpcParams rpcParams = default)
        {
            if (_clientSyncPhase == ClientSyncPhase.LobbyReady)
            {
                _phaseReadyClients.Add(rpcParams.Receive.SenderClientId);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ReportSceneLoadedRpc(RpcParams rpcParams = default)
        {
            if (_clientSyncPhase == ClientSyncPhase.SceneLoaded)
            {
                _phaseReadyClients.Add(rpcParams.Receive.SenderClientId);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ReportPositionedRpc(RpcParams rpcParams = default)
        {
            if (_clientSyncPhase == ClientSyncPhase.Positioned)
            {
                _phaseReadyClients.Add(rpcParams.Receive.SenderClientId);
            }
        }


        private bool EveryoneReady(HashSet<ulong> clients)
        {
            if (_expectedPlayers <= 0 || NetworkManager.Singleton.ConnectedClientsIds.Count != _expectedPlayers)
            {
                return false;
            }
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (!clients.Contains(client.ClientId) || client.PlayerObject == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void UpdateLobbyServer()
        {
            if (!EveryoneReady(_phaseReadyClients))
            {
                _lobbyStartTime.Value = 0d;
                return;
            }

            if (_lobbyStartTime.Value <= 0d)
            {
                _lobbyStartTime.Value = NetworkManager.ServerTime.Time + _lobbyCountdownSeconds;
                return;
            }

            if (NetworkManager.ServerTime.Time < _lobbyStartTime.Value)
            {
                return;
            }

            AssignMissingRolesServer(_configuredCollectors, _configuredDisruptors);
            BeginClientSyncPhase(ClientSyncPhase.SceneLoaded);
            _loadGame.Value = true;
        }

        private void BeginClientSyncPhase(ClientSyncPhase phase)
        {
            _phaseReadyClients.Clear();
            _clientSyncPhase = phase;
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
            StartRoundServer(_roundStartDelay);
        }

        public void StartRoundServer(float startDelay)
        {
            if (!IsServer || _stageConfig == null || _roundActive.Value || _isRoundStarting)
            {
                if (RoomLobbyController.IsDevelopmentQuickStartActive)
                {
                    Debug.LogWarning($"[QuickRound] StartRoundServer skipped: server={IsServer}, stageConfig={_stageConfig != null}, active={_roundActive.Value}, starting={_isRoundStarting}", this);
                }
                return;
            }

            int nextRound = _round.Value + 1;
            if (nextRound > _maxRounds.Value)
            {
                return;
            }

            _roundStartDelay = Mathf.Max(0f, startDelay);
            _isRoundStarting = true;
            StartRoundAfterDelayAsync(nextRound).Forget();
        }

        private async UniTaskVoid StartRoundAfterDelayAsync(int round)
        {
            if (_roundCts != null)
            {
                _roundCts.Cancel();
                _roundCts.Dispose();
            }
            _roundCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            try
            {
                if (round > 1)
                {
                    BeginClientSyncPhase(ClientSyncPhase.Positioned);
                    if (NetworkConnector.Instance == null || !NetworkConnector.Instance.TryRelocatePlayers())
                    {
                        BeginClientSyncPhase(ClientSyncPhase.Complete);
                        Debug.LogError("Failed to relocate players before starting the next round.", this);
                        return;
                    }

                    while (!EveryoneReady(_phaseReadyClients))
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, _roundCts.Token);
                    }
                    BeginClientSyncPhase(ClientSyncPhase.Complete);
                }

                foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    NetworkPlayerStats stats = client.PlayerObject == null ? null : client.PlayerObject.GetComponent<NetworkPlayerStats>();
                    if (stats != null)
                    {
                        stats.ResetRoundDisruptorStatsServer();
                        stats.ResetRoundLifeServer();
                    }
                }
                _matchWinner.Value = MatchWinner.None;
                _targetCoins.Value = _stageConfig.GetRequiredCoins(round);
                _remainingCoins.Value = _targetCoins.Value;
                _spawnCoins.Value = _stageConfig.GetSpawnCoinCount(round, _coinMultiplier.Value);
                _remainingSeconds.Value = Mathf.CeilToInt(_stageConfig.GetTimeLimit(round));
                _round.Value = round;
                NotifyStateChanged();

                await UniTask.Delay(
                    TimeSpan.FromSeconds(_roundStartDelay + GameManager.RoundBannerFadeInDuration),
                    cancellationToken: _roundCts.Token);
                if (!IsServer)
                {
                    return;
                }

                _roundActive.Value = true;
                _nextTickTime = NetworkManager.Singleton.ServerTime.Time + 1d;
                NotifyStateChanged();
            }
            catch (OperationCanceledException) { }
            finally
            {
                _isRoundStarting = false;
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

        public void NotifyPlayerDeathServer()
        {
            if (!IsServer || !IsRoundActive || CountAliveCollectorsServer() > 0)
            {
                return;
            }

            FinishMatchServer(MatchWinner.Disruptor);
        }

        public void RelocatePlayerServer(NetworkPlayerStats stats)
        {
            if (!IsServer || stats == null || NetworkManager.Singleton == null || stats.NetworkObject == null)
            {
                return;
            }

            NetworkSpawner spawner = FindAnyObjectByType<NetworkSpawner>();
            NetworkPlayerController player = stats.GetComponent<NetworkPlayerController>();
            if (spawner != null)
            {
                spawner.TryRelocatePlayer(player);
            }
        }

        private int CountAliveCollectorsServer()
        {
            int alive = 0;
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                NetworkPlayerStats stats = client.PlayerObject == null ? null : client.PlayerObject.GetComponent<NetworkPlayerStats>();
                if (stats != null && stats.Role == PlayerRole.Collector && stats.IsAlive)
                {
                    alive++;
                }
            }

            return alive;
        }

        private void ApplyTimeoutPenaltyServer()
        {
            int penalty = _stageConfig != null
                ? _stageConfig.GetTimeoutPenalty(_remainingCoins.Value)
                : 0;
            if (penalty <= 0)
            {
                return;
            }

            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                NetworkPlayerStats stats = client.PlayerObject == null ? null : client.PlayerObject.GetComponent<NetworkPlayerStats>();
                if (stats != null && stats.Role == PlayerRole.Collector)
                {
                    stats.ApplyDamageServer(new DamageInfo(ulong.MaxValue, penalty, DamageType.Timeout, Vector3.zero));
                }
            }
        }

        private async UniTaskVoid FinishRoundServer(bool collectorWon)
        {
            if (!IsServer || !_roundActive.Value)
            {
                return;
            }

            _roundActive.Value = false;
            bool finalCollectorWon = collectorWon;
            if (_round.Value >= _maxRounds.Value)
            {
                finalCollectorWon = CountAliveCollectorsServer() > 0;
                _matchWinner.Value = finalCollectorWon
                    ? MatchWinner.Collector
                    : MatchWinner.Disruptor;
            }
            _collectorWon.Value = finalCollectorWon;
            _resultVersion.Value++;
            NotifyStateChanged();

            if (_round.Value < _maxRounds.Value)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(2f), cancellationToken: destroyCancellationToken);
                StartRoundServer();
            }
        }

        private void FinishMatchServer(MatchWinner winner)
        {
            if (!IsServer || !_roundActive.Value)
            {
                return;
            }

            _roundActive.Value = false;
            _collectorWon.Value = winner == MatchWinner.Collector;
            _matchWinner.Value = winner;
            _resultVersion.Value++;
            NotifyStateChanged();
        }

        public void AssignMissingRolesServer(int maxCollectors, int maxDisruptors)
        {
            if (!IsServer || NetworkManager.Singleton == null)
            {
                return;
            }

            List<NetworkPlayerStats> unselected = new();
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
            if (RoundFinished != null)
            {
                RoundFinished.Invoke(_collectorWon.Value);
            }
            if (_round.Value >= _maxRounds.Value)
            {
                if (MatchFinished != null)
                {
                    MatchFinished.Invoke(_collectorWon.Value);
                }
            }
        }

        private void NotifyStateChanged()
        {
            float time = _remainingSeconds.Value;
            if (StateChanged != null)
            {
                StateChanged.Invoke(_round.Value, _targetCoins.Value, _remainingCoins.Value, time, _roundActive.Value);
            }
        }

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
        public MatchWinner MatchWinner => _matchWinner.Value;
        public bool IsRoleSelectionOpen => _lobbyStartTime.Value > 0d && !_loadGame.Value;

    }
}
