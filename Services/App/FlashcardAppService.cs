using FlashcardsAI.Models;
using FlashcardsAI.Services.Ai;

namespace FlashcardsAI.Services.App;

public class FlashcardAppService
{
    private readonly IAiFlashcardGenerator _ai;

    public FlashcardAppService(IAiFlashcardGenerator ai)
    {
        _ai = ai;
    }

    public Deck? LastDeck { get; private set; }

   
    public async Task<Deck> GenerateFromTextAsync(
        string sourceText,
        GenerateOptions options,
        string? sourceName = null,
        CancellationToken ct = default)
    {
        // STEP 1: Normalize + validate input
        sourceText = (sourceText ?? string.Empty).Trim();

        if (sourceText.Length == 0)
        {
            var empty = CreateEmptyDeck(sourceName);
            LastDeck = empty;
            return empty;
        }

        // STEP 2: Ask AI to create flashcards
        var cards = await _ai.GenerateAsync(sourceText, options, ct);

        // STEP 3: Ensure we have something to show
        cards ??= new List<Flashcard>();

        // Keep only the requested number of cards
        cards = cards.Take(Math.Max(1, options.Count)).ToList();

        if (cards.Count == 0)
        {
            cards.Add(new Flashcard
            {
                Front = "No flashcards generated",
                Back = "Try with more text or change card type."
            });
        }

        // STEP 4: Build final Deck
        var deck = new Deck
        {
            Title = !string.IsNullOrWhiteSpace(sourceName) ? sourceName : "Generated deck",
            SourceName = sourceName,
            Cards = cards
        };

        LastDeck = deck;
        return deck;
    }

    private static Deck CreateEmptyDeck(string? sourceName)
    {
        return new Deck
        {
            Title = !string.IsNullOrWhiteSpace(sourceName) ? sourceName : "Empty",
            SourceName = sourceName,
            Cards = new List<Flashcard>
            {
                new()
                {
                    Front = "No text provided",
                    Back = "Paste your notes or upload a file (TXT/PDF)."
                }
            }
        };
    }
}
