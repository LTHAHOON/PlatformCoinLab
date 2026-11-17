using UnityEngine;

namespace CoinGame.Utilities
{
    [CreateAssetMenu(menuName = "CoinGame/Spawn Placement Settings")]
    public sealed class SpawnPlacementSettings : ScriptableObject
    {
        public VectorMinMaxData RotationRange;
        public Vector3 PositionOffset;
        public bool AvoidBlockedFront;
        public Vector3 LocalForward = Vector3.forward;
        public LayerMask BlockingMask;
        [Min(1)]
        public int RotationAttempts = 16;
        [Min(0f)]
        public float FrontDistance = 1.2f;
        [Min(0.01f)]
        public float FrontHalfWidth = 0.4f;
        [Min(0.01f)]
        public float FrontHalfHeight = 0.3f;
        [Min(0f)]
        public float CollisionPadding = 0.02f;

        public Quaternion SampleRotation(Quaternion fallback)
        {
            if (RotationRange == null)
            {
                return fallback;
            }

            Vector3 min = Vector3.Min(RotationRange.MinVector, RotationRange.MaxVetor);
            Vector3 max = Vector3.Max(RotationRange.MinVector, RotationRange.MaxVetor);
            Vector3 euler = new(Random.Range(min.x, max.x), Random.Range(min.y, max.y), Random.Range(min.z, max.z));
            return Quaternion.Euler(euler);
        }
    }
}
