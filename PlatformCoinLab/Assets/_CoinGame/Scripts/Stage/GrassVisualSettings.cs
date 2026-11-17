using UnityEngine;

namespace CoinGame.Stage
{
    [CreateAssetMenu(menuName = "CoinGame/Stage/Grass Visual Settings")]
    public sealed class GrassVisualSettings : ScriptableObject
    {
        [SerializeField]
        [Range(0f, 1f)]
        private float _occupiedOpacity = 0.35f;
        [SerializeField]
        [Min(0.01f)]
        private float _fadeSeconds = 0.2f;

        public float OccupiedOpacity => _occupiedOpacity;
        public float FadeSeconds => _fadeSeconds;
    }
}
