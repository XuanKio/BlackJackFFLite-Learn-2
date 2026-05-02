using BlackJackFFLite.Domain.Cards;
using BlackJackFFLite.Domain.Characters;
using BlackJackFFLite.Domain.Statuses;

namespace BlackJackFFLite.Gameplay.Drawing
{
    public sealed class CardDrawService
    {
        private readonly ICardDrawPolicy _normalDrawPolicy;
        private readonly ICardDrawPolicy _luckyDrawPolicy;

        public CardDrawService(
            ICardDrawPolicy normalDrawPolicy,
            ICardDrawPolicy luckyDrawPolicy)
        {
            _normalDrawPolicy = normalDrawPolicy;
            _luckyDrawPolicy = luckyDrawPolicy;
        }

        public Card DrawFor(Character character, IDeck deck)
        {
            ICardDrawPolicy drawPolicy = character.Statuses.Has<LuckStatus>()
                ? _luckyDrawPolicy
                : _normalDrawPolicy;

            Card card = drawPolicy.Draw(character, deck);
            character.Hand.Add(card);

            return card;
        }
    }
}