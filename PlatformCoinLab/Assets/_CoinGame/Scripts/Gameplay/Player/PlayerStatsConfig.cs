using UnityEngine;

namespace CoinGame.Gameplay.Player
{
    [CreateAssetMenu(fileName = "PlayerStatsConfig", menuName = "CoinGame/Player Stats Config")]
    public class PlayerStatsConfig : ScriptableObject
    {
        [Min(1)] public int MaxHealth = 100;
        [Min(0)] public float DisruptorRespawnDelay = 5f;
        [Min(0)] public float ReviveReadyDelay = 3f;
        [Min(0)] public float TimeoutDamagePerMissingCoin = 1f;
    }
}
