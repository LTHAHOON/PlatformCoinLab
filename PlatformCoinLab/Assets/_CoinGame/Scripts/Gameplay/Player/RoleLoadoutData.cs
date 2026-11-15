using UnityEngine;

namespace CoinGame.Gameplay.Player
{
    using CoinGame.Gameplay.Combat;

    [CreateAssetMenu(fileName = "RoleLoadoutData", menuName = "CoinGame/Role Loadout Data")]
    public class RoleLoadoutData : ScriptableObject
    {
        public WeaponKind CollectorStartingWeapon = WeaponKind.Fist;
        public WeaponKind DisruptorStartingWeapon = WeaponKind.Fist;
        [Min(1)]
        public int CollectorBasicDamage = 8;
        [Min(1)]
        public int DisruptorBasicDamage = 12;
    }
}
