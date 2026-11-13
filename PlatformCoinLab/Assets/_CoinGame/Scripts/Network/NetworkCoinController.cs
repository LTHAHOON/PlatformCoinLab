using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using Unity.Netcode;
using UnityEngine;

namespace CoinGame.Network
{
    using CoinGame.Utilities;
    using CoinGame.Managers;

    public class NetworkCoinController : NetworkBehaviour
    {
        private CancellationTokenSource _triggerEnterCts;
        private CancellationTokenSource _despawnCts;

        public override void OnNetworkSpawn()
        {
            if (_despawnCts != null)
            {
                _despawnCts.Dispose();
            }
            _despawnCts = new CancellationTokenSource();

            if (!IsServer)
            {
                return;
            }

            if (_triggerEnterCts is null)
            {
                _triggerEnterCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                OnTriggerEnterAsync(_triggerEnterCts.Token).Forget();
            }

        }

        public override void OnNetworkDespawn()
        {
            if (_triggerEnterCts is not null)
            {
                _triggerEnterCts.Cancel();
                _triggerEnterCts.Dispose();
                _triggerEnterCts = null;
            }

            if (_despawnCts is not null)
            {
                _despawnCts.Cancel();
                _despawnCts.Dispose();
                _despawnCts = null;
            }
        }

        private async UniTaskVoid OnTriggerEnterAsync(CancellationToken token)
        {
            AsyncTriggerEnterTrigger triggerEnter = this.GetAsyncTriggerEnterTrigger();

            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    Collider collider = await triggerEnter.OnTriggerEnterAsync(token);
                    if (!collider || !collider.TryGetComponent(out NetworkPlayerStats playerStats))
                    {
                        continue;
                    }
                    if (playerStats.Role != CoinGame.Player.PlayerRole.Collector)
                    {
                        continue;
                    }
                    if (playerStats.IsCoinLocked || playerStats.IsBeingPushed)
                    {
                        playerStats.RegisterBlockedCoinAttemptServer();
                        continue;
                    }
                    playerStats.AddScoreServer(1);
                    if (NetworkRoundState.Instance != null)
                    {
                        NetworkRoundState.Instance.CollectCoinServer();
                    }
                    NetworkGameObjectPool.Despawn(gameObject);
                    break;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void NotifyStageCoinCollectedRpc()
        {
            if (!GameManager.Instance)
            {
                Debug.LogError("GameManager??嶺뚢돦堉??????怨룸????덈펲.", this);
                return;
            }

            GameManager.Instance.CollectStageCoin();
        }

        public CancellationToken DespawnToken =>
            _despawnCts != null ? _despawnCts.Token : destroyCancellationToken;
    }

}

