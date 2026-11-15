using CoinGame.Gameplay.Stage;
using NUnit.Framework;

namespace CoinGame.Tests.EditMode
{
    public class RoundOutcomeEvaluatorTests
    {
        [Test]
        public void AllCollectorsDeadMeansDisruptorWin()
        {
            Assert.AreEqual(MatchWinner.Disruptor, RoundOutcomeEvaluator.EvaluateCollectorDeath(0));
        }

        [Test]
        public void AllDisruptorsDeadMeansCollectorWin()
        {
            Assert.AreEqual(MatchWinner.Collector, RoundOutcomeEvaluator.EvaluateDisruptorDeath(0));
        }

        [Test]
        public void FinalRoundRequiresCoinsAndSurvivor()
        {
            Assert.AreEqual(MatchWinner.Collector, RoundOutcomeEvaluator.EvaluateFinalRound(true, 1));
            Assert.AreEqual(MatchWinner.Disruptor, RoundOutcomeEvaluator.EvaluateFinalRound(false, 1));
        }
    }
}
