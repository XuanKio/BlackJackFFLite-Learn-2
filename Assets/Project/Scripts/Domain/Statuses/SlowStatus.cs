namespace BlackJackFFLite.Domain.Statuses
{
    public sealed class SlowStatus : IStatus
    {
        public string Id => "Slow";
        public int Duration { get; private set; }

        public SlowStatus(int duration)
        {
            Duration = duration;
        }

        public void Tick()
        {
            Duration--;
        }
    }
}