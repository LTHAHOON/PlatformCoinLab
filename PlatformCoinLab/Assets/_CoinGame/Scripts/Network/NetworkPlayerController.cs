using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CoinGame.Network
{
    using CoinGame.Managers;
    using CoinGame.Player;
    using CoinGame.Stage;
    using CoinGame.Input;
    using CoinGame.Gameplay.Player;
    using CoinGame.Utilities;

    using Unity.Netcode.Components;
    using UnityEngine.AddressableAssets;

    [RequireComponent(typeof(CharacterController), typeof(NetworkTransform), typeof(NetworkPlayerStats))]
    [RequireComponent(typeof(NetworkInventory), typeof(NetworkCombat))]
    public class NetworkPlayerController : NetworkBehaviour
    {
        [SerializeField]
        private float _moveSpeed = 4f;
        [SerializeField]
        private float _rotationSpeed = 12f;
        [SerializeField]
        private bool _followPlayerCamera = false;
        [SerializeField]
        private AssetReferenceT<VectorMinMaxData> _playerPosMinMaxDataRef;
        [SerializeField]
        private AssetReferenceT<RoleAbilityConfig> _roleAbilityConfigRef;

        private VectorMinMaxData _playerPosMinMaxData;
        private RoleAbilityConfig _roleAbilityConfig;
        private NetworkPlayerStats _networkPlayerStats;
        private NetworkCombat _combat;
        private NetworkInventory _inventory;
        private CharacterController _controller;
        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _attackAction;
        private InputAction _interactAction;
        private InputAction _crouchAction;
        private PlayerInputStateProvider _inputProvider;
        private CancellationTokenSource _moveCts;
        private float _pushCooldownUntil;
        private float _lockCooldownUntil;
        private Renderer[] _playerRenderers;
        private MaterialPropertyBlock _playerPropertyBlock;
        private Camera _followCamera;
        private Vector3 _cameraOffset;
        private Quaternion _cameraRotation;
        private bool _cameraInitialized;

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void RelocateForGameRpc(Vector3 position)
        {
            if (!IsSpawned)
            {
                return;
            }

            NetworkTransform networkTransform = GetComponent<NetworkTransform>();
            if (networkTransform == null)
            {
                Debug.LogError("NetworkPlayerController requires a NetworkTransform to relocate the player.", this);
                return;
            }

            if (!IsOwner)
            {
                return;
            }

            bool wasEnabled = _controller != null && _controller.enabled;
            if (_controller != null)
            {
                _controller.enabled = false;
            }

            networkTransform.Teleport(position, transform.rotation, transform.localScale);

            if (_controller != null)
            {
                _controller.enabled = wasEnabled;
            }

            if (NetworkRoundState.Instance != null)
            {
                NetworkRoundState.Instance.ReportPositionedRpc();
            }
        }
        public override void OnNetworkSpawn()
        {
            _playerPosMinMaxDataRef.LoadAssetAsync().Completed += (handle) =>
            {
                _playerPosMinMaxData = handle.Result;
            };
            _roleAbilityConfigRef.LoadAssetAsync().Completed += (handle) =>
            {
                _roleAbilityConfig = handle.Result;
            };
            _playerRenderers = GetComponentsInChildren<Renderer>();
            _playerPropertyBlock ??= new MaterialPropertyBlock();
            _networkPlayerStats = GetComponent<NetworkPlayerStats>();
            _combat = GetComponent<NetworkCombat>();
            _inventory = GetComponent<NetworkInventory>();
            _inputProvider = GetComponent<PlayerInputStateProvider>();
            if (_networkPlayerStats == null)
            {
                Debug.LogError("NetworkPlayerController requires a NetworkPlayerStats component.", this);
                return;
            }
            if (!IsOwner)
            {
                return;
            }
            if (!_controller)
            {
                _controller = GetComponent<CharacterController>();
                if (!_controller)
                {
                    Debug.LogError("ERROR: CharacterController??嶺뚢돦堉??????怨룸????덈펲.");
                    return;
                }
            }
            if (!_inputProvider)
            {
                _inputProvider = GetComponent<PlayerInputStateProvider>();
            }
            if (!_playerInput)
            {
                _playerInput = GetComponent<PlayerInput>();
            }
            if (!_playerInput)
            {
                Debug.LogError("ERROR: PlayerInput??嶺뚢돦堉??????怨룸????덈펲.");
                return;
            }

            InputActionMap playerMap = _playerInput.actions.FindActionMap("Gameplay");
            _moveAction = playerMap != null ? playerMap.FindAction("Move") : null;
            _attackAction = playerMap != null ? playerMap.FindAction("Attack") : null;
            _interactAction = playerMap != null ? playerMap.FindAction("Interact") : null;
            _crouchAction = playerMap != null ? playerMap.FindAction("Crouch") : null;
            if (_moveAction == null)
            {
                Debug.LogError("ERROR: PlayerInput?????Gameplay/Move ???떷??嶺뚢돦堉??????怨룸????덈펲.");
                return;
            }
            if (_moveCts is null)
            {
                _moveCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                MoveAsync(_moveCts.Token).Forget();
                AbilityAsync(_moveCts.Token).Forget();
            }
            _moveAction.Enable();
            if (_attackAction != null)
            {
                _attackAction.Enable();
            }
            if (_interactAction != null)
            {
                _interactAction.Enable();
            }
            if (_crouchAction != null)
            {
                _crouchAction.Enable();
            }
            _controller.enabled = true;
        }

        private void Update()
        {
            if (!IsSpawned)
            {
                return;
            }
            if (_networkPlayerStats == null)
            {
                return;
            }

            Color roleColor = _networkPlayerStats.IsCoinLocked
                ? new Color(0.35f, 0.75f, 1f)
                : _networkPlayerStats.Role == PlayerRole.Disruptor
                    ? new Color(1f, 0.45f, 0.25f)
                    : Color.white;

            foreach (Renderer renderer in _playerRenderers)
            {
                bool hidden = _networkPlayerStats.IsStealthed && !IsFriendlyLocalPlayer();
                renderer.enabled = !hidden;
                _playerPropertyBlock.SetColor("_BaseColor", roleColor);
                _playerPropertyBlock.SetColor("_Color", roleColor);
                renderer.SetPropertyBlock(_playerPropertyBlock);
            }
        }

        private bool IsFriendlyLocalPlayer()
        {
            NetworkObject local = NetworkManager.Singleton == null ? null : NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            NetworkPlayerStats localStats = local != null ? local.GetComponent<NetworkPlayerStats>() : null;
            return localStats != null && localStats.Role == _networkPlayerStats.Role;
        }

        private void LateUpdate()
        {
            if (!_followPlayerCamera || !IsOwner || !IsSpawned || GameManager.Instance == null || !GameManager.Instance.IsInGameSceneLoaded)
            {
                return;
            }

            if (_followCamera == null || !_followCamera.isActiveAndEnabled)
            {
                _followCamera = Camera.main;
                _cameraInitialized = false;
            }
            if (_followCamera == null)
            {
                return;
            }

            if (!_cameraInitialized)
            {
                _cameraOffset = _followCamera.transform.position - transform.position;
                _cameraRotation = _followCamera.transform.rotation;
                _cameraInitialized = true;
            }

            _followCamera.transform.SetPositionAndRotation(transform.position + _cameraOffset, _cameraRotation);
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
            if (_attackAction != null)
            {
                _attackAction.Disable();
            }
            if (_interactAction != null)
            {
                _interactAction.Disable();
            }
            if (_crouchAction != null)
            {
                _crouchAction.Disable();
            }
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
                    if (_networkPlayerStats == null)
                    {
                        return;
                    }
                    if (_networkPlayerStats.Role == PlayerRole.Disruptor && _networkPlayerStats.IsAlive)
                    {
                        if (AttackPressed()) _combat.RequestAttack();
                        if (_inputProvider != null && _inputProvider.Current.Reload) _combat.RequestReload();
                        ProcessWeaponSelection();
                        if (BoostPressed()) BoostRpc();
                        if (_inputProvider != null && _inputProvider.Current.Push && Time.time >= _pushCooldownUntil)
                        {
                            _pushCooldownUntil = Time.time + PushCooldown;
                            UsePushRpc(transform.position, transform.forward);
                        }
                        if (_inputProvider != null && _inputProvider.Current.Skill && Time.time >= _lockCooldownUntil)
                        {
                            _lockCooldownUntil = Time.time + Mathf.Max(LockCooldown, ItemBlockCooldown);
                            UseLockRpc(transform.position);
                        }
                    }
                    else if (_networkPlayerStats.IsAlive)
                    {
                        if (AttackPressed()) _combat.RequestAttack();
                        if (_inputProvider != null && _inputProvider.Current.Reload) _combat.RequestReload();
                        ProcessWeaponSelection();
                        if (BoostPressed()) BoostRpc();
                    }
                    if (RevivePressed()) ReviveRpc();

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException) { }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void UsePushRpc(Vector3 origin, Vector3 direction)
        {
            if (!IsServer || _networkPlayerStats == null)
            {
                return;
            }

            NetworkPlayerController target = FindNearestOpponent(transform.position, AbilityRange);
            if (target != null && target.TryGetComponent(out NetworkPlayerStats targetStats))
            {
                if (_networkPlayerStats.Role == PlayerRole.Disruptor && targetStats.Role == PlayerRole.Collector)
                {
                    targetStats.SetBeingPushedServer(NetworkManager.Singleton.ServerTime.Time + PushStunDuration);
                    Vector3 pushDirection = transform.forward;
                    if (pushDirection.sqrMagnitude < 0.01f)
                    {
                        pushDirection = direction;
                    }

                    if (target.OwnerClientId == NetworkManager.ServerClientId)
                    {
                        target.ApplyPushServer(pushDirection.normalized, PushForce);
                    }
                    else
                    {
                        target.ReceivePushRpc(target.OwnerClientId, pushDirection.normalized, PushForce);
                    }
                }
            }
        }

        private void ApplyPushServer(Vector3 direction, float force)
        {
            if (!IsServer || _controller == null)
            {
                return;
            }

            _controller.Move(direction * force);
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
            if (!IsServer || _networkPlayerStats == null)
            {
                return;
            }

            NetworkPlayerController target = FindNearestOpponent(transform.position, AbilityRange);
            if (target != null && target.TryGetComponent(out NetworkPlayerStats targetStats))
            {
                if (_networkPlayerStats.Role == PlayerRole.Disruptor && targetStats.Role == PlayerRole.Collector)
                {
                    targetStats.SetCoinLockServer(NetworkManager.Singleton.ServerTime.Time + LockDuration);
                    if (_roleAbilityConfig != null && _roleAbilityConfig.EnableItemBlockSkill)
                    {
                        targetStats.SetItemBlockedServer(NetworkManager.Singleton.ServerTime.Time + ItemBlockDuration);
                    }
                }
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void BoostRpc()
        {
            if (!IsServer || _inventory == null || !_inventory.ConsumeBoostServer()) return;
            _networkPlayerStats.SetBoostServer(NetworkManager.Singleton.ServerTime.Time + 3d);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void ReviveRpc() { _networkPlayerStats.TryReviveServer(); }

        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        public void RestoreAfterReviveRpc()
        {
            if (_controller != null)
            {
                _controller.enabled = true;
            }
            if (_moveAction != null)
            {
                _moveAction.Enable();
            }
            if (_attackAction != null)
            {
                _attackAction.Enable();
            }
        }

        private NetworkPlayerController FindNearestOpponent(Vector3 origin, float range)
        {
            if (!IsServer || NetworkManager.Singleton == null)
            {
                return null;
            }

            NetworkPlayerController nearest = null;
            float nearestDistance = float.MaxValue;
            float rangeSqr = range * range;
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null || client.PlayerObject == NetworkObject)
                {
                    continue;
                }

                NetworkPlayerController candidate = client.PlayerObject.GetComponent<NetworkPlayerController>();
                if (candidate == null)
                {
                    continue;
                }

                NetworkPlayerStats candidateStats = candidate.GetComponent<NetworkPlayerStats>();
                if (candidateStats == null || !candidateStats.IsAlive)
                {
                    continue;
                }

                float distance = (candidate.transform.position - origin).sqrMagnitude;
                if (distance <= rangeSqr && distance < nearestDistance)
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
                    if (GameManager.Instance != null && !GameManager.Instance.CanPlayersMove || _networkPlayerStats != null && (!_networkPlayerStats.IsAlive || _networkPlayerStats.IsStunned))
                    {
                        await UniTask.Yield(PlayerLoopTiming.EarlyUpdate, token);
                        continue;
                    }
                    if (_playerPosMinMaxData == null)
                    {
                        await UniTask.Yield(PlayerLoopTiming.EarlyUpdate, token);
                        continue;
                    }

                    Vector2 moveInput = _inputProvider != null ? _inputProvider.Current.Move : Vector2.zero;
                    if (moveInput.sqrMagnitude < 0.0001f && Gamepad.current != null)
                    {
                        moveInput = Gamepad.current.leftStick.ReadValue();
                    }

                    Vector3 moveDir = new(moveInput.x, 0f, moveInput.y);
                    if (moveDir.sqrMagnitude > 0.0001f)
                    {
                        if (_combat != null) _combat.AimDirection = moveDir.normalized;
                        Quaternion targetRotation = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation,
                            targetRotation,
                            Mathf.Clamp01(_rotationSpeed * Time.deltaTime));
                        float speed = _moveSpeed * (_networkPlayerStats != null && _networkPlayerStats.IsBoosted ? 1.5f : 1f);
                        Vector3 moveDelta = speed * Time.deltaTime * moveDir.normalized;
                        Vector3 curPlayerPos = _controller.transform.position;
                        Vector3 targetPlayerPos = curPlayerPos + moveDelta;
                        targetPlayerPos.x = Mathf.Clamp(targetPlayerPos.x, _playerPosMinMaxData.MinVector.x, _playerPosMinMaxData.MaxVetor.x);
                        targetPlayerPos.z = Mathf.Clamp(targetPlayerPos.z, _playerPosMinMaxData.MinVector.z, _playerPosMinMaxData.MaxVetor.z);

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

        private bool AttackPressed()
        {
            if (_inputProvider != null && _inputProvider.Current.Attack)
            {
                return true;
            }

            if (_attackAction != null && _attackAction.WasPressedThisFrame())
            {
                return true;
            }

            return Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame;
        }

        private void ProcessWeaponSelection()
        {
            if (Keyboard.current == null || _combat == null)
            {
                return;
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                _combat.RequestSelectWeapon(Gameplay.Combat.WeaponKind.Fist);
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                _combat.RequestSelectWeapon(Gameplay.Combat.WeaponKind.Gun);
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                _combat.RequestSelectWeapon(Gameplay.Combat.WeaponKind.Sword);
            }
        }

        private bool BoostPressed()
        {
            return _inputProvider != null && _inputProvider.Current.Boost ||
                Keyboard.current != null && Keyboard.current.digit4Key.wasPressedThisFrame;
        }

        private bool RevivePressed()
        {
            return _inputProvider != null && _inputProvider.Current.Revive ||
                Keyboard.current != null && Keyboard.current.digit5Key.wasPressedThisFrame;
        }


        private float PushCooldown => _roleAbilityConfig ? _roleAbilityConfig.PushCooldown : 2f;
        private float LockCooldown => _roleAbilityConfig ? _roleAbilityConfig.LockCooldown : 10f;
        private float LockDuration => _roleAbilityConfig ? _roleAbilityConfig.CoinLockDuration : 5f;
        private float PushForce => _roleAbilityConfig ? _roleAbilityConfig.PushForce : 5f;
        private float AbilityRange => _roleAbilityConfig ? _roleAbilityConfig.AbilityRange : 2.5f;
        private float PushStunDuration => _roleAbilityConfig ? _roleAbilityConfig.PushStunDuration : 1f;
        private float ItemBlockDuration => _roleAbilityConfig ? _roleAbilityConfig.ItemBlockDuration : 5f;
        private float ItemBlockCooldown => _roleAbilityConfig ? _roleAbilityConfig.ItemBlockCooldown : 10f;
        public float PushCooldownRemaining => Mathf.Max(0f, _pushCooldownUntil - Time.time);
        public float SkillCooldownRemaining => Mathf.Max(0f, _lockCooldownUntil - Time.time);

    }
}

