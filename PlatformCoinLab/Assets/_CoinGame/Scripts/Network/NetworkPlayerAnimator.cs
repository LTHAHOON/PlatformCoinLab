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
            if (_animatorUtility == null)
            {
                _animatorUtility = GetComponent<CharacterAnimatorUtility>();
            }
        }

        public override void OnNetworkSpawn()
        {
            _lastPosition = transform.position;
            _lastHealth = _stats != null ? _stats.Health : 0;
            _lastLifeState = _stats != null ? _stats.LifeState : PlayerLifeState.Alive;
        }

        private void Update()
        {
            if (_animatorUtility == null || _stats == null)
            {
                return;
            }

            Vector3 delta = transform.position - _lastPosition;
            delta.y = 0f;
            float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.001f);
            float targetMoveParameter = Mathf.Clamp01(speed / 4f);
            float blend = 1f - Mathf.Exp(-12f * Time.deltaTime);
            _moveParameter = Mathf.Lerp(_moveParameter, targetMoveParameter, blend);
            _animatorUtility.SetMove(_moveParameter);
            SetFloat(GetParameter("Speed", "Speed"), speed);
            SetBool(GetParameter("IsMoving", "IsMoving"), speed > 0.05f);
            SetInteger(GetParameter("WeaponType", "WeaponType"), _inventory != null && _inventory.CurrentKind == WeaponKind.Sword ? 1 : 0);
            SetBool(GetParameter("IsDead", "IsDead"), !_stats.IsAlive);

            if (_combat != null)
            {
                if (_combat.AttackVersion != _lastAttack)
                {
                    _animatorUtility.Trigger(GetParameter("Attack", "Attack"));
                }
                if (_combat.ReloadVersion != _lastReload)
                {
                    _animatorUtility.Trigger(GetParameter("Reload", "Reload"));
                }
                _lastAttack = _combat.AttackVersion;
                _lastReload = _combat.ReloadVersion;
            }

            if (_stats.Health < _lastHealth)
            {
                _animatorUtility.Trigger(GetParameter("Hit", "Hit"));
                _animatorUtility.Trigger(GetParameter("Damage", "Damage"));
            }
            if (_lastLifeState != _stats.LifeState && !_stats.IsAlive)
            {
                _animatorUtility.Trigger(GetParameter("Death", "Death"));
            }
            if (_lastLifeState != _stats.LifeState && _stats.IsAlive)
            {
                _animatorUtility.Trigger(GetParameter("Revive", "Revive"));
            }

            _lastHealth = _stats.Health;
            _lastLifeState = _stats.LifeState;
            _lastPosition = transform.position;
        }

        private void SetFloat(string parameter, float value)
        {
            _animatorUtility.SetFloat(parameter, value);
        }

        private void SetBool(string parameter, bool value)
        {
            _animatorUtility.SetBool(parameter, value);
        }

        private void SetInteger(string parameter, int value)
        {
            _animatorUtility.SetInteger(parameter, value);
        }

        private string GetParameter(string name, string fallback)
        {
            if (_parameterSet == null)
            {
                return fallback;
            }

            if (name == "Speed")
            {
                return _parameterSet.Speed;
            }
            if (name == "IsMoving")
            {
                return _parameterSet.IsMoving;
            }
            if (name == "WeaponType")
            {
                return _parameterSet.WeaponType;
            }
            if (name == "IsDead")
            {
                return _parameterSet.IsDead;
            }
            if (name == "Attack")
            {
                return _parameterSet.Attack;
            }
            if (name == "Reload")
            {
                return _parameterSet.Reload;
            }
            if (name == "Hit")
            {
                return _parameterSet.Hit;
            }
            if (name == "Damage")
            {
                return _parameterSet.Damage;
            }
            if (name == "Death")
            {
                return _parameterSet.Death;
            }
            if (name == "Revive")
            {
                return _parameterSet.Revive;
            }
            return fallback;
        }
    }
}
