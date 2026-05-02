using System;
using System.Collections.Generic;

namespace BlackJackFFLite.Domain.Cards
{
    public sealed class StandardDeck : IDeck
    {
        private readonly List<Card> _cards = new List<Card>();
        private readonly Random _random;

        public int CardsRemaining => _cards.Count;

        public StandardDeck()
        {
            _random = new Random();
            Reset();
        }

        public StandardDeck(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            Reset();
        }

        public Card Draw()
        {
            if (_cards.Count == 0)
                throw new InvalidOperationException("Cannot draw from an empty deck.");

            Card card = _cards[0];
            _cards.RemoveAt(0);

            return card;
        }

        public bool TryDraw(Predicate<Card> predicate, out Card card)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            for (int i = 0; i < _cards.Count; i++)
            {
                if (!predicate(_cards[i]))
                    continue;

                card = _cards[i];
                _cards.RemoveAt(i);
                return true;
            }

            card = default;
            return false;
        }

        public void Shuffle()
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int randomIndex = _random.Next(i + 1);

                Card temp = _cards[i];
                _cards[i] = _cards[randomIndex];
                _cards[randomIndex] = temp;
            }
        }

        public void Reset()
        {
            _cards.Clear();

            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardRank rank in Enum.GetValues(typeof(CardRank)))
                {
                    _cards.Add(new Card(suit, rank));
                }
            }

            Shuffle();
        }
    }
}
