using BlackJackFFLite.Domain.Cards;
using BlackJackFFLite.Domain.Characters;
using BlackJackFFLite.Gameplay.Combat;

namespace BlackJackFFLite.Gameplay.Effects
{
    public sealed class HeartEffect : ISuitEffect
    {
        public CardSuit Suit => CardSuit.Heart;

        public void Apply(Character owner, RoundContext context, int stackCount)
        {
            owner.Heal(stackCount);
        }
    }
}