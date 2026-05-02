namespace BlackJackFFLite.Domain.Statuses
{
    public interface IStatus
    {
        string Id { get; }
        int Duration { get; }

        void Tick();
    }
}

