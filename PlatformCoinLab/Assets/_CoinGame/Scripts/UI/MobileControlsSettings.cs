using UnityEngine;

namespace CoinGame.UI
{
    [CreateAssetMenu(menuName = "CoinGame/UI/Mobile Controls")]
    public sealed class MobileControlsSettings : ScriptableObject
    {
        [Min(1f)]
        public float JoystickRadius = 95f;
        [Range(0f, 0.5f)]
        public float DeadZone = 0.08f;
        [Min(0.01f)]
        public float ShowDuration = 0.08f;
        [Min(0.01f)]
        public float HideDuration = 0.1f;
        [Range(0.1f, 1f)]
        public float HiddenScale = 0.8f;
        [Range(0f, 1f)]
        public float JoystickOpacity = 0.8f;
        [Range(0.6f, 1.5f)]
        public float MinimumButtonScale = 0.75f;
        [Range(1f, 2f)]
        public float MaximumButtonScale = 1.35f;
    }
}
