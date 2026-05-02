using System.Collections.Generic;
using System.Linq;
using BlackJackFFLite.Domain.Cards;
using BlackJackFFLite.Domain.Characters;
using BlackJackFFLite.Domain.Statuses;
using BlackJackFFLite.Gameplay.Combat;

namespace BlackJackFFLite.Gameplay.Effects
{
    public sealed class SuitEffectResolver
    {
        private readonly Dictionary<CardSuit, ISuitEffect> _effects;

        public SuitEffectResolver(IEnumerable<ISuitEffect> effects)
        {
            _effects = effects.ToDictionary(effect => effect.Suit);
        }

        public void ApplyEffects(Character character, RoundContext context)
        {
            if (character.Statuses.Has<SilentStatus>())
                return;

            var suitGroups = character.Hand.Cards.GroupBy(card => card.Suit);

            foreach (var group in suitGroups)
            {
                CardSuit suit = group.Key;
                int stackCount = group.Count();

                if (_effects.TryGetValue(suit, out ISuitEffect effect))
                {
                    effect.Apply(character, context, stackCount);
                }
            }
        }
    }
}