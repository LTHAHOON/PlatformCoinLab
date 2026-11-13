using UnityEngine;

namespace CoinGame.Gameplay.Combat
{
    public abstract class WeaponBase
    {
        protected readonly WeaponData Data;
        protected WeaponBase(WeaponData data) { Data = data; }

        public WeaponData Definition => Data;

    }
}
