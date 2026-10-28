using System.Text;
using TMPro;
using UnityEngine;

namespace CoinGame.UI
{
    public class PlayerStatsView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _scoreText;

        private StringBuilder _sb;

        public void SetScoreText(int previousScore, int newScore)
        {
            if (_sb == null)
            {
                _sb = new();
            }
            _sb.Append("Score: ");
            _sb.Append(newScore);
            _scoreText.SetText(_sb);
            _sb.Clear();
        }

    }
}