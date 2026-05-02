using System;
using System.Linq;
using BlackJackFFLite.Domain.Cards;
using BlackJackFFLite.Domain.Hands;

namespace BlackJackFFLite.Gameplay.Rules
{
    public sealed class StrongestSuitResolver
    {
        public CardSuit Resolve(Hand hand)
        {
            if (hand.CardCount == 0)
                throw new InvalidOperationException("Cannot resolve strongest suit from an empty hand.");

            return hand.Cards
                .GroupBy(card => card.Suit)
                .OrderByDescending(group => group.Sum(card => card.Value))
                .First()
                .Key;
        }
    }
}