using System.Collections.Generic;
using BlackJackFFLite.Domain.Cards;

namespace BlackJackFFLite.Domain.Hands
{
    public sealed class Hand
    {
        private const int BustLimit = 21;

        private readonly List<Card> _cards = new List<Card>();

        public IReadOnlyList<Card> Cards => _cards;

        public int Score => CalculateScore(_cards);

        public bool IsBust => Score > BustLimit;

        public int CardCount => _cards.Count;

        public void Add(Card card)
        {
            AddCard(card);
        }

        public void AddCard(Card card)
        {
            _cards.Add(card);
        }

        public void Clear()
        {
            _cards.Clear();
        }

        public int GetScoreWith(Card card)
        {
            List<Card> cards = new List<Card>(_cards) { card };
            return CalculateScore(cards);
        }

        public bool WouldBust(Card card)
        {
            return GetScoreWith(card) > BustLimit;
        }

        public int CountSuit(CardSuit suit)
        {
            int count = 0;

            foreach (Card card in _cards)
            {
                if (card.Suit == suit)
                    count++;
            }

            return count;
        }

        public int GetSuitScore(CardSuit suit)
        {
            int score = 0;

            foreach (Card card in _cards)
            {
                if (card.Suit == suit)
                    score += card.Value;
            }

            return score;
        }

        private static int CalculateScore(IEnumerable<Card> cards)
        {
            int total = 0;
            int aceCount = 0;

            foreach (Card card in cards)
            {
                if (CardRank.Ace == card.Rank)
                {
                    aceCount++;
                    total += 11;
                }
                else
                {
                    total += card.Value;
                }
            }

            while (total > BustLimit && aceCount > 0)
            {
                total -= 10;
                aceCount--;
            }

            return total;
        }
    }
}
