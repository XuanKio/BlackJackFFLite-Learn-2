namespace BlackJackFFLite.Domain.Statuses
{
    public sealed class LuckStatus : IStatus
    {
        public string Id => "Luck";
        public int Duration { get; private set; }

        public LuckStatus(int duration)
        {
            Duration = duration;
        }

        public void Tick()
        {
            Duration--;
        }
    }
}