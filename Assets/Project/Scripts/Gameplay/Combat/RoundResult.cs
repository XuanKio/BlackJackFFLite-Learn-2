namespace BlackJackFFLite.Gameplay.Combat
{
    public sealed class RoundResult
    {
        public RoundWinner Winner { get; }
        public int DamageDealt { get; }
        public string Reason { get; }

        public bool IsDraw => Winner == RoundWinner.Draw;

        public RoundResult(RoundWinner winner, int damageDealt, string reason)
        {
            Winner = winner;
            DamageDealt = damageDealt;
            Reason = reason;
        }

        public static RoundResult PlayerWin(int damageDealt, string reason)
        {
            return new RoundResult(RoundWinner.Player, damageDealt, reason);
        }

        public static RoundResult EnemyWin(int damageDealt, string reason)
        {
            return new RoundResult(RoundWinner.Enemy, damageDealt, reason);
        }

        public static RoundResult Draw(string reason)
        {
            return new RoundResult(RoundWinner.Draw, 0, reason);
        }
    }
}