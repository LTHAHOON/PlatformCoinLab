using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;

namespace CoinGame.Content
{
    public sealed class ContentBootstrap : MonoBehaviour
    {
        [SerializeField]
        private BootstrapContentSettings _settings;
        [SerializeField]
        private BootstrapDownloadView _view;
        private bool _busy;
        private Action<UnityWebRequest> _previousOverride;
        private Action<UnityWebRequest> _requestOverride;

        private void Start()
        {
            _view.DownloadButton.onClick.AddListener(Download);
            _view.RetryButton.onClick.AddListener(Check);
            _previousOverride = Addressables.WebRequestOverride;
            _requestOverride = ConfigureRequest;
            Addressables.WebRequestOverride = _requestOverride;
            Check();
        }

        private void ConfigureRequest(UnityWebRequest request)
        {
            if (_previousOverride != null)
            {
                _previousOverride(request);
            }
            request.timeout = _settings.RequestTimeoutSeconds;
        }

        private void Check()
        {
            RunAsync(false).Forget();
        }

        private void Download()
        {
            RunAsync(true).Forget();
        }

        private async UniTask RunAsync(bool download)
        {
            if (_busy)
            {
                return;
            }
            _busy = true;
            _view.SetButtons(false, false);
            CancellationToken token = destroyCancellationToken;
            try
            {
                if (download)
                {
                    await DownloadAsync(token);
                    await EnterAsync(token);
                    return;
                }

                RequiredContentState.Reset();
                _view.Show("콘텐츠 확인 중", "최신 리소스 목록을 확인하고 있습니다.");
                await CompleteAsync(Addressables.InitializeAsync(false), token);
                _view.Show("업데이트 확인 중", "서버의 최신 콘텐츠 버전을 확인하고 있습니다.");
                List<string> catalogs = await CompleteAsync(Addressables.CheckForCatalogUpdates(false), token);
                if (catalogs.Count > 0)
                {
                    _view.Show("목록 갱신 중", "최신 콘텐츠 목록을 적용하고 있습니다.");
                    await CompleteAsync(Addressables.UpdateCatalogs(catalogs, false), token);
                }
                _view.Show("다운로드 용량 확인 중", "필수 게임 리소스의 캐시를 확인하고 있습니다.");
                IList<IResourceLocation> locations = await CompleteAsync(Addressables.LoadResourceLocationsAsync(_settings.RequiredLabel), token);
                if (locations.Count == 0)
                {
                    throw new InvalidOperationException("필수 콘텐츠가 없습니다. Addressables 빌드와 required 라벨을 확인하세요.");
                }
                long size = await CompleteAsync(Addressables.GetDownloadSizeAsync(_settings.RequiredLabel), token);
                if (size == 0)
                {
                    await EnterAsync(token);
                    return;
                }
                _view.Show("게임 리소스 다운로드", $"필요 용량 {size / 1048576f:F2} MB\nWi-Fi 연결을 권장합니다.");
                _view.SetButtons(true, false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                RequiredContentState.Reset();
                if (!token.IsCancellationRequested)
                {
                    _view.Show("다운로드를 완료하지 못했습니다", "서버 주소와 네트워크 연결을 확인한 뒤 다시 시도해 주세요.");
                    _view.SetButtons(false, true);
                    Debug.LogWarning($"[ContentBootstrap] {exception.Message}", this);
                }
            }
            finally
            {
                _busy = false;
            }
        }

        private async UniTask DownloadAsync(CancellationToken token)
        {
            AsyncOperationHandle handle = Addressables.DownloadDependenciesAsync(_settings.RequiredLabel, false);
            try
            {
                while (!handle.IsDone)
                {
                    token.ThrowIfCancellationRequested();
                    DownloadStatus progress = handle.GetDownloadStatus();
                    _view.Show("게임 리소스 다운로드 중", $"{progress.DownloadedBytes / 1048576f:F2} / {progress.TotalBytes / 1048576f:F2} MB  ·  {progress.Percent:P0}", progress.Percent);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw handle.OperationException;
                }
            }
            finally
            {
                ReleaseWhenComplete(handle);
            }
        }

        private async UniTask EnterAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            RequiredContentState.MarkReady(_settings.RequiredLabel);
            _view.Show("준비 완료", "게임을 시작합니다.", 1f);
            await UniTask.NextFrame(cancellationToken: token);
            await SceneManager.LoadSceneAsync(_settings.PersistentScenePath, LoadSceneMode.Single);
        }

        private async UniTask<T> CompleteAsync<T>(AsyncOperationHandle<T> handle, CancellationToken token)
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            using IDisposable timeoutTimer = timeout.CancelAfterSlim(TimeSpan.FromSeconds(_settings.RequestTimeoutSeconds), DelayType.Realtime);
            try
            {
                return await handle.ToUniTask(cancellationToken: timeout.Token);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                throw new TimeoutException("Content request timed out.");
            }
            finally
            {
                ReleaseWhenComplete(handle);
            }
        }

        private static void ReleaseWhenComplete(AsyncOperationHandle handle)
        {
            if (!handle.IsValid())
            {
                return;
            }
            if (handle.IsDone)
            {
                Addressables.Release(handle);
                return;
            }
            handle.Completed += operation => Addressables.Release(operation);
        }

        private void OnDestroy()
        {
            _view.DownloadButton.onClick.RemoveListener(Download);
            _view.RetryButton.onClick.RemoveListener(Check);
            if (Addressables.WebRequestOverride == _requestOverride)
            {
                Addressables.WebRequestOverride = _previousOverride;
            }
        }
    }
}
