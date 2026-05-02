using BlackJackFFLite.Domain.Cards;
using BlackJackFFLite.Domain.Characters;
using BlackJackFFLite.Gameplay.Combat;

namespace BlackJackFFLite.Gameplay.Effects
{
    public sealed class DiamondEffect : ISuitEffect
    {
        public CardSuit Suit => CardSuit.Diamond;

        public void Apply(Character owner, RoundContext context, int stackCount)
        {
            owner.AddGold(stackCount);
        }
    }
}