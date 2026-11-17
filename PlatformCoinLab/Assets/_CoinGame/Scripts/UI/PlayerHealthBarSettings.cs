using UnityEngine;

namespace CoinGame.UI
{
    [CreateAssetMenu(menuName = "CoinGame/UI/Player Health Bar Settings")]
    public sealed class PlayerHealthBarSettings : ScriptableObject
    {
        public Vector3 WorldOffset = new Vector3(0f, 2.25f, 0f);
        [Min(0f)]
        public float DamageVisibleSeconds = 3f;
        [Min(0.01f)]
        public float FadeSeconds = 0.25f;
        [Min(0.01f)]
        public float HealthChangeSeconds = 0.15f;
    }
}
