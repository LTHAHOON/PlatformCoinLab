using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

namespace CoinGame.Utilities
{
    public class NetworkGameObjectPool : GameObjectPool
    {
        private static readonly Dictionary<GameObject, NetworkGameObjectPool> Owners = new();
        private readonly Dictionary<GameObject, CancellationTokenSource> _leases = new();

        public NetworkGameObjectPool(GameObject prefab, int defaultCapacity, int maxSize) : base(prefab, defaultCapacity, maxSize) { }

        public CancellationToken GetDespawnToken(GameObject instance)
        {
            if (!_leases.TryGetValue(instance, out CancellationTokenSource cts))
            {
                return default;
            }
            return cts.Token;
        }

        // Pooled object: Despawn(false), non-pooled object: Despawn(true), regular object: Destroy()
        public static void Despawn(GameObject instance)
        {
            bool canUseObjectPool = Owners.TryGetValue(instance, out NetworkGameObjectPool pool);
            if (instance.TryGetComponent(out NetworkObject networkObject) && networkObject.IsSpawned)
            {
                if (!networkObject.NetworkManager.IsServer)
                {
                    return;
                }
                networkObject.Despawn(!canUseObjectPool);
            }
            else if (!canUseObjectPool)
            {
                UnityEngine.Object.Destroy(instance);
            }

            if (canUseObjectPool)
            {
                pool.Release(instance);
            }
        }

        public override void Release(GameObject instance)
        {
            if (!_leases.Remove(instance, out CancellationTokenSource lease))
            {
                return;
            }
            Owners.Remove(instance);
            lease.Cancel();
            lease.Dispose();

            base.Release(instance);
        }

        protected override void OnGet(GameObject instance)
        {
            _leases.Add(instance, new CancellationTokenSource());
            Owners.Add(instance, this);

            base.OnGet(instance);
        }

        public override void Dispose()
        {
            foreach (var entry in _leases)
            {
                Owners.Remove(entry.Key);
                entry.Value.Cancel();
                entry.Value.Dispose();
            }
            _leases.Clear();
            base.Dispose();
        }

    }


}