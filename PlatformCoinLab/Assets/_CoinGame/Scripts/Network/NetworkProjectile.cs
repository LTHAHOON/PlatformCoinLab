using Unity.Netcode;
using UnityEngine;

namespace CoinGame.Network
{
    using CoinGame.Gameplay.Combat;
    using CoinGame.Utilities;
    using CoinGame.Player;

    public sealed class NetworkProjectile : NetworkBehaviour
    {
        private Vector3 _direction;
        private float _speed;
        private float _remaining;
        private int _damage;
        private ulong _attacker;
        public void InitializeServer(Vector3 direction, float speed, float range, int damage, ulong attacker)
        {
            if (!IsServer)
            {
                return;
            }

            _direction = direction;
            _speed = speed;
            _remaining = range;
            _damage = damage;
            _attacker = attacker;
        }
        private void Update()
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }
            float step = _speed * Time.deltaTime;
            if (Physics.Raycast(transform.position, _direction, out RaycastHit hit, step))
            {
                if (hit.collider.TryGetComponent(out NetworkPlayerStats target) &&
                    target.Role != PlayerRole.Unselected)
                {
                    target.ApplyDamageServer(new DamageInfo(_attacker, _damage, DamageType.Weapon, _direction * 2f));
                }

                if (hit.collider.TryGetComponent(out NetworkCrate crate))
                {
                    crate.ApplyWeaponHitServer(WeaponKind.Gun, _damage, _attacker);
                }

                NetworkGameObjectPool.Despawn(gameObject);
                return;
            }
            transform.position += _direction * step;
            _remaining -= step;
            if (_remaining <= 0f)
            {
                NetworkGameObjectPool.Despawn(gameObject);
            }
        }
    }
}
