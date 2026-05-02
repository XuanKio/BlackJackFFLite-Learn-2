using BlackJackFFLite.Domain.Characters;

namespace  BlackJackFFLite.Gameplay.Combat
{
    public sealed class DamageCalculator
    {
        public int Calculate(Character attacker)
        {
            return attacker.BaseDamage;
        }
    }
}
