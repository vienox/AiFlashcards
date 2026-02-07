namespace FlashcardsAI.Models;

public class Flashcard
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DeckId { get; set; }

    public Deck? Deck { get; set; }

    public string Front { get; set; } = string.Empty; 
    public string Back { get; set; } = string.Empty;  

    public string? Tag { get; set; } 
}
