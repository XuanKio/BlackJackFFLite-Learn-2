using System;

namespace BlackJackFFLite.Core.StateMachine
{
    public sealed class GameStateMachine
    {
        private IGameState _currentState;

        public IGameState CurrentState => _currentState;

        public void ChangeState(IGameState nextState)
        {
            if (nextState == null)
                throw new ArgumentNullException(nameof(nextState));

            _currentState?.Exit();

            _currentState = nextState;

            _currentState.Enter();
        }
    }
}