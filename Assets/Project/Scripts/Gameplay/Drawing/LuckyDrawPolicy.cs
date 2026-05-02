using BlackJackFFLite.Domain.Cards;
using BlackJackFFLite.Domain.Characters;

namespace BlackJackFFLite.Gameplay.Drawing
{
    public sealed class LuckyDrawPolicy : ICardDrawPolicy
    {
        public Card Draw(Character character, IDeck deck)
        {
            if (deck.TryDraw(
                    card => !character.Hand.WouldBust(card) && character.Hand.GetScoreWith(card) >= 18,
                    out Card luckyCard))
            {
                return luckyCard;
            }

            if (deck.TryDraw(card => !character.Hand.WouldBust(card), out Card safeCard))
            {
                return safeCard;
            }

            return deck.Draw();
        }
    }
}
