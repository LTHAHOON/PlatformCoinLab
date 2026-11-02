using Unity.Netcode;
using UnityEngine;
using CoinGame.UI;
using CoinGame.Utilities;
using CoinGame.Player;

namespace CoinGame.Network
{
    public class NetworkPlayerStats : NetworkBehaviour
    {
        private readonly NetworkVariable<int> _curScore = new(0);
        private readonly NetworkVariable<PlayerRole> _role = new(PlayerRole.Unselected);
        private readonly NetworkVariable<double> _coinLockUntil = new(0d);
        private readonly NetworkVariable<double> _pushedUntil = new(0d);
        private readonly NetworkVariable<int> _pushAttempts = new(0);
        private readonly NetworkVariable<int> _successfulPushes = new(0);
        private readonly NetworkVariable<int> _successfulLocks = new(0);
        private readonly NetworkVariable<int> _successfulSteals = new(0);
        private readonly NetworkVariable<int> _disruptionScore = new(0);
        private readonly NetworkVariable<int> _matchPlayerCount = new(0);
        private PlayerStatsView _playerStatsView;
        private bool _scoreEventsBound;
        private ulong _coinLockSourceClientId = ulong.MaxValue;
        private ulong _pushSourceClientId = ulong.MaxValue;

        public PlayerRole Role => _role.Value;
        public bool IsCoinLocked => NetworkManager.Singleton != null && NetworkManager.Singleton.ServerTime.Time < _coinLockUntil.Value;
        public bool IsBeingPushed => NetworkManager.Singleton != null && NetworkManager.Singleton.ServerTime.Time < _pushedUntil.Value;
        public int Score => _curScore.Value;
        public int DisruptionScore => _disruptionScore.Value;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                RefreshMatchPlayerCount();
            }

            if (!IsOwner)
            {
                return;
            }

            TryBindStatsView();
            if (!_scoreEventsBound)
            {
                _curScore.OnValueChanged += OnScoreChanged;
                _scoreEventsBound = true;
            }
            PlayerPrefsStorage.GetOrInitPlayerSessionFloat(PlayerSessionKey.SCORE, 0f);
        }

        private void Update()
        {
            if (IsOwner && !_playerStatsView)
            {
                TryBindStatsView();
            }
        }

        private void TryBindStatsView()
        {
            if (_playerStatsView)
            {
                return;
            }

            _playerStatsView = FindAnyObjectByType<PlayerStatsView>();
            if (!_playerStatsView)
            {
                return;
            }

            _playerStatsView.SetScoreText(0, _curScore.Value);
            _curScore.OnValueChanged += _playerStatsView.SetScoreText;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
            {
                return;
            }

            if (_playerStatsView)
            {
                _playerStatsView.SetScoreText(0, 0);
                _curScore.OnValueChanged -= _playerStatsView.SetScoreText;
            }

            _curScore.OnValueChanged -= OnScoreChanged;
            _playerStatsView = null;
            _scoreEventsBound = false;
        }

        private const int MaxAddScoreAmount = 10000;
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Server)]
        public void AddScoreRpc(int addAmount)
        {
            if (addAmount <= 0 || addAmount > MaxAddScoreAmount)
            {
                Debug.LogWarning($"잘못된 점수 증가량: {addAmount}");
                return;
            }

            _curScore.Value += addAmount;
        }

        private const int MaxSetScoreAmount = 10000000;
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SetScoreRpc(int curScore)
        {
            if (curScore < 0 || curScore > MaxSetScoreAmount)
            {
                Debug.LogWarning($"잘못된 점수 설정값: {curScore}");
                return;
            }
            _curScore.Value = curScore;
        }

        public void OnScoreChanged(int previousValue, int newValue)
        {
        }

        public void SelectRole(PlayerRole role)
        {
            SelectRoleRpc(role);
        }

        public void AssignRoleServer(PlayerRole role)
        {
            if (IsServer && role is PlayerRole.Collector or PlayerRole.Disruptor)
            {
                _role.Value = role;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SelectRoleRpc(PlayerRole role)
        {
            if (role is not (PlayerRole.Collector or PlayerRole.Disruptor))
            {
                return;
            }

            int roleCount = 0;
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null || client.PlayerObject == gameObject)
                {
                    continue;
                }

                NetworkPlayerStats otherStats = client.PlayerObject.GetComponent<NetworkPlayerStats>();
                if (otherStats != null && otherStats.Role == role)
                {
                    ++roleCount;
                }
            }

            int maxRoleCount = role == PlayerRole.Collector
                ? RoomLobbyController.Instance?.MaxCollectors ?? 1
                : RoomLobbyController.Instance?.MaxDisruptors ?? 1;
            if (roleCount >= maxRoleCount)
            {
                return;
            }

            _role.Value = role;
        }

        public bool TrySpendScore(int amount)
        {
            if (!IsServer || amount <= 0 || _curScore.Value < amount)
            {
                return false;
            }

            _curScore.Value -= amount;
            return true;
        }

        public void AddScoreServer(int amount)
        {
            if (IsServer && amount > 0)
            {
                _curScore.Value += amount;
            }
        }

        public void SetCoinLockServer(double until, NetworkPlayerStats source)
        {
            if (IsServer)
            {
                _coinLockUntil.Value = until;
                _coinLockSourceClientId = source != null ? source.OwnerClientId : ulong.MaxValue;
            }
        }

        public void SetBeingPushedServer(double until, NetworkPlayerStats source)
        {
            if (IsServer)
            {
                _pushedUntil.Value = Mathf.Max((float)_pushedUntil.Value, (float)until);
                _pushSourceClientId = source != null ? source.OwnerClientId : ulong.MaxValue;
            }
        }

        public void RegisterBlockedCoinAttemptServer()
        {
            if (!IsServer || NetworkManager.Singleton == null)
            {
                return;
            }

            ulong sourceClientId = IsCoinLocked ? _coinLockSourceClientId :
                IsBeingPushed ? _pushSourceClientId : ulong.MaxValue;
            if (sourceClientId == ulong.MaxValue ||
                !NetworkManager.Singleton.ConnectedClients.TryGetValue(sourceClientId, out NetworkClient sourceClient) ||
                sourceClient.PlayerObject == null)
            {
                return;
            }

            NetworkPlayerStats sourceStats = sourceClient.PlayerObject.GetComponent<NetworkPlayerStats>();
            if (sourceStats != null)
            {
                sourceStats._disruptionScore.Value++;
            }
        }

        public void ResetRoundDisruptorStatsServer()
        {
            if (!IsServer) return;
            _pushAttempts.Value = 0;
            _successfulPushes.Value = 0;
            _successfulLocks.Value = 0;
            _successfulSteals.Value = 0;
            _disruptionScore.Value = 0;
            _pushedUntil.Value = 0d;
            _coinLockSourceClientId = ulong.MaxValue;
            _pushSourceClientId = ulong.MaxValue;
        }

        private void RefreshMatchPlayerCount()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (!IsServer || networkManager == null)
            {
                return;
            }

            int playerCount = networkManager.ConnectedClientsList.Count;
            foreach (NetworkClient client in networkManager.ConnectedClientsList)
            {
                NetworkPlayerStats playerStats = client.PlayerObject != null
                    ? client.PlayerObject.GetComponent<NetworkPlayerStats>()
                    : null;
                if (playerStats != null)
                {
                    playerStats._matchPlayerCount.Value = playerCount;
                }
            }
        }
    }
}


