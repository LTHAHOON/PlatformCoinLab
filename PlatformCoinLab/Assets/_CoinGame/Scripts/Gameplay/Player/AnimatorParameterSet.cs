using UnityEngine;

namespace CoinGame.Gameplay.Player
{
    [CreateAssetMenu(fileName = "AnimatorParameterSet", menuName = "CoinGame/Animator Parameter Set")]
    public class AnimatorParameterSet : ScriptableObject
    {
        public string Move = "MoveSpeed";
        public string Speed = "Speed";
        public string IsMoving = "IsMoving";
        public string WeaponType = "WeaponType";
        public string IsDead = "IsDead";
        public string Attack = "Attack";
        public string Reload = "Reload";
        public string Hit = "Hit";
        public string Damage = "Damage";
        public string Death = "Death";
        public string Revive = "Revive";
    }
}
