using UnityEngine;

namespace CoinGame.Stage
{
    [CreateAssetMenu(fileName = "RoleAbilityConfig", menuName = "CoinGame/Role Ability Config")]
    public class RoleAbilityConfig : ScriptableObject
    {
        [Min(0.1f)]
        public float PushCooldown = 3f;
        [Min(0.1f)]
        public float LockCooldown = 8f;
        [Min(0.1f)]
        public float StealCooldown = 10f;
        [Min(0.1f)]
        public float CoinLockDuration = 3f;
        [Min(0f)]
        public float PushForce = 5f;
        [Min(0.1f)]
        public float PushStunDuration = 0.4f;
        [Min(0.1f)]
        public float ItemBlockDuration = 5f;
        [Min(0.1f)]
        public float ItemBlockCooldown = 10f;
        [Min(1)]
        public int StealAmount = 1;
    }
}
