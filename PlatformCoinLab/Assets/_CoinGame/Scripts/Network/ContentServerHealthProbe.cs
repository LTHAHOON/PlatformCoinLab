using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace CoinGame.Network
{
    public sealed class ContentServerHealthProbe
    {
        private const string DefaultHealthUrl = "https://localhost:5443/health";
        private readonly string _healthUrl;

        public ContentServerHealthProbe(string healthUrl)
        {
            _healthUrl = string.IsNullOrWhiteSpace(healthUrl)
                ? DefaultHealthUrl
                : healthUrl.TrimEnd('/');
        }

        public async UniTask<bool> IsAvailableAsync(CancellationToken cancellationToken)
        {
            using UnityWebRequest request = UnityWebRequest.Get(_healthUrl);
            if (Uri.TryCreate(_healthUrl, UriKind.Absolute, out Uri uri) &&
                uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                request.certificateHandler = new LocalhostCertificateHandler();
            }

            request.timeout = 3;
            try
            {
                await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
                return request.result == UnityWebRequest.Result.Success;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private sealed class LocalhostCertificateHandler : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                return true;
            }
        }
    }
}
