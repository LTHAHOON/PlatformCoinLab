namespace CoinGame.Gameplay.Items
{
    public static class DropGuaranteePolicy
    {
        public static bool ShouldGuaranteeAttackItem(int round, int collectorCount, int grantedCount)
        {
            if (round != 1)
            {
                return false;
            }

            return collectorCount > grantedCount;
        }
    }
}
