using BlackJackFFLite.Domain.Characters;
using BlackJackFFLite.Gameplay.Effects;
using BlackJackFFLite.Gameplay.Rules;

namespace BlackJackFFLite.Gameplay.Combat
{
    public sealed class RoundResolver
    {
        private readonly DamageCalculator _damageCalculator;
        private readonly SuitEffectResolver _suitEffectResolver;
        private readonly AfterRoundStatusResolver _afterRoundStatusResolver;

        public RoundResolver(
            DamageCalculator damageCalculator,
            SuitEffectResolver suitEffectResolver,
            AfterRoundStatusResolver afterRoundStatusResolver)
        {
            _damageCalculator = damageCalculator;
            _suitEffectResolver = suitEffectResolver;
            _afterRoundStatusResolver = afterRoundStatusResolver;
        }

        public RoundResult Resolve(Character player, Character enemy)
        {
            RoundContext context = new RoundContext(player, enemy);
            _suitEffectResolver.ApplyEffects(player, context);
            _suitEffectResolver.ApplyEffects(enemy, context);

            bool playerBust = player.Hand.IsBust;
            bool enemyBust = enemy.Hand.IsBust;

            if (playerBust && enemyBust)
            {
                return FinishRound(player, enemy, RoundResult.Draw("Both bust"));
            }

            if (playerBust)
            {
                return EnemyWins(player, enemy, context, "Player bust");
            }

            if (enemyBust)
            {
                return PlayerWins(player, enemy, context, "Enemy bust");
            }

            int playerScore = player.Hand.Score;
            int enemyScore = enemy.Hand.Score;

            if (playerScore > enemyScore)
            {
                return PlayerWins(player, enemy, context, "Player has higher score");
            }

            if (enemyScore > playerScore)
            {
                return EnemyWins(player, enemy, context, "Enemy has higher score");
            }

            return FinishRound(player, enemy, RoundResult.Draw("Equal score"));
        }

        private RoundResult PlayerWins(Character player, Character enemy, RoundContext context, string reason)
        {
            int damage = _damageCalculator.Calculate(player, context);
            enemy.TakeDamage(damage);

            return FinishRound(player, enemy, RoundResult.PlayerWin(damage, reason));
        }

        private RoundResult EnemyWins(Character player, Character enemy, RoundContext context, string reason)
        {
            int damage = _damageCalculator.Calculate(enemy, context);
            player.TakeDamage(damage);

            return FinishRound(player, enemy, RoundResult.EnemyWin(damage, reason));
        }

        private RoundResult FinishRound(Character player, Character enemy, RoundResult result)
        {
            player.Statuses.TickAll();
            enemy.Statuses.TickAll();

            if (result.Winner == RoundWinner.Player)
            {
                _afterRoundStatusResolver.ApplyFromLoserHand(enemy, player);
            }
            else if (result.Winner == RoundWinner.Enemy)
            {
                _afterRoundStatusResolver.ApplyFromLoserHand(player, enemy);
            }

            return result;
        }
    }
}
