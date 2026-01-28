namespace FlashcardsAI.Models;

public sealed class Deck
{
    public string Title { get; set; } = string.Empty;
    public List<Flashcard> Cards { get; set; } = new();
}
