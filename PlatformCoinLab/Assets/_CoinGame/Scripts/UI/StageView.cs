using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using CoinGame.Managers;
using CoinGame.Session;
using CoinGame.Network;
using CoinGame.Player;

namespace CoinGame.UI
{
    public class StageView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _remainingCoinText;
        [SerializeField]
        private TMP_Text _timerText;
        [SerializeField]
        private TMP_Text _roundText;

        private StringBuilder _sb = new();
        private NetworkPlayerStats _localStats;
        private PlayerRole _lastRole = PlayerRole.Unselected;
        private bool _isBound;
        [SerializeField]
        private TMP_Text _scoreboardText;
        [SerializeField]
        private TMP_Text _collectorScoreboardText;
        [SerializeField]
        private TMP_Text _disruptorScoreboardText;
        [SerializeField]
        private CanvasGroup _roundBannerGroup;
        [SerializeField]
        private TMP_Text _roundBannerText;
        private CancellationTokenSource _roundBannerCts;
        private float _nextScoreboardRefresh;
        private float _nextLocalStatsLookup;
        private int _lastBannerRound = -1;
        private int _roundBannerVersion;

        private void OnEnable()
        {
            TryBindGameManager();
        }

        private void OnDisable()
        {
            if (!_isBound || GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.OnStageSessionChanged -= SetStageView;
            GameManager.Instance.OnStageTimerChanged -= SetTimerText;
            _isBound = false;
        }

        private void Update()
        {
            TryBindGameManager();
            if (Time.unscaledTime >= _nextLocalStatsLookup)
            {
                _nextLocalStatsLookup = Time.unscaledTime + 0.25f;
                if ((_localStats == null || !_localStats.IsSpawned) && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
                {
                    NetworkObject localObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                    if (localObject != null)
                    {
                        _localStats = localObject.GetComponent<NetworkPlayerStats>();
                    }
                }
            }

            if (_localStats != null && _lastRole != _localStats.Role)
            {
                _lastRole = _localStats.Role;
                if (_remainingCoinText != null)
                {
                    _remainingCoinText.transform.parent.gameObject.SetActive(_lastRole != PlayerRole.Disruptor);
                }
            }

            if (Time.unscaledTime >= _nextScoreboardRefresh)
            {
                _nextScoreboardRefresh = Time.unscaledTime + 0.25f;
                RefreshScoreboard();
            }
        }

        private void TryBindGameManager()
        {
            if (_isBound || GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.OnStageSessionChanged += SetStageView;
            GameManager.Instance.OnStageTimerChanged += SetTimerText;
            SetStageView(GameManager.Instance.CurrentStageSession, true);
            SetTimerText(GameManager.Instance.RemainingTime);
            _isBound = true;
        }

        private void SetStageView(StageSession stageSession, bool isStart)
        {
            if (isStart)
            {
                SetRoundText(stageSession.Round);
                if (stageSession.Round > 0 && stageSession.Round != _lastBannerRound)
                {
                    _lastBannerRound = stageSession.Round;
                    ShowRoundBannerAsync(stageSession.Round, GameManager.Instance.RoundStartDelay).Forget();
                }
            }

            SetRemainingCoinText(stageSession.CollectedCoins, stageSession.RequiredCoins);
        }

        public void SetRemainingCoinText(int collectedCoins, int requiredCoins)
        {
            if (_remainingCoinText == null)
            {
                return;
            }

            _sb.Append(Mathf.Clamp(collectedCoins, 0, requiredCoins));
            _sb.Append('/');
            _sb.Append(Mathf.Max(0, requiredCoins));
            _remainingCoinText.SetText(_sb);
            _sb.Clear();
        }

        public void SetTimerText(float remainingTime)
        {
            if (_timerText == null)
            {
                return;
            }

            int totalSeconds = Mathf.CeilToInt(Mathf.Max(remainingTime, 0f));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            _sb.Append(minutes.ToString("00"));
            _sb.Append(':');
            _sb.Append(seconds.ToString("00"));
            _timerText.SetText(_sb);
            _sb.Clear();
        }

        public void SetRoundText(int curRound)
        {
            if (_roundText == null)
            {
                return;
            }

            _sb.Append(curRound);
            _roundText.SetText(_sb);
            _sb.Clear();
        }

        private void RefreshScoreboard()
        {
            TMP_Text collectorText = _collectorScoreboardText != null ? _collectorScoreboardText : _scoreboardText;
            if (collectorText == null || _disruptorScoreboardText == null || _localStats == null ||
                _localStats.Role == PlayerRole.Unselected)
            {
                collectorText?.SetText(string.Empty);
                _disruptorScoreboardText?.SetText(string.Empty);
                return;
            }

            List<NetworkPlayerStats> stats = new();
            if (NetworkManager.Singleton == null)
            {
                collectorText.SetText(string.Empty);
                _disruptorScoreboardText.SetText(string.Empty);
                return;
            }

            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                NetworkPlayerStats playerStats = client.PlayerObject != null
                    ? client.PlayerObject.GetComponent<NetworkPlayerStats>()
                    : null;
                if (playerStats != null)
                {
                    stats.Add(playerStats);
                }
            }
            stats.Sort((left, right) => left.OwnerClientId.CompareTo(right.OwnerClientId));

            StringBuilder collectorBuilder = new();
            StringBuilder disruptorBuilder = new();
            collectorBuilder.AppendLine("<color=#8FE7FF>COLLECTORS</color>");
            disruptorBuilder.AppendLine("<color=#FFB86B>DISRUPTORS</color>");
            foreach (NetworkPlayerStats playerStats in stats)
            {
                if (playerStats.Role == PlayerRole.Collector)
                {
                    collectorBuilder.Append("P");
                    collectorBuilder.Append(playerStats.OwnerClientId + 1);
                    collectorBuilder.Append("  Score ");
                    collectorBuilder.Append(playerStats.Score);
                    collectorBuilder.AppendLine();
                }
                else if (playerStats.Role == PlayerRole.Disruptor)
                {
                    disruptorBuilder.Append("P");
                    disruptorBuilder.Append(playerStats.OwnerClientId + 1);
                    disruptorBuilder.Append("  Disrupt ");
                    disruptorBuilder.Append(playerStats.DisruptionScore);
                    disruptorBuilder.AppendLine();
                }
            }
            collectorText.SetText(collectorBuilder);
            _disruptorScoreboardText.SetText(disruptorBuilder);
        }

        private async UniTaskVoid ShowRoundBannerAsync(int round, float startDelay)
        {
            if (_roundBannerGroup == null || _roundBannerText == null)
            {
                return;
            }

            _roundBannerCts?.Cancel();
            _roundBannerCts?.Dispose();
            _roundBannerCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            CancellationToken token = _roundBannerCts.Token;
            int bannerVersion = ++_roundBannerVersion;
            _roundBannerText.text = $"ROUND {round}";
            _roundBannerGroup.alpha = 0f;
            _roundBannerGroup.gameObject.SetActive(true);

            try
            {
                await FadeBannerAsync(0f, 1f, GameManager.RoundBannerFadeInDuration, token);
                await UniTask.Delay(TimeSpan.FromSeconds(startDelay), cancellationToken: token);
                await FadeBannerAsync(1f, 0f, 0.5f, token);
                if (bannerVersion == _roundBannerVersion)
                {
                    _roundBannerGroup.gameObject.SetActive(false);
                }
            }
            catch (OperationCanceledException)
            {
                if (bannerVersion == _roundBannerVersion && _roundBannerGroup != null)
                {
                    _roundBannerGroup.alpha = 0f;
                    _roundBannerGroup.gameObject.SetActive(false);
                }
            }
        }

        private async UniTask FadeBannerAsync(float from, float to, float duration, CancellationToken token)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                token.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                _roundBannerGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            _roundBannerGroup.alpha = to;
        }

        private void OnDestroy()
        {
            _roundBannerVersion++;
            _roundBannerCts?.Cancel();
            _roundBannerCts?.Dispose();
        }
    }
}
