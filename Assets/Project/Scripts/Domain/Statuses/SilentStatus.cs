namespace BlackJackFFLite.Domain.Statuses
{
    public sealed class SilentStatus : IStatus
    {
        public string Id => "Silent";
        public int Duration { get; private set; }

        public SilentStatus(int duration)
        {
            Duration = duration;
        }

        public void Tick()
        {
            Duration--;
        }
    }
}