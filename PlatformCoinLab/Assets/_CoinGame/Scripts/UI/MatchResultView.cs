using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace CoinGame.UI
{
    using CoinGame.Network;
    using CoinGame.Player;
    using CoinGame.Gameplay.Stage;

    public sealed class MatchResultView : MonoBehaviour
    {
        private CanvasGroup _group;
        private TMP_Text _label;
        private NetworkRoundState _roundState;
        private bool _bound;

        private void Start()
        {
            Build();
            TryBind();
        }

        private void Update()
        {
            TryBind();
            if (_roundState != null && _roundState.MatchWinner != MatchWinner.None)
            {
                ShowResult();
            }
        }

        private void OnDestroy()
        {
            if (_bound && _roundState != null)
            {
                _roundState.MatchFinished -= OnMatchFinished;
            }
        }

        private void TryBind()
        {
            if (_bound || NetworkRoundState.Instance == null)
            {
                return;
            }

            _roundState = NetworkRoundState.Instance;
            _roundState.MatchFinished += OnMatchFinished;
            _bound = true;
        }

        private void OnMatchFinished(bool collectorWon)
        {
            ShowResult();
        }

        private void ShowResult()
        {
            if (_roundState == null || NetworkManager.Singleton == null ||
                NetworkManager.Singleton.SpawnManager == null)
            {
                return;
            }

            NetworkObject localObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            NetworkPlayerStats localStats = localObject == null
                ? null
                : localObject.GetComponent<NetworkPlayerStats>();
            if (localStats == null || localStats.Role == PlayerRole.Unselected)
            {
                return;
            }

            bool collectorWon = _roundState.MatchWinner == MatchWinner.Collector;
            bool localWon = collectorWon == (localStats.Role == PlayerRole.Collector);
            _label.text = localWon ? "WIN" : "LOSS";
            _label.color = localWon ? new Color(0.25f, 1f, 0.45f) : new Color(1f, 0.25f, 0.25f);
            _group.alpha = 1f;
            _group.interactable = false;
            _group.blocksRaycasts = true;
        }

        private void Build()
        {
            GameObject panel = new("MatchResultPanel");
            panel.transform.SetParent(transform, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image background = panel.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.72f);
            _group = panel.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            GameObject labelObject = new("MatchResultLabel");
            labelObject.transform.SetParent(panel.transform, false);
            RectTransform labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(800f, 220f);
            labelRect.anchoredPosition = Vector2.zero;
            _label = labelObject.AddComponent<TextMeshProUGUI>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 132f;
            _label.fontStyle = FontStyles.Bold;
            _label.text = string.Empty;
        }
    }
}
