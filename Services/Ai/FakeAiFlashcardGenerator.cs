using FlashcardsAI.Models;

namespace FlashcardsAI.Services.Ai;

public class FakeAiFlashcardGenerator : IAiFlashcardGenerator
{
    public Task<List<Flashcard>> GenerateAsync(
        string sourceText,
        GenerateOptions options,
        CancellationToken ct = default)
    {
        sourceText ??= string.Empty;

        var parts = sourceText
            .Split(new[] { '.', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToList();

        var cards = new List<Flashcard>();

        foreach (var p in parts.Take(options.Count))
        {
            cards.Add(new Flashcard
            {
                Front = $"Explain: {p}",
                Back = p
            });
        }

        if (cards.Count == 0)
        {
            cards.Add(new Flashcard
            {
                Front = "No input text",
                Back = "Paste some notes to generate flashcards."
            });
        }

        return Task.FromResult(cards);
    }
}
