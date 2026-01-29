using FlashcardsAI.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace FlashcardsAI.Services.Ai;

public interface IAiFlashcardGenerator
{
    Task<List<Flashcard>> GenerateAsync(
        string sourceText,
        GenerateOptions options,
        CancellationToken ct = default);

    Task<List<Flashcard>> GenerateFromFileAsync(
        IBrowserFile file,
        GenerateOptions options,
        CancellationToken ct = default);
}
