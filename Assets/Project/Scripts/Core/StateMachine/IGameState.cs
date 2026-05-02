namespace BlackJackFFLite.Core.StateMachine
{
    public interface IGameState
    {
        void Enter();
        void Exit();
    }
}