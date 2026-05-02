using BlackJackFFLite.Domain.Hands;

namespace BlackJackFFLite.Domain.Characters
{
    public sealed class Character
    {
        public string Id { get; }

        public int MaxHp { get; }
        public int Hp { get; private set; }

        public int Block { get; private set; }
        public int Gold { get; private set; }
        public int BaseDamage { get; }

        public Hand Hand { get; }

        public bool IsDead => Hp <= 0;

        public Character(string id, int maxHp, int baseDamage)
        {
            Id = id;
            MaxHp = maxHp;
            Hp = maxHp;
            BaseDamage = baseDamage;
            Hand = new Hand();
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0)
                return;

            int remainingDamage = damage;

            if (Block > 0)
            {
                int blockedDamage = remainingDamage > Block ? Block : remainingDamage;

                Block -= blockedDamage;
                remainingDamage -= blockedDamage;
            }

            Hp -= remainingDamage;

            if (Hp < 0)
                Hp = 0;
        }

        public void Heal(int amount)
        {
            if (amount <= 0)
                return;

            Hp += amount;

            if (Hp > MaxHp)
                Hp = MaxHp;
        }

        public void AddBlock(int amount)
        {
            if (amount <= 0)
                return;

            Block += amount;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;

            Gold += amount;
        }

        public void ClearBlock()
        {
            Block = 0;
        }

        public void ResetForNewGame()
        {
            Hp = MaxHp;
            Block = 0;
            Gold = 0;
            Hand.Clear();
        }

        public void ResetForNewRound()
        {
            Block = 0;
            Hand.Clear();
        }
    }
}