using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using CoinGame.Player;
using CoinGame.Stage;

namespace CoinGame.Network
{
    using CoinGame.Utilities;

    [RequireComponent(typeof(CharacterController))]
    public class NetworkPlayerController : NetworkBehaviour
    {
        [SerializeField]
        private float _moveSpeed = 4f;
        [SerializeField]
        private VectorMinMaxData _playerPosMinMaxData;
        private CharacterController _controller;
        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _attackAction;
        private InputAction _interactAction;
        private InputAction _crouchAction;
        private CancellationTokenSource _moveCts;
        private float _pushCooldownUntil;
        private float _lockCooldownUntil;
        private float _stealCooldownUntil;
        private Renderer[] _renderers;
        [SerializeField]
        private RoleAbilityConfig _roleAbilityConfig;

        public override void OnNetworkSpawn()
        {
            _renderers = GetComponentsInChildren<Renderer>();
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
                _attackAction = playerMap.FindAction("Attack");
                _interactAction = playerMap.FindAction("Interact");
                _crouchAction = playerMap.FindAction("Crouch");
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
                AbilityAsync(_moveCts.Token).Forget();
            }

            _moveAction.Enable();
            _attackAction?.Enable();
            _interactAction?.Enable();
            _crouchAction?.Enable();
            _controller.enabled = true;
        }

        private void Update()
        {
            if (!IsSpawned)
            {
                return;
            }

            NetworkPlayerStats stats = GetComponent<NetworkPlayerStats>();
            Color roleColor = stats.IsCoinLocked
                ? new Color(0.35f, 0.75f, 1f)
                : stats.Role == PlayerRole.Disruptor
                    ? new Color(1f, 0.45f, 0.25f)
                    : Color.white;

            foreach (Renderer renderer in _renderers)
            {
                renderer.material.color = roleColor;
            }
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
            _attackAction?.Disable();
            _interactAction?.Disable();
            _crouchAction?.Disable();
            if (_controller)
            {
                _controller.enabled = false;
            }
        }

        private async UniTaskVoid AbilityAsync(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    NetworkPlayerStats stats = GetComponent<NetworkPlayerStats>();
                    if (stats.Role == PlayerRole.Disruptor)
                    {
                        if (_attackAction != null && _attackAction.WasPressedThisFrame() && Time.time >= _pushCooldownUntil)
                        {
                            _pushCooldownUntil = Time.time + PushCooldown;
                            UsePushRpc(transform.position, transform.forward);
                        }
                        if (_interactAction != null && _interactAction.WasPressedThisFrame() && Time.time >= _lockCooldownUntil)
                        {
                            _lockCooldownUntil = Time.time + LockCooldown;
                            UseLockRpc(transform.position);
                        }
                        if (_crouchAction != null && _crouchAction.WasPressedThisFrame() && Time.time >= _stealCooldownUntil)
                        {
                            _stealCooldownUntil = Time.time + StealCooldown;
                            UseStealRpc(transform.position);
                        }
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException) { }
        }

        private float PushCooldown => _roleAbilityConfig ? _roleAbilityConfig.PushCooldown : 2f;
        private float LockCooldown => _roleAbilityConfig ? _roleAbilityConfig.LockCooldown : 8f;
        private float StealCooldown => _roleAbilityConfig ? _roleAbilityConfig.StealCooldown : 10f;
        private float LockDuration => _roleAbilityConfig ? _roleAbilityConfig.CoinLockDuration : 3f;
        private float PushForce => _roleAbilityConfig ? _roleAbilityConfig.PushForce : 5f;
        private int StealAmount => _roleAbilityConfig ? _roleAbilityConfig.StealAmount : 1;

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void UsePushRpc(Vector3 origin, Vector3 direction)
        {
            NetworkPlayerController target = FindNearestOpponent(origin);
            if (target == null)
            {
                return;
            }

            target.ReceivePushRpc(target.OwnerClientId, direction.normalized, PushForce);
        }

        [Rpc(SendTo.NotServer)]
        private void ReceivePushRpc(ulong targetClientId, Vector3 direction, float force)
        {
            if (!IsOwner || OwnerClientId != targetClientId || _controller == null)
            {
                return;
            }

            _controller.Move(direction * force);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void UseLockRpc(Vector3 origin)
        {
            NetworkPlayerController target = FindNearestOpponent(origin);
            if (target != null)
            {
                target.GetComponent<NetworkPlayerStats>().SetCoinLockServer(NetworkManager.Singleton.ServerTime.Time + LockDuration);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void UseStealRpc(Vector3 origin)
        {
            NetworkPlayerController target = FindNearestOpponent(origin);
            NetworkPlayerStats targetStats = target ? target.GetComponent<NetworkPlayerStats>() : null;
            NetworkPlayerStats ownStats = GetComponent<NetworkPlayerStats>();
            if (targetStats != null && ownStats != null && targetStats.TrySpendScore(StealAmount))
            {
                ownStats.AddScoreServer(StealAmount);
            }
        }

        private NetworkPlayerController FindNearestOpponent(Vector3 origin)
        {
            NetworkPlayerController nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null || client.PlayerObject == NetworkObject)
                {
                    continue;
                }

                NetworkPlayerController candidate = client.PlayerObject.GetComponent<NetworkPlayerController>();
                float distance = (candidate.transform.position - origin).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }
            return nearest;
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
                        Vector3 moveDelta = _moveSpeed * Time.deltaTime * moveDir.normalized;
                        Vector3 curPlayerPos = _controller.transform.position;
                        Vector3 targetPlayerPos = curPlayerPos + moveDelta;
                        targetPlayerPos = targetPlayerPos.Clamp(_playerPosMinMaxData.MinVector, _playerPosMinMaxData.MaxVetor);
                        
                        _controller.Move(targetPlayerPos - curPlayerPos);
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
