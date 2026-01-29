using FlashcardsAI.Models;
using FlashcardsAI.Services.TextExtraction;
using Microsoft.AspNetCore.Components.Forms;

namespace FlashcardsAI.Services.Ai;

public class FakeAiFlashcardGenerator : IAiFlashcardGenerator
{
    private readonly ITextExtractor _textExtractor;

    public FakeAiFlashcardGenerator(ITextExtractor textExtractor)
    {
        _textExtractor = textExtractor;
    }

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

    public async Task<List<Flashcard>> GenerateFromFileAsync(
        IBrowserFile file,
        GenerateOptions options,
        CancellationToken ct = default)
    {
        var result = await _textExtractor.ExtractAsync(file, ct);
        return await GenerateAsync(result.Text, options, ct);
    }
}
