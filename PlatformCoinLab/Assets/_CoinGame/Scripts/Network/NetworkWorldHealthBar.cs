using UnityEngine;

namespace CoinGame.Network
{
    using CoinGame.UI;

    public sealed class NetworkWorldHealthBar : MonoBehaviour
    {
        private NetworkPlayerStats _stats;
        private PlayerHealthBarView _view;

        private void Awake()
        {
            _stats = GetComponent<NetworkPlayerStats>();
        }

        private void LateUpdate()
        {
            // Players spawn in Persistent before the InGame Canvas is loaded.
            if (_view == null && _stats.IsSpawned && PlayerHealthBarLayer.Instance != null)
            {
                _view = PlayerHealthBarLayer.Instance.Create(transform, _stats);
            }
        }

        private void OnDisable()
        {
            if (_view != null)
            {
                Destroy(_view.gameObject);
            }
        }
    }
}
