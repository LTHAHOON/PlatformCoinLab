using System;
using UnityEngine;

namespace CoinGame.Session
{
    using CoinGame.Stage;

    public struct StageSession
    {
        public int Round { get; }
        public int RequiredCoins { get; }
        public float TimeLimit { get; }

        public int CollectedCoins { get; private set; }

        public int RemainingCoins => Math.Max(0, RequiredCoins - CollectedCoins);
        public bool IsCleared => CollectedCoins >= RequiredCoins;

        public StageSession(int round, StageConfig config)
        {
            Round = round;
            RequiredCoins = config.GetRequiredCoins(round);
            TimeLimit = config.GetTimeLimit(round);

            CollectedCoins = 0;
        }
        
        public void CollectCoin()
        {
            CollectedCoins++;
        }
    }
}
