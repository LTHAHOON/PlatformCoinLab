using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace CoinGame.Network
{
    public sealed class RoomLobbyContentService
    {
        private readonly ContentServerHealthProbe _healthProbe;

        public RoomLobbyContentService(string healthUrl)
        {
            _healthProbe = new ContentServerHealthProbe(healthUrl);
        }

        public async UniTask<bool> IsAvailableAsync(System.Threading.CancellationToken token)
        {
            if (!await UsesRemoteContentAsync(token))
            {
                return true;
            }
            return await _healthProbe.IsAvailableWithRetryAsync(token);
        }

        private static async UniTask<bool> UsesRemoteContentAsync(System.Threading.CancellationToken token)
        {
            try
            {
                AsyncOperationHandle<IResourceLocator> handle = Addressables.InitializeAsync();
                await handle.ToUniTask(cancellationToken: token);
                foreach (IResourceLocator locator in Addressables.ResourceLocators)
                foreach (object key in locator.Keys)
                {
                    if (!locator.Locate(key, typeof(object), out IList<IResourceLocation> locations)) continue;
                    foreach (IResourceLocation location in locations)
                    if (Uri.TryCreate(location.InternalId, UriKind.Absolute, out Uri uri) &&
                        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)) return true;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"Unable to determine Addressables content mode. {exception.Message}");
                return true;
            }
            return false;
        }
    }
}
