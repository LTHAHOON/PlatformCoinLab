using Unity.Netcode;
using UnityEngine;

namespace CoinGame.UI
{
    using CoinGame.Managers;

    public sealed class MobileMovementControls : MonoBehaviour
    {
        [SerializeField]
        private bool _previewInEditor;
        [SerializeField]
        private FloatingJoystick _joystick;

        private void Awake()
        {
            if (!Application.isMobilePlatform && !(Application.isEditor && _previewInEditor))
            {
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient ||
                GameManager.Instance == null || !GameManager.Instance.CanPlayersMove)
            {
                _joystick.ResetPointer();
            }
        }
    }
}
