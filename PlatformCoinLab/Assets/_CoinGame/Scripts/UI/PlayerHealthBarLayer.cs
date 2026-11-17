using UnityEngine;

namespace CoinGame.UI
{
    using CoinGame.Network;

    public sealed class PlayerHealthBarLayer : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _root;
        [SerializeField]
        private Camera _worldCamera;
        [SerializeField]
        private Canvas _canvas;
        [SerializeField]
        private PlayerHealthBarView _prefab;
        [SerializeField]
        private PlayerHealthBarSettings _settings;

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public PlayerHealthBarView Create(Transform target, NetworkPlayerStats stats)
        {
            PlayerHealthBarView view = Instantiate(_prefab, _root);
            view.Bind(target, stats, _root, _worldCamera, _canvas, _settings);
            return view;
        }

        public static PlayerHealthBarLayer Instance { get; private set; }
    }
}
