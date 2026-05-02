using System;
using BlackJackFFLite.Domain.Cards;
using BlackJackFFLite.Domain.Characters;
using BlackJackFFLite.Domain.Statuses;

namespace BlackJackFFLite.Gameplay.Rules
{
    public sealed class AfterRoundStatusResolver
    {
        private readonly StrongestSuitResolver _strongestSuitResolver;

        public AfterRoundStatusResolver(StrongestSuitResolver strongestSuitResolver)
        {
            _strongestSuitResolver = strongestSuitResolver;
        }

        public void ApplyFromLoserHand(Character loser, Character opponent)
        {
            if (loser.Hand.CardCount == 0)
                return;

            CardSuit strongestSuit = _strongestSuitResolver.Resolve(loser.Hand);
            IStatus status = CreateStatus(strongestSuit);
            Character target = ShouldApplyToLoser(strongestSuit) ? loser : opponent;

            target.Statuses.Add(status);
        }

        public IStatus CreateStatus(CardSuit strongestSuit)
        {
            switch (strongestSuit)
            {
                case CardSuit.Spade:
                    return new SilentStatus(duration: 1);

                case CardSuit.Heart:
                    return new SlowStatus(duration: 1);

                case CardSuit.Diamond:
                    return new CriticalStatus(duration: 1, bonusDamage: 3);

                case CardSuit.Club:
                    return new LuckStatus(duration: 1);

                default:
                    throw new ArgumentOutOfRangeException(nameof(strongestSuit), strongestSuit, null);
            }
        }

        public bool ShouldApplyToLoser(CardSuit strongestSuit)
        {
            return strongestSuit == CardSuit.Diamond || strongestSuit == CardSuit.Club;
        }
    }
}
