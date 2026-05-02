using BlackJackFFLite.Domain.Cards;
using BlackJackFFLite.Domain.Characters;
using BlackJackFFLite.Gameplay.Combat;

namespace BlackJackFFLite.Core.Events
{
    public readonly struct CardDrawnEvent
    {
        public Character Owner { get; }
        public Card Card { get; }

        public CardDrawnEvent(Character owner, Card card)
        {
            Owner = owner;
            Card = card;
        }
    }

    public readonly struct RoundResolvedEvent
    {
        public RoundResult Result { get; }

        public RoundResolvedEvent(RoundResult result)
        {
            Result = result;
        }
    }

    public readonly struct CharacterChangedEvent
    {
        public Character Character { get; }

        public CharacterChangedEvent(Character character)
        {
            Character = character;
        }
    }

    public readonly struct GameOverEvent
    {
        public Character Winner { get; }

        public GameOverEvent(Character winner)
        {
            Winner = winner;
        }
    }
}