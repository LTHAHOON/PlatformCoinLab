using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

namespace CoinGame.Utilities
{
    public class GameObjectPool : IDisposable
    {
        protected readonly GameObject _prefab;
        protected readonly ObjectPool<GameObject> _pool;
        protected readonly Transform _parent;
        public GameObjectPool(GameObject prefab, int defaultCapacity, int maxSize, Transform parent = null)
        {
            _prefab = prefab;
            _pool = new ObjectPool<GameObject>(Create, OnGet, OnRelease, OnDestroy,
                true, defaultCapacity, maxSize);
            _parent = parent;
        }

        public GameObject Get()
        {
            GameObject objToGet = _pool.Get();
            return objToGet;
        }

        public virtual void Release(GameObject instance)
        {
            _pool.Release(instance);
        }

        protected GameObject Create()
        {
            GameObject newObj = UnityEngine.Object.Instantiate(_prefab, _parent);
            return newObj;
        }

        protected virtual void OnGet(GameObject instance)
        {
            instance.SetActive(true);
        }

        private void OnRelease(GameObject instance) => instance.SetActive(false);

        private void OnDestroy(GameObject instance)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(instance);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        public virtual void Dispose()
        {
            _pool.Clear();
        }
    }

    internal class PooledNetworkPrefabHandler : INetworkPrefabInstanceHandler
    {
        private readonly GameObjectPool _pool;

        public PooledNetworkPrefabHandler(GameObjectPool pool) => _pool = pool;

        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            GameObject instance = _pool.Get();
            instance.transform.SetPositionAndRotation(position, rotation);
            if (!instance.TryGetComponent(out NetworkObject networkObject))
            {
                _pool.Release(instance);
                throw new InvalidOperationException("Pooled network prefab은 NetworkObject가 필요합니다");
            }
            return networkObject;
        }

        public void Destroy(NetworkObject networkObject) => _pool.Release(networkObject.gameObject);
    }
}
