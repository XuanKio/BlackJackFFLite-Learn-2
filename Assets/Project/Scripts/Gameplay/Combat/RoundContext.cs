using System.Collections.Generic;
using BlackJackFFLite.Domain.Characters;

namespace BlackJackFFLite.Gameplay.Combat
{
    public sealed class RoundContext
    {
        private readonly Dictionary<Character, int> _bonusDamageByCharacter = new Dictionary<Character, int>();

        public Character Player { get; }
        public Character Enemy { get; }

        public RoundContext(Character player, Character enemy)
        {
            Player = player;
            Enemy = enemy;
        }

        public void AddBonusDamage(Character character, int amount)
        {
            if (amount <= 0)
                return;

            if (_bonusDamageByCharacter.ContainsKey(character))
            {
                _bonusDamageByCharacter[character] += amount;
                return;
            }

            _bonusDamageByCharacter.Add(character, amount);
        }

        public int GetBonusDamage(Character character)
        {
            return _bonusDamageByCharacter.TryGetValue(character, out int bonusDamage)
                ? bonusDamage
                : 0;
        }
    }
}
