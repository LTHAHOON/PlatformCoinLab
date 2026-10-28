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
                    Debug.LogError("ERROR: CharacterController을 찾을 수 없습니다.");
                    return;
                }
            }
            if (!_playerInput)
            {
                _playerInput = FindAnyObjectByType<PlayerInput>();
                if (!_playerInput)
                {
                    Debug.LogError("ERROR: PlayerInput을 찾을 수 없습니다.");
                    return;
                }
                InputActionMap playerMap = _playerInput.actions.FindActionMap("Player");
                _moveAction = playerMap.FindAction("Move");
                if (_moveAction == null)
                {
                    Debug.LogError("ERROR: PlayerInput에서 Player/Move 액션을 찾을 수 없습니다.");
                    return;
                }
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
                _moveAction.Disable();
            }
            if (_controller)
            {
                _controller.enabled = false;
            }
        }

        private async UniTaskVoid MoveAsync(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    Vector2 moveInput = _moveAction.ReadValue<Vector2>();
                    if (moveInput.sqrMagnitude < 0.0001f && Gamepad.current != null)
                    {
                        moveInput = Gamepad.current.leftStick.ReadValue();
                    }

                    Vector3 moveDir = new(moveInput.x, 0f, moveInput.y);
                    if (moveDir.sqrMagnitude > 0.0001f)
                    {
                        _controller.Move(_moveSpeed * Time.deltaTime * moveDir.normalized);
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
