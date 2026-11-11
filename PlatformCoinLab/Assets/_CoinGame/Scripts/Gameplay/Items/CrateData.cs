using UnityEngine;

namespace CoinGame.Gameplay.Items
{
    [CreateAssetMenu(menuName = "CoinGame/Crate Data")]
    public class CrateData : ScriptableObject
    {
        [Min(1)] public int MaxHealth = 30;
        [Min(1)] public int OpenedHitCount = 2;
        [Min(0f)] public float LidOpenAngle = 105f;
        [Min(0.01f)] public float LidOpenDuration = 0.35f;
        public Vector3 LidRotationAxis = Vector3.right;
        public Vector3 DropOffset = new(0f, 0.8f, 0.6f);
        [Min(0.01f)] public float DestroyDuration = 0.2f;
    }
}
