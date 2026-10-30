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
        private readonly NetworkVariable<int> _matchPlayerCount = new(0);
        private PlayerStatsView _playerStatsView;

        public PlayerRole Role => _role.Value;
        public int MatchPlayerCount => _matchPlayerCount.Value;
        public bool IsCoinLocked => NetworkManager.Singleton != null && NetworkManager.Singleton.ServerTime.Time < _coinLockUntil.Value;

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

            if (!_playerStatsView)
            {
                _playerStatsView = FindAnyObjectByType<PlayerStatsView>();
                if (!_playerStatsView)
                {
                    Debug.LogError("PlayerStatsView를 찾을 수 없습니다.");
                    return;
                }
            }
            _playerStatsView.SetScoreText(0, _curScore.Value);
            _curScore.OnValueChanged += _playerStatsView.SetScoreText;
            _curScore.OnValueChanged += OnScoreChanged;
            _role.OnValueChanged += OnRoleChanged;
            PlayerPrefsStorage.GetOrInitPlayerSessionFloat(PlayerSessionKey.SCORE, 0f);
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
            {
                return;
            }
            if (!_playerStatsView)
            {
                return;
            }

            _playerStatsView.SetScoreText(0, 0);
            _curScore.OnValueChanged -= _playerStatsView.SetScoreText;
            _curScore.OnValueChanged -= OnScoreChanged;
            _role.OnValueChanged -= OnRoleChanged;
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

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SelectRoleRpc(PlayerRole role)
        {
            if (role is not (PlayerRole.Collector or PlayerRole.Disruptor))
            {
                return;
            }

            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null || client.PlayerObject == gameObject)
                {
                    continue;
                }

                NetworkPlayerStats otherStats = client.PlayerObject.GetComponent<NetworkPlayerStats>();
                if (otherStats != null && otherStats.Role == role)
                {
                    return;
                }
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

        public void SetCoinLockServer(double until)
        {
            if (IsServer)
            {
                _coinLockUntil.Value = until;
            }
        }

        private void OnRoleChanged(PlayerRole previousRole, PlayerRole newRole)
        {
        }

        private void RefreshMatchPlayerCount()
        {
            NetworkPlayerStats[] stats = FindObjectsByType<NetworkPlayerStats>(FindObjectsSortMode.None);
            foreach (NetworkPlayerStats playerStats in stats)
            {
                playerStats._matchPlayerCount.Value = stats.Length;
            }
        }
    }
}


