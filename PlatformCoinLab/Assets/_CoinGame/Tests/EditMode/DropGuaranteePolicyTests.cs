using CoinGame.Gameplay.Items;
using NUnit.Framework;

namespace CoinGame.Tests.EditMode
{
    public class DropGuaranteePolicyTests
    {
        [Test]
        public void RoundOneGuaranteesOneAttackItemPerCollector()
        {
            Assert.IsTrue(DropGuaranteePolicy.ShouldGuaranteeAttackItem(1, 2, 0));
            Assert.IsTrue(DropGuaranteePolicy.ShouldGuaranteeAttackItem(1, 2, 1));
            Assert.IsFalse(DropGuaranteePolicy.ShouldGuaranteeAttackItem(1, 2, 2));
            Assert.IsFalse(DropGuaranteePolicy.ShouldGuaranteeAttackItem(2, 2, 0));
        }
    }
}
