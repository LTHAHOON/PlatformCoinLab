using UnityEngine;

namespace CoinGame.Gameplay.Combat
{
    public enum WeaponKind : byte { None, Gun, Sword }

    [CreateAssetMenu(fileName = "WeaponData", menuName = "CoinGame/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        public WeaponKind Kind;
        [Min(1)] public int Damage = 12;
        [Min(0.01f)] public float AttackInterval = 0.3f;
        [Min(0.1f)] public float Range = 12f;
        [Min(0.1f)] public float ProjectileSpeed = 14f;
        [Min(1)] public int MagazineSize = 12;
        [Min(0)] public int ReserveAmmo = 60;
        [Min(0.01f)] public float ArcAngle = 70f;
        [Min(0.01f)] public float CrateDamage = 2f;
        public Vector3 HandScale = Vector3.one;
        public Vector3 HandOffset = Vector3.zero;
        public Vector3 HandRotation = Vector3.zero;
        public Vector3 MuzzleOffset = new(0f, 0f, 0.55f);
    }
}
