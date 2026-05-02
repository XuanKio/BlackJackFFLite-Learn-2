namespace BlackJackFFLite.Domain.Statuses
{
    public sealed class CriticalStatus : IStatus
    {
        public string Id => "Critical";
        public int Duration { get; private set; }

        public int BonusDamage { get; }

        public CriticalStatus(int duration, int bonusDamage)
        {
            Duration = duration;
            BonusDamage = bonusDamage;
        }

        public void Tick()
        {
            Duration--;
        }
    }
}