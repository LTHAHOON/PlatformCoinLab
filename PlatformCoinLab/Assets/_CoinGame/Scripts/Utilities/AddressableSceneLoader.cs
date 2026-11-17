using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace CoinGame.Utilities
{
    // Scene handles and serialized asynchronous transitions share one owner.
    public sealed class AddressableSceneLoader
    {
        private readonly SemaphoreSlim _transition = new SemaphoreSlim(1, 1);
        private AsyncOperationHandle<SceneInstance> _sceneHandle;

        public async UniTask LoadAsync(string scenePath, CancellationToken token)
        {
            await _transition.WaitAsync(token);
            try
            {
                if (SceneManager.GetSceneByPath(scenePath).isLoaded)
                {
                    return;
                }

                _sceneHandle = Addressables.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
                try
                {
                    await _sceneHandle.ToUniTask();
                }
                catch
                {
                    if (_sceneHandle.IsValid())
                    {
                        Addressables.Release(_sceneHandle);
                    }
                    _sceneHandle = default;
                    throw;
                }

                if (token.IsCancellationRequested)
                {
                    await UnloadAddressableAsync();
                    token.ThrowIfCancellationRequested();
                }
            }
            finally
            {
                _transition.Release();
            }
        }

        public async UniTask UnloadAsync(string scenePath, CancellationToken token)
        {
            await _transition.WaitAsync(token);
            try
            {
                if (_sceneHandle.IsValid())
                {
                    await UnloadAddressableAsync();
                    return;
                }

                // A scene opened directly in the Editor has no Addressables handle.
                if (SceneManager.GetSceneByPath(scenePath).isLoaded)
                {
                    await SceneManager.UnloadSceneAsync(scenePath);
                }
            }
            finally
            {
                _transition.Release();
            }
        }

        private async UniTask UnloadAddressableAsync()
        {
            AsyncOperationHandle<SceneInstance> unloadHandle = Addressables.UnloadSceneAsync(_sceneHandle, false);
            try
            {
                await unloadHandle.ToUniTask();
            }
            finally
            {
                if (unloadHandle.IsValid())
                {
                    Addressables.Release(unloadHandle);
                }
                _sceneHandle = default;
            }
        }
    }
}
