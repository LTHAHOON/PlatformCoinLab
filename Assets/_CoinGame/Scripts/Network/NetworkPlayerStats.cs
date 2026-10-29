using Unity.Netcode;
using UnityEngine;
using CoinGame.Player;
using CoinGame.UI;
using CoinGame.Sessions;

namespace CoinGame.Network
{
    public class NetworkPlayerStats : NetworkBehaviour
    {
        private readonly NetworkVariable<int> _curScore = new(0);
        private PlayerStatsView _playerStatsView;

        public override void OnNetworkSpawn()
        {
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
            PlayerSession.GetOrInitPlayerSessionFloat(PlayerSessionKey.SCORE, 0f);
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
    }
}


