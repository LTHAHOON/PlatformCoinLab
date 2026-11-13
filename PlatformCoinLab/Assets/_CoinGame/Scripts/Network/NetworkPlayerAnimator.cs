using Unity.Netcode;
using UnityEngine;

namespace CoinGame.Network
{
    using CoinGame.Gameplay.Combat;
    using CoinGame.Gameplay.Player;

    public class NetworkPlayerAnimator : NetworkBehaviour
    {
        private Animator _animator;
        private NetworkPlayerStats _stats;
        private NetworkInventory _inventory;
        private NetworkCombat _combat;
        private Vector3 _lastPosition;
        private int _lastAttack;
        private int _lastReload;
        private int _lastHealth;
        private PlayerLifeState _lastLifeState;

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            _stats = GetComponent<NetworkPlayerStats>();
            _inventory = GetComponent<NetworkInventory>();
            _combat = GetComponent<NetworkCombat>();
        }

        public override void OnNetworkSpawn()
        {
            _lastPosition = transform.position;
            _lastHealth = _stats != null ? _stats.Health : 0;
            _lastLifeState = _stats != null ? _stats.LifeState : PlayerLifeState.Alive;
        }

        private void Update()
        {
            if (_animator == null || _stats == null) return;
            Vector3 delta = transform.position - _lastPosition;
            delta.y = 0f;
            float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.001f);
            _animator.SetFloat("Speed", speed);
            _animator.SetBool("IsMoving", speed > 0.05f);
            _animator.SetInteger("WeaponType", _inventory != null && _inventory.CurrentKind == WeaponKind.Sword ? 1 : 0);
            _animator.SetBool("IsDead", !_stats.IsAlive);
            if (_combat != null)
            {
                if (_combat.AttackVersion != _lastAttack) Trigger("Attack");
                if (_combat.ReloadVersion != _lastReload) Trigger("Reload");
                _lastAttack = _combat.AttackVersion;
                _lastReload = _combat.ReloadVersion;
            }
            if (_stats.Health < _lastHealth)
            {
                Trigger("Hit");
                Trigger("Damage");
            }
            if (_lastLifeState != _stats.LifeState && !_stats.IsAlive) Trigger("Death");
            if (_lastLifeState != _stats.LifeState && _stats.IsAlive) Trigger("Revive");
            _lastHealth = _stats.Health;
            _lastLifeState = _stats.LifeState;
            _lastPosition = transform.position;
        }

        private void Trigger(string parameter)
        {
            if (_animator.HasParameter(parameter)) _animator.SetTrigger(parameter);
        }
    }

    internal static class AnimatorExtensions
    {
        public static bool HasParameter(this Animator animator, string name)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.name == name) return true;
            }
            return false;
        }
    }
}
