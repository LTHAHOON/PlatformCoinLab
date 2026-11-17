using Unity.Netcode;
using UnityEngine;

namespace CoinGame.Network
{
    using CoinGame.Gameplay.Combat;
    using CoinGame.Gameplay.Player;

    public class NetworkPlayerAnimator : NetworkBehaviour
    {
        [SerializeField]
        private AnimatorParameterSet _parameterSet;
        [SerializeField]
        private CharacterAnimatorUtility _animatorUtility;
        private NetworkPlayerStats _stats;
        private NetworkInventory _inventory;
        private NetworkCombat _combat;
        private Vector3 _lastPosition;
        private int _lastAttack;
        private int _lastReload;
        private int _lastHealth;
        private PlayerLifeState _lastLifeState;
        private float _moveParameter;

        private void Awake()
        {
            _stats = GetComponent<NetworkPlayerStats>();
            _inventory = GetComponent<NetworkInventory>();
            _combat = GetComponent<NetworkCombat>();
        }

        public override void OnNetworkSpawn()
        {
            _lastPosition = transform.position;
            _lastHealth = _stats.Health;
            _lastLifeState = _stats.LifeState;
            _lastAttack = _combat.AttackVersion;
            _lastReload = _combat.ReloadVersion;
            _moveParameter = 0f;
            ResetTransientTriggers();
            _animatorUtility.SetMove(0f);
            _animatorUtility.SetBool(_parameterSet.IsDead, !_stats.IsAlive);
        }

        private void Update()
        {
            if (!IsSpawned || Time.deltaTime <= 0f)
            {
                return;
            }

            UpdateMovement();
            _animatorUtility.SetInteger(_parameterSet.WeaponType, (int)_inventory.CurrentKind);
            _animatorUtility.SetBool(_parameterSet.IsDead, !_stats.IsAlive);
            if (_lastLifeState != _stats.LifeState)
            {
                ResetTransientTriggers();
            }

            if (_stats.IsAlive)
            {
                UpdateActions();
            }

            _lastAttack = _combat.AttackVersion;
            _lastReload = _combat.ReloadVersion;
            _lastHealth = _stats.Health;
            _lastLifeState = _stats.LifeState;
            _lastPosition = transform.position;
        }

        private void UpdateMovement()
        {
            Vector3 delta = transform.position - _lastPosition;
            delta.y = 0f;
            float speed = delta.magnitude / Time.deltaTime;
            if (!_stats.IsAlive)
            {
                speed = 0f;
            }

            float target = Mathf.Clamp01(speed / _parameterSet.FullMoveSpeed);
            float blend = 1f - Mathf.Exp(-_parameterSet.MoveBlendResponse * Time.deltaTime);
            _moveParameter = Mathf.Lerp(_moveParameter, target, blend);
            _animatorUtility.SetMove(_moveParameter);
            _animatorUtility.SetFloat(_parameterSet.Speed, speed);
            _animatorUtility.SetBool(_parameterSet.IsMoving, speed > _parameterSet.MovingThreshold);
        }

        private void UpdateActions()
        {
            if (_inventory.CurrentKind == WeaponKind.Gun)
            {
                if (_combat.AttackVersion != _lastAttack)
                {
                    _animatorUtility.Trigger(_parameterSet.Attack);
                }

                if (_combat.ReloadVersion != _lastReload)
                {
                    _animatorUtility.Trigger(_parameterSet.Reload);
                }
            }

            if (_stats.Health < _lastHealth)
            {
                _animatorUtility.Trigger(_parameterSet.Hit);
            }
        }

        private void ResetTransientTriggers()
        {
            _animatorUtility.ResetTrigger(_parameterSet.Attack);
            _animatorUtility.ResetTrigger(_parameterSet.Reload);
            _animatorUtility.ResetTrigger(_parameterSet.Hit);
        }
    }
}