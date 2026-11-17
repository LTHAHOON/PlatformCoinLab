using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoinGame.Content
{
    public sealed class BootstrapDownloadView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _status;
        [SerializeField]
        private TMP_Text _detail;
        [SerializeField]
        private Image _progress;
        [SerializeField]
        private Button _downloadButton;
        [SerializeField]
        private Button _retryButton;

        public void Show(string status, string detail, float progress = 0f)
        {
            _status.text = status;
            _detail.text = detail;
            _progress.fillAmount = progress;
        }

        public void SetButtons(bool download, bool retry)
        {
            _downloadButton.gameObject.SetActive(download);
            _retryButton.gameObject.SetActive(retry);
        }

        public Button DownloadButton => _downloadButton;
        public Button RetryButton => _retryButton;
    }
}
