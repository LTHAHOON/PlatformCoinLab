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

        public static MatchWinner EvaluateFinalRound(int aliveCollectors)
        {
            return aliveCollectors > 0 ? MatchWinner.Collector : MatchWinner.Disruptor;
        }

        public static int CalculateTimeoutPenalty(int missingCoins, float coefficient)
        {
            return (int)System.Math.Max(0, System.Math.Ceiling(missingCoins * coefficient));
        }
    }
}
