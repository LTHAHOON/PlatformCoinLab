using UnityEngine;

namespace CoinGame.Gameplay.Items
{
    using CoinGame.Gameplay.Combat;

    public enum ItemKind : byte { Ammo, Health, Gun, Sword, Boost, Invulnerable, Stealth, Revive, Random }

    [CreateAssetMenu(fileName = "ItemData", menuName = "CoinGame/Item Data")]
    public class ItemData : ScriptableObject
    {
        public ItemKind Kind;
        [Min(0)] public int Weight = 1;
        [Min(0)] public int Amount = 24;
        [Min(0)] public float Duration = 3f;
        public WeaponData Weapon;
        [Min(0.01f)] public float DropScale = 0.22f;
        [Min(0.05f)] public float PickupRadius = 0.3f;
    }
}
