using UnityEngine;
using UnityEngine.InputSystem;

namespace CoinGame.Input
{
    public class PlayerInputStateProvider : MonoBehaviour
    {
        [SerializeField] private PlayerInput _playerInput;
        private InputActionMap _map;
        private PlayerInputState _state;
        public PlayerInputState Current => _state;

        private void Awake()
        {
            _playerInput ??= GetComponent<PlayerInput>();
            _map = _playerInput != null ? _playerInput.actions.FindActionMap("Gameplay") : null;
        }

        private void Update()
        {
            if (_map == null) return;
            _state.Move = _map.FindAction("Move")?.ReadValue<Vector2>() ?? Vector2.zero;
            _state.Attack = Pressed("Attack");
            _state.Interact = Pressed("Interact");
            _state.WeaponPrevious = Pressed("WeaponPrevious") || Pressed("Previous");
            _state.WeaponNext = Pressed("WeaponNext") || Pressed("Next");
            _state.Reload = Pressed("Reload");
            _state.Boost = Pressed("Boost");
            _state.Revive = Pressed("Revive");
            _state.Push = Pressed("Push");
            _state.Skill = Pressed("Skill");
        }

        public void SetMobileState(PlayerInputState state)
        {
            _state = state;
        }

        private bool Pressed(string actionName)
        {
            return _map.FindAction(actionName)?.WasPressedThisFrame() ?? false;
        }
    }
}
