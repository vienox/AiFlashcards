using Microsoft.AspNetCore.Components.Forms;

namespace FlashcardsAI.Services.TextExtraction;

public interface ITextExtractor
{
    Task<TextExtractionResult> ExtractAsync(IBrowserFile file, CancellationToken ct = default);
}
