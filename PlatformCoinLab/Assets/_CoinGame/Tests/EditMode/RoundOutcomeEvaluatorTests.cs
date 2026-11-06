using NUnit.Framework;
using CoinGame.Gameplay.Stage;

namespace CoinGame.Tests
{
    public class RoundOutcomeEvaluatorTests
    {
        [Test] public void AllCollectorsDeadMeansDisruptorWin() => Assert.AreEqual(MatchWinner.Disruptor, RoundOutcomeEvaluator.EvaluateCollectorDeath(0));
        [Test] public void SurvivorKeepsMatchForCollector() => Assert.AreEqual(MatchWinner.Collector, RoundOutcomeEvaluator.EvaluateFinalRound(1));
        [Test] public void TimeoutPenaltyUsesMissingCoins() => Assert.AreEqual(4, RoundOutcomeEvaluator.CalculateTimeoutPenalty(3, 1.2f));
    }
}
