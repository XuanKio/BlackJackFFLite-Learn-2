using System.Collections.Generic;
using BlackJackFFLite.Domain.Cards;

namespace BlackJackFFLite.Domain.Hands
{
    public sealed class Hand
    {
        private readonly List<Card> _cards = new List<Card>();
        public IReadOnlyList<Card> Cards => _cards;
        public int Score => CalculateScore();

        public bool IsBust => Score > 21;

        public int CardCount => _cards.Count;

        public void Add(Card card)
        {
            _cards.Add(card);
        }

        public void Clear()
        {
            _cards.Clear();
        }

        private int CalculateScore()
        {
            int total = 0;
            int aceCount = 0;

            foreach (Card card in _cards)
            {
                if (CardRank.Ace == card.Rank)
                {
                    aceCount++;
                    total += 11; // Initially treat Ace as 11
                }
                else
                {
                    total += card.Value;
                }
            }

            while (total > 21 && aceCount > 0)
            {
                total -= 10;
                aceCount--;
            }

            return total;
        }
    }
}