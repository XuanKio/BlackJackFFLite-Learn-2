using BlackJackFFLite.Domain.Cards;
using BlackJackFFLite.Domain.Characters;

namespace BlackJackFFLite.Gameplay.Drawing
{
    public sealed class NormalDrawPolicy : ICardDrawPolicy
    {
        public Card Draw(Character character, IDeck deck)
        {
            return deck.Draw();
        }
    }
}