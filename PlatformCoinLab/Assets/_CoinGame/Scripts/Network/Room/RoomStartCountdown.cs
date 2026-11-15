using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CoinGame.Network
{
    public sealed class RoomStartCountdown : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _countdownText;

        private bool _isRunning;
        private int _remainingSeconds;

        public bool IsRunning => _isRunning;
        public event Action Completed;

        public void ShowRemainingSeconds(int seconds)
        {
            _remainingSeconds = Mathf.Max(0, seconds);
            _isRunning = _remainingSeconds > 0;
            UpdateText();
        }

        public void StartCountdown(int seconds)
        {
            if (_isRunning || seconds <= 0)
            {
                return;
            }

            RunCountdownAsync(seconds).Forget();
        }

        public void Cancel()
        {
            _isRunning = false;
            _remainingSeconds = 0;
            UpdateText();
        }

        private async UniTaskVoid RunCountdownAsync(int seconds)
        {
            _isRunning = true;
            _remainingSeconds = seconds;
            UpdateText();

            while (_isRunning && _remainingSeconds > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: destroyCancellationToken);
                if (!_isRunning)
                {
                    return;
                }

                _remainingSeconds--;
                UpdateText();
            }

            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;
            UpdateText();
            if (Completed != null)
            {
                Completed.Invoke();
            }
        }

        private void UpdateText()
        {
            if (_countdownText == null)
            {
                return;
            }

            _countdownText.gameObject.SetActive(_isRunning);
            _countdownText.text = _isRunning ? $"Starting in {_remainingSeconds}" : string.Empty;
        }
    }
}
