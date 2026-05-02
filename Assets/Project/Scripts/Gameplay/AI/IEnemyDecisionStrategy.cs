using BlackJackFFLite.Domain.Characters;

namespace  BlackJackFFLite.Gameplay.AI
{
    public interface IEnemyDecisionStrategy
    {
        EnemyAction Decide(Character enemy, Character player);
    }
}

