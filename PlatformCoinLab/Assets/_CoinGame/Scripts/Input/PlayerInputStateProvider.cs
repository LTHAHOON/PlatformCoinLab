using UnityEngine;
using UnityEngine.InputSystem;

namespace CoinGame.Input
{
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerInputStateProvider : MonoBehaviour
    {
        [SerializeField]
        private PlayerInput _playerInput;
        private InputActionMap _map;
        private InputAction _move;
        private InputAction _attack;
        private InputAction _interact;
        private InputAction _weaponPrevious;
        private InputAction _previous;
        private InputAction _weaponNext;
        private InputAction _next;
        private InputAction _reload;
        private InputAction _boost;
        private InputAction _revive;
        private InputAction _push;
        private InputAction _skill;
        private PlayerInputState _state;

        private void Awake()
        {
            enabled = false;
            if (_playerInput == null)
            {
                _playerInput = GetComponent<PlayerInput>();
            }
        }

        private void OnEnable()
        {
            _map = _playerInput.actions.FindActionMap("Gameplay");
            if (_map == null)
            {
                return;
            }

            _move = _map.FindAction("Move");
            _attack = _map.FindAction("Attack");
            _interact = _map.FindAction("Interact");
            _weaponPrevious = _map.FindAction("WeaponPrevious");
            _previous = _map.FindAction("Previous");
            _weaponNext = _map.FindAction("WeaponNext");
            _next = _map.FindAction("Next");
            _reload = _map.FindAction("Reload");
            _boost = _map.FindAction("Boost");
            _revive = _map.FindAction("Revive");
            _push = _map.FindAction("Push");
            _skill = _map.FindAction("Skill");
        }

        private void OnDisable()
        {
            _state = default;
        }

        private void Update()
        {
            if (_move != null)
            {
                _state.Move = _move.ReadValue<Vector2>();
            }
            else
            {
                _state.Move = Vector2.zero;
            }
            _state.Attack = Pressed(_attack);
            _state.Interact = Pressed(_interact);
            _state.WeaponPrevious = Pressed(_weaponPrevious) || Pressed(_previous);
            _state.WeaponNext = Pressed(_weaponNext) || Pressed(_next);
            _state.Reload = Pressed(_reload);
            _state.Boost = Pressed(_boost);
            _state.Revive = Pressed(_revive);
            _state.Push = Pressed(_push);
            _state.Skill = Pressed(_skill);
            _state.SelectFist = false;
            _state.SelectGun = false;
            _state.SelectSword = false;

            if (MobileControlInput.Active != null)
            {
                MergeMobileState(MobileControlInput.Active.Consume());
            }
        }

        private void MergeMobileState(PlayerInputState state)
        {
            if (state.Move != Vector2.zero)
            {
                _state.Move = state.Move;
            }

            _state.Attack = _state.Attack || state.Attack;
            _state.Interact = _state.Interact || state.Interact;
            _state.WeaponPrevious = _state.WeaponPrevious || state.WeaponPrevious;
            _state.WeaponNext = _state.WeaponNext || state.WeaponNext;
            _state.Reload = _state.Reload || state.Reload;
            _state.Boost = _state.Boost || state.Boost;
            _state.Revive = _state.Revive || state.Revive;
            _state.Push = _state.Push || state.Push;
            _state.Skill = _state.Skill || state.Skill;
            _state.SelectFist = state.SelectFist;
            _state.SelectGun = state.SelectGun;
            _state.SelectSword = state.SelectSword;
        }

        private static bool Pressed(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            return action.WasPressedThisFrame();
        }

        public PlayerInputState Current => _state;

    }
}
