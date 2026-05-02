using BlackJackFFLite.Domain.Characters;

namespace BlackJackFFLite.Gameplay.Combat
{
    public sealed class RoundResolver
    {
        private readonly DamageCalculator _damageCalculator;

        public RoundResolver(DamageCalculator damageCalculator)
        {
            _damageCalculator = damageCalculator;
        }

        public RoundResult Resolve(Character player, Character enemy)
        {
            bool playerBust = player.Hand.IsBust;
            bool enemyBust = enemy.Hand.IsBust;

            if (playerBust && enemyBust)
            {
                return RoundResult.Draw("Both bust");
            }

            if (playerBust)
            {
                return EnemyWins(player, enemy, "Player bust");
            }

            if (enemyBust)
            {
                return PlayerWins(player, enemy, "Enemy bust");
            }

            int playerScore = player.Hand.Score;
            int enemyScore = enemy.Hand.Score;

            if (playerScore > enemyScore)
            {
                return PlayerWins(player, enemy, "Player has higher score");
            }

            if (enemyScore > playerScore)
            {
                return EnemyWins(player, enemy, "Enemy has higher score");
            }

            return RoundResult.Draw("Equal score");
        }

        private RoundResult PlayerWins(Character player, Character enemy, string reason)
        {
            int damage = _damageCalculator.Calculate(player);
            enemy.TakeDamage(damage);

            return RoundResult.PlayerWin(damage, reason);
        }

        private RoundResult EnemyWins(Character player, Character enemy, string reason)
        {
            int damage = _damageCalculator.Calculate(enemy);
            player.TakeDamage(damage);

            return RoundResult.EnemyWin(damage, reason);
        }
    }
}