using BlackJackFFLite.Domain.Cards;
using BlackJackFFLite.Domain.Characters;

namespace BlackJackFFLite.Gameplay.Drawing
{
    public interface ICardDrawPolicy
    {
        Card Draw(Character character, IDeck deck);
    }
}