using System.Text;
using Microsoft.AspNetCore.Components.Forms;
using UglyToad.PdfPig;

namespace FlashcardsAI.Services.TextExtraction;

public sealed class FileTextExtractor : ITextExtractor
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;

    public async Task<TextExtractionResult> ExtractAsync(IBrowserFile file, CancellationToken ct = default)
    {
        if (file is null)
        {
            return new TextExtractionResult(string.Empty, "Nie wybrano pliku.");
        }

        var extension = Path.GetExtension(file.Name).ToLowerInvariant();

        try
        {
            return extension switch
            {
                ".txt" or ".md" or ".csv" => new TextExtractionResult(await ReadTextAsync(file, ct), null),
                ".pdf" => new TextExtractionResult(await ReadPdfAsync(file, ct), null),
                _ => new TextExtractionResult(string.Empty, "Obslugiwane pliki: PDF, TXT, MD, CSV.")
            };
        }
        catch (IOException ex)
        {
            return new TextExtractionResult(string.Empty, $"Nie udalo sie wczytac pliku: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new TextExtractionResult(string.Empty, $"Blad podczas przetwarzania: {ex.Message}");
        }
    }

    private static async Task<string> ReadTextAsync(IBrowserFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream(MaxUploadBytes, ct);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(ct);
    }

    private static async Task<string> ReadPdfAsync(IBrowserFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream(MaxUploadBytes, ct);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        var builder = new StringBuilder();
        using var document = PdfDocument.Open(buffer);
        foreach (var page in document.GetPages())
        {
            builder.AppendLine(page.Text);
        }

        return builder.ToString();
    }
}
