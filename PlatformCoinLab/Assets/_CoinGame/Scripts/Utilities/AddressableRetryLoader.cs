using System;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CoinGame.Utilities
{
    public static class AddressableRetryLoader
    {
        public static async UniTask<AsyncOperationHandle<T>> LoadAsync<T>(object key, int retries = 2)
        {
            for (int attempt = 0; attempt <= retries; attempt++)
            {
                AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
                await handle.ToUniTask();
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return handle;
                }

                Addressables.Release(handle);
                if (attempt == retries)
                {
                    throw handle.OperationException ?? new Exception($"Addressable load failed: {key}");
                }

                await UniTask.Delay(TimeSpan.FromSeconds(attempt + 1));
            }
            throw new InvalidOperationException("Unreachable");
        }

        public static void Release<T>(AsyncOperationHandle<T> handle)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }
}
