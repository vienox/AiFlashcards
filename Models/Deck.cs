namespace FlashcardsAI.Models;

public class Deck
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AccountId { get; set; }

    public Account? Account { get; set; }

    public string Title { get; set; } = "New deck";

    public string? SourceName { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<Flashcard> Cards { get; set; } = new();
}
