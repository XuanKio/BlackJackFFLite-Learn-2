using BlackJackFFLite.Domain.Cards;
using BlackJackFFLite.Domain.Characters;
using BlackJackFFLite.Gameplay.Combat;

namespace BlackJackFFLite.Gameplay.Effects
{
    public sealed class SpadeEffect : ISuitEffect
    {
        public CardSuit Suit => CardSuit.Spade;

        public void Apply(Character owner, RoundContext context, int stackCount)
        {
            owner.AddBlock(stackCount);
        }
    }
}