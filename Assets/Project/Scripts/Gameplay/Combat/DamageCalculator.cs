using BlackJackFFLite.Domain.Characters;
using BlackJackFFLite.Domain.Statuses;

namespace BlackJackFFLite.Gameplay.Combat
{
    public sealed class DamageCalculator
    {
        public int Calculate(Character attacker, RoundContext context)
        {
            int damage = attacker.BaseDamage + context.GetBonusDamage(attacker);
            CriticalStatus criticalStatus = attacker.Statuses.Get<CriticalStatus>();

            if (criticalStatus != null)
            {
                damage += criticalStatus.BonusDamage;
            }

            return damage;
        }
    }
}
