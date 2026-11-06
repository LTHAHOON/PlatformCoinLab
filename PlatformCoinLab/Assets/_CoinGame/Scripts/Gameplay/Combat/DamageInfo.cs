using UnityEngine;

namespace CoinGame.Gameplay.Combat
{
    public enum DamageType : byte
    {
        Generic,
        Push,
        Weapon,
        Timeout
    }

    public readonly struct DamageInfo
    {
        public readonly ulong Attacker;
        public readonly int Amount;
        public readonly DamageType Type;
        public readonly Vector3 Knockback;

        public DamageInfo(ulong attacker, int amount, DamageType type, Vector3 knockback)
        {
            Attacker = attacker;
            Amount = amount;
            Type = type;
            Knockback = knockback;
        }
    }
}
