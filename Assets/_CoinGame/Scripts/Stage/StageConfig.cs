using System;
using UnityEngine;

namespace CoinGame.Stage
{
    [CreateAssetMenu(menuName = "CoinGame/Stage Config")]
    public class StageConfig : ScriptableObject
    {
        [SerializeField]
        [Min(1)] 
        private int BaseCoinCount = 5;
        [SerializeField]
        [Min(0)] 
        private int CoinIncreasePerRound = 5;
        [SerializeField]
        [Min(0)] 
        private float BaseTimeLimit = 120f;
        [SerializeField]
        [Min(0)] 
        private float TimeDecreasePerRound = 2f;
        [SerializeField]
        [Min(1)] 
        private float MinimumTimeLimit = 30f;

        public int GetRequiredCoins(int round)
        {
            long count = BaseCoinCount + (long)((round - 1) * CoinIncreasePerRound);
            return (int)Math.Min(int.MaxValue, count);
        }

        public float GetTimeLimit(int round)
        {
            float timeLimit = Mathf.Max(MinimumTimeLimit, BaseTimeLimit - (round - 1) * TimeDecreasePerRound);
            return timeLimit;
        }
    }
}
