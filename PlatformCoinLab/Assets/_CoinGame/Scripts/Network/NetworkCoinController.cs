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
        private bool _collected;
        private Renderer[] _renderers;
        private int _gridReservationId;

        public void SetGridReservation(int reservationId)
        {
            _gridReservationId = reservationId;
        }

        public override void OnNetworkSpawn()
        {
            _collected = false;
            _renderers = GetComponentsInChildren<Renderer>(true);
            ApplyVisibility();
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

        private void Update()
        {
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            if (_renderers == null)
            {
                return;
            }

            NetworkPlayerStats localStats = null;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
            {
                NetworkObject localObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                if (localObject != null)
                {
                    localStats = localObject.GetComponent<NetworkPlayerStats>();
                }
            }

            bool visible = localStats != null && localStats.Role == CoinGame.Player.PlayerRole.Collector;
            foreach (Renderer renderer in _renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_gridReservationId != 0)
            {
                GridSpawnReservation.ReleaseReservation(_gridReservationId);
                _gridReservationId = 0;
            }

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
                    TryCollectServer(playerStats);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        private void OnTriggerStay(Collider collider)
        {
            if (!IsServer || !collider || !collider.TryGetComponent(out NetworkPlayerStats playerStats))
            {
                return;
            }

            TryCollectServer(playerStats);
        }

        private void TryCollectServer(NetworkPlayerStats playerStats)
        {
            if (!IsServer || _collected || playerStats.Role != CoinGame.Player.PlayerRole.Collector)
            {
                return;
            }

            if (playerStats.IsCoinLocked || playerStats.IsBeingPushed)
            {
                return;
            }

            _collected = true;
            if (NetworkRoundState.Instance != null)
            {
                NetworkRoundState.Instance.CollectCoinServer();
            }
            NetworkGameObjectPool.Despawn(gameObject);
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

