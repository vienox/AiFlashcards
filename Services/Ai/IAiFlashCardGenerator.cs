using FlashcardsAI.Models;

namespace FlashcardsAI.Services.Ai;

public interface IAiFlashcardGenerator
{
    Task<List<Flashcard>> GenerateAsync(
        string sourceText,
        GenerateOptions options,
        CancellationToken ct = default);
}
