using UnityEngine;

namespace CoinGame.Gameplay.Player
{
    [CreateAssetMenu(fileName = "PlayerStatsConfig", menuName = "CoinGame/Player Stats Config")]
    public class PlayerStatsConfig : ScriptableObject
    {
        [Min(1)] public int MaxHealth = 100;
        [Min(0)] public float ReviveReadyDelay = 3f;
        [Range(1, 100)] public int RoundClearReviveHealthPercent = 50;
    }
}
