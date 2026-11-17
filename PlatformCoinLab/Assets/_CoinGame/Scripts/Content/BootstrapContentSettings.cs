using UnityEngine;

namespace CoinGame.Content
{
    [CreateAssetMenu(menuName = "CoinGame/Bootstrap Content")]
    public sealed class BootstrapContentSettings : ScriptableObject
    {
        [SerializeField]
        private string _requiredLabel = "required";
        [SerializeField]
        private string _persistentScenePath = "Assets/_CoinGame/Scenes/Persistent_CoinGameScene.unity";
        [SerializeField]
        private int _requestTimeoutSeconds = 30;
        [SerializeField]
        private int _downloadRetryCount = 2;

        public string RequiredLabel => _requiredLabel;
        public string PersistentScenePath => _persistentScenePath;
        public int RequestTimeoutSeconds => _requestTimeoutSeconds;
        public int DownloadRetryCount => _downloadRetryCount;
    }
}
