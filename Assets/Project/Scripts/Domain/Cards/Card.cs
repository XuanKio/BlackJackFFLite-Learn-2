using System;

namespace BlackJackFFLite.Domain.Cards
{
    public readonly struct Card
    {
        public CardRank Rank { get; }
        public CardSuit Suit { get; }

        public int Value =>GetBlackJackValue();
        public Card(CardSuit suit, CardRank rank)
        {
            Suit = suit;
            Rank = rank;
        }

        private int GetBlackJackValue()
        {
            switch (Rank)
            {
                case CardRank.Ace:
                    return 11;
                case CardRank.Two:
                    return 2;
                case CardRank.Three:
                    return 3;
                case CardRank.Four:
                    return 4;
                case CardRank.Five:
                    return 5;
                case CardRank.Six:
                    return 6;
                case CardRank.Seven:
                    return 7;
                case CardRank.Eight:
                    return 8;
                case CardRank.Nine:
                    return 9;
                case CardRank.Ten:
                case CardRank.Jack:
                case CardRank.Queen:
                case CardRank.King:
                    return 10;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        public override string ToString()
        {
            return $"{Rank} of {Suit}";
        }
    }
}