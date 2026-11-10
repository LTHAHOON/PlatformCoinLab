using System;
using UnityEngine;

namespace CoinGame.Stage
{
    [CreateAssetMenu(menuName = "CoinGame/Stage Config")]
    public class StageConfig : ScriptableObject
    {
        [SerializeField]
        [Min(1)] 
        private int BaseCoinCount = 10;
        [SerializeField]
        [Min(0)] 
        private int CoinIncreasePerRound = 3;
        [SerializeField]
        [Min(0)] 
        private float BaseTimeLimit = 120f;
        [SerializeField]
        [Min(0)] 
        private float TimeDecreasePerRound = 2f;
        [SerializeField]
        [Min(1)] 
        private float MinimumTimeLimit = 30f;
        [SerializeField]
        [Min(0f)]
        private float TimeoutDamagePerMissingCoin = 1f;
        [SerializeField]
        [Min(0)] private int CrateMinimum = 2;
        [SerializeField]
        [Min(0)] private int CrateMaximum = 4;

        public int GetRequiredCoins(int round)
        {
            long count = BaseCoinCount + (long)((round - 1) * CoinIncreasePerRound);
            return (int)Math.Min(int.MaxValue, count);
        }

        public int GetSpawnCoinCount(int round, int coinMultiplier)
        {
            long count = (long)GetRequiredCoins(round) * Mathf.Max(1, coinMultiplier);
            return (int)Math.Min(int.MaxValue, count);
        }

        public float GetTimeLimit(int round)
        {
            float timeLimit = Mathf.Max(MinimumTimeLimit, BaseTimeLimit - (round - 1) * TimeDecreasePerRound);
            return timeLimit;
        }

        public int GetTimeoutPenalty(int missingCoins)
        {
            return (int)Math.Max(0, Math.Ceiling(missingCoins * TimeoutDamagePerMissingCoin));
        }

        public int GetCrateCount() { return UnityEngine.Random.Range(CrateMinimum, Mathf.Max(CrateMinimum, CrateMaximum) + 1); }
    }
}



