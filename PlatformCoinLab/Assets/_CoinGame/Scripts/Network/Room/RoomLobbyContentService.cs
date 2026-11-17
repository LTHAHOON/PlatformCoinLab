using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CoinGame.Network
{
    using CoinGame.Content;

    public sealed class RoomLobbyContentService
    {
        private const string RequiredLabel = "required";

        public async UniTask<bool> IsAvailableAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (RequiredContentState.IsReadyFor(RequiredLabel))
            {
                return true;
            }

            AsyncOperationHandle<long> handle = Addressables.GetDownloadSizeAsync(RequiredLabel);
            try
            {
                long remainingBytes = await handle.ToUniTask(cancellationToken: token);
                return remainingBytes == 0;
            }
            finally
            {
                if (handle.IsDone)
                {
                    Addressables.Release(handle);
                }
                else
                {
                    handle.Completed += operation => Addressables.Release(operation);
                }
            }
        }
    }
}
