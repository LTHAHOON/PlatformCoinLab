using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CoinGame.Network
{
    [RequireComponent(typeof(CharacterController))]
    public class NetworkPlayerController : NetworkBehaviour
    {
        [SerializeField]
        private float _moveSpeed = 4f;
        private CharacterController _controller;
        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private CancellationTokenSource _moveCts;
        private Vector3 _moveDir = Vector3.zero;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                return;
            }
            if (!_controller)
            {
                _controller = GetComponent<CharacterController>();
                if (!_controller)
                {
                    Debug.LogError("ERROR: CharacterController�?찾을 ???�습?�다.");
                    return;
                }
            }
            if (!_playerInput)
            {
                _playerInput = FindAnyObjectByType<PlayerInput>();
                if (!_playerInput)
                {
                    Debug.LogError("ERROR: PlayerInput??찾을 ???�습?�다.");
                    return;
                }
                _moveAction = _playerInput.actions["Move"];
                _moveAction.performed += OnMove;
                _moveAction.canceled += OnMove;
            }
            if (_moveCts is null)
            {
                _moveCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                MoveAsync(_moveCts.Token).Forget();
            }

            _moveAction.Enable();
            _controller.enabled = true;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
            {
                return;
            }

            if (_moveCts is not null)
            {
                _moveCts.Cancel();
                _moveCts.Dispose();
                _moveCts = null;
            }
            if (_moveAction is not null)
            {
                _moveAction.performed -= OnMove;
                _moveAction.canceled -= OnMove;
                _moveAction.Disable();
            }
            if (_controller)
            {
                _controller.enabled = false;
            }
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            Vector2 moveDir = context.ReadValue<Vector2>();
            _moveDir = new(moveDir.x, 0f, moveDir.y);
            _moveDir.Normalize();
        }

        private async UniTaskVoid MoveAsync(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    if (_moveDir != Vector3.zero)
                    {
                        _controller.Move(_moveSpeed * Time.deltaTime * _moveDir);
                    }
                    await UniTask.Yield(PlayerLoopTiming.EarlyUpdate, token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

    }
}

