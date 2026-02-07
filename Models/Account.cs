namespace FlashcardsAI.Models;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<Deck> Decks { get; set; } = new();
}
