using BlackJackFFLite.Domain.Characters;

namespace BlackJackFFLite.Gameplay.AI
{
    public sealed class BasicEnemyAI : IEnemyDecisionStrategy
    {
        private readonly int _standThreshold;

        public BasicEnemyAI(int standThreshold = 17)
        {
            _standThreshold = standThreshold;
        }

        public EnemyAction Decide(Character enemy, Character player)
        {
            if (enemy.Hand.IsBust)
                return EnemyAction.Stand;

            if (enemy.Hand.Score < _standThreshold)
                return EnemyAction.Hit;

            return EnemyAction.Stand;
        }
    }
}