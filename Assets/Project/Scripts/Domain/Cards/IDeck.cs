namespace BlackJackFFLite.Domain.Cards
{
    public interface IDeck
    {
        int CardsRemaining { get; }

        Card Draw();

        void Shuffle();

        void Reset();
    }
}