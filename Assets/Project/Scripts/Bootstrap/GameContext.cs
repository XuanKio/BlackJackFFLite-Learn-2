using BlackJackFFLite.Core.Events;
using BlackJackFFLite.Core.StateMachine;
using BlackJackFFLite.Domain.Cards;
using BlackJackFFLite.Domain.Characters;
using BlackJackFFLite.Gameplay.AI;
using BlackJackFFLite.Gameplay.Combat;

namespace BlackJackFFLite.Bootstrap
{
    public sealed class GameContext
    {
        public EventBus EventBus { get; }
        public GameStateMachine StateMachine { get; }

        public Character Player { get; }
        public Character Enemy { get; }

        public IDeck Deck { get; }

        public RoundResolver RoundResolver { get; }
        public IEnemyDecisionStrategy EnemyAI { get; }

        public GameContext(
            EventBus eventBus,
            GameStateMachine stateMachine,
            Character player,
            Character enemy,
            IDeck deck,
            RoundResolver roundResolver,
            IEnemyDecisionStrategy enemyAI)
        {
            EventBus = eventBus;
            StateMachine = stateMachine;

            Player = player;
            Enemy = enemy;

            Deck = deck;

            RoundResolver = roundResolver;
            EnemyAI = enemyAI;
        }

        public bool IsGameOver()
        {
            return Player.IsDead || Enemy.IsDead;
        }

        public Character GetWinner()
        {
            if (Player.IsDead)
                return Enemy;

            if (Enemy.IsDead)
                return Player;

            return null;
        }

        public void ResetForNewGame()
        {
            Player.ResetForNewGame();
            Enemy.ResetForNewGame();
            Deck.Reset();
        }

        public void ResetForNewRound()
        {
            Player.ResetForNewRound();
            Enemy.ResetForNewRound();
        }
    }
}