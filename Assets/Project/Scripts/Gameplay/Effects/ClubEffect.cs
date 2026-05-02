using BlackJackFFLite.Domain.Cards;
using BlackJackFFLite.Domain.Characters;
using BlackJackFFLite.Gameplay.Combat;

namespace BlackJackFFLite.Gameplay.Effects
{
    public sealed class ClubEffect : ISuitEffect
    {
        public CardSuit Suit => CardSuit.Club;

        public void Apply(Character owner, RoundContext context, int stackCount)
        {
            context.AddBonusDamage(owner, stackCount);
        }
    }
}