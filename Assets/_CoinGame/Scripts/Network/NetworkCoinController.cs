using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using Unity.Netcode;
using UnityEngine;
using CoinGame.Utilities;

namespace CoinGame.Network
{
    public class NetworkCoinController : NetworkBehaviour
    {
        private CancellationTokenSource _triggerEnterCts;
        private CancellationTokenSource _despawnCts;

        public CancellationToken DespawnToken =>
            _despawnCts != null ? _despawnCts.Token : destroyCancellationToken;

        public override void OnNetworkSpawn()
        {
            _despawnCts?.Dispose();
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
                    playerStats.AddScoreRpc(1);
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
    }

}

