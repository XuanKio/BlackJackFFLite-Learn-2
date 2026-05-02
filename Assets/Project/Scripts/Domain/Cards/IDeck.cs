using System;

namespace BlackJackFFLite.Domain.Cards
{
    public interface IDeck
    {
        int CardsRemaining { get; }

        Card Draw();

        bool TryDraw(Predicate<Card> predicate, out Card card);

        void Shuffle();

        void Reset();
    }
}
