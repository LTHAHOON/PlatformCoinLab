using System.Text;
using TMPro;
using UnityEngine;
using CoinGame.Managers;
using CoinGame.Session;

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

        private void OnEnable()
        {
            GameManager.Instance.OnStageSessionChanged += SetStageView;
            GameManager.Instance.OnStageTimerChanged += SetTimerText;
            SetStageView(GameManager.Instance.CurrentStageSession, true);
            SetTimerText(GameManager.Instance.RemainingTime);
        }

        private void OnDisable()
        {
            GameManager.Instance.OnStageSessionChanged -= SetStageView;
            GameManager.Instance.OnStageTimerChanged -= SetTimerText;
        }

        private void SetStageView(StageSession stageSession, bool isStart)
        {
            if (isStart)
            {
                SetRoundText(stageSession.Round);
            }

            SetRemainingCoinText(stageSession.RemainingCoins);
        }

        public void SetRemainingCoinText(int remainingCoin)
        {
            _sb.Append("Remaining Coin: ");
            _sb.Append(remainingCoin);
            _remainingCoinText.SetText(_sb);
            _sb.Clear();
        }

        public void SetTimerText(float remainingTime)
        {
            int totalSeconds = Mathf.CeilToInt(Mathf.Max(remainingTime, 0f));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            _sb.Append("Timer: ");
            _sb.Append(minutes.ToString("00"));
            _sb.Append(':');
            _sb.Append(seconds.ToString("00"));
            _timerText.SetText(_sb);
            _sb.Clear();
        }

        public void SetRoundText(int curRound)
        {
            _sb.Append("Round: ");
            _sb.Append(curRound);
            _roundText.SetText(_sb);
            _sb.Clear();
        }
    }
}
