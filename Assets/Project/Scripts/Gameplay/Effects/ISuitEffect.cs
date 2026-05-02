using BlackJackFFLite.Domain.Cards;
using BlackJackFFLite.Domain.Characters;
using BlackJackFFLite.Gameplay.Combat;

namespace BlackJackFFLite.Gameplay.Effects
{
    public interface ISuitEffect
    {
        CardSuit Suit { get; }

        void Apply(Character owner, RoundContext context, int stackCount);
    }
}