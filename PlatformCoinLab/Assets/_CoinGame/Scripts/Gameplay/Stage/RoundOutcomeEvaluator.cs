namespace CoinGame.Gameplay.Stage
{
    public enum MatchWinner : byte
    {
        None,
        Collector,
        Disruptor
    }

    public static class RoundOutcomeEvaluator
    {
        public static MatchWinner EvaluateCollectorDeath(int aliveCollectors)
        {
            return aliveCollectors <= 0 ? MatchWinner.Disruptor : MatchWinner.None;
        }

        public static MatchWinner EvaluateDisruptorDeath(int aliveDisruptors)
        {
            return aliveDisruptors <= 0 ? MatchWinner.Collector : MatchWinner.None;
        }

        public static MatchWinner EvaluateFinalRound(bool coinsCollected, int aliveCollectors)
        {
            if (!coinsCollected)
            {
                return MatchWinner.Disruptor;
            }

            return aliveCollectors > 0 ? MatchWinner.Collector : MatchWinner.Disruptor;
        }
    }
}
