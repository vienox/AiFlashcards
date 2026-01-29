using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlashcardsAI.Models;
using FlashcardsAI.Services.TextExtraction;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;

namespace FlashcardsAI.Services.Ai;

public sealed class OpenAiFlashcardGenerator : IAiFlashcardGenerator
{
    private const long MaxUploadBytes = 50 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly ITextExtractor _textExtractor;
    private readonly string _model;

    public OpenAiFlashcardGenerator(
        HttpClient http,
        IConfiguration configuration,
        ITextExtractor textExtractor)
    {
        _http = http;
        _textExtractor = textExtractor;
        _model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";

        var apiKey = configuration["OpenAI:ApiKey"]
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Missing OpenAI API key. Set OpenAI:ApiKey or OPENAI_API_KEY.");
        }

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<List<Flashcard>> GenerateAsync(
        string sourceText,
        GenerateOptions options,
        CancellationToken ct = default)
    {
        var prompt = BuildPrompt(options.Count);
        var payload = BuildTextPayload(prompt, sourceText);
        var responseJson = await PostJsonAsync("responses", payload, ct);
        return ParseFlashcards(responseJson);
    }

    public async Task<List<Flashcard>> GenerateFromFileAsync(
        IBrowserFile file,
        GenerateOptions options,
        CancellationToken ct = default)
    {
        if (file is null)
        {
            return new List<Flashcard>();
        }

        var extension = Path.GetExtension(file.Name);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            var textResult = await _textExtractor.ExtractAsync(file, ct);
            return await GenerateAsync(textResult.Text, options, ct);
        }

        var fileId = await UploadFileAsync(file, ct);
        var prompt = BuildPrompt(options.Count);
        var payload = BuildFilePayload(prompt, fileId);
        var responseJson = await PostJsonAsync("responses", payload, ct);
        return ParseFlashcards(responseJson);
    }

    private static string BuildPrompt(int count)
    {
        return $"Create {count} concise flashcards from the provided content. " +
               "Focus on key concepts and definitions. " +
               "Return only valid JSON matching the schema.";
    }

    private object BuildTextPayload(string prompt, string sourceText)
    {
        return new Dictionary<string, object?>
        {
            ["model"] = _model,
            ["input"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["role"] = "user",
                    ["content"] = new object[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["type"] = "input_text",
                            ["text"] = $"{prompt}\n\nSOURCE:\n{sourceText}"
                        }
                    }
                }
            },
            ["text"] = BuildTextFormat()
        };
    }

    private object BuildFilePayload(string prompt, string fileId)
    {
        return new Dictionary<string, object?>
        {
            ["model"] = _model,
            ["input"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["role"] = "user",
                    ["content"] = new object[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["type"] = "input_file",
                            ["file_id"] = fileId
                        },
                        new Dictionary<string, object?>
                        {
                            ["type"] = "input_text",
                            ["text"] = prompt
                        }
                    }
                }
            },
            ["text"] = BuildTextFormat()
        };
    }

    private static object BuildTextFormat()
    {
        return new Dictionary<string, object?>
        {
            ["format"] = new Dictionary<string, object?>
            {
                ["type"] = "json_schema",
                ["name"] = "flashcards",
                ["strict"] = true,
                ["schema"] = BuildSchema()
            }
        };
    }

    private static object BuildSchema()
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = new Dictionary<string, object?>
            {
                ["cards"] = new Dictionary<string, object?>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object?>
                    {
                        ["type"] = "object",
                        ["additionalProperties"] = false,
                        ["properties"] = new Dictionary<string, object?>
                        {
                            ["front"] = new Dictionary<string, object?>
                            {
                                ["type"] = "string"
                            },
                            ["back"] = new Dictionary<string, object?>
                            {
                                ["type"] = "string"
                            }
                        },
                        ["required"] = new[] { "front", "back" }
                    }
                }
            },
            ["required"] = new[] { "cards" }
        };
    }

    private async Task<string> UploadFileAsync(IBrowserFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream(MaxUploadBytes, ct);
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(file.ContentType ?? "application/pdf");

        content.Add(fileContent, "file", file.Name);
        content.Add(new StringContent("user_data"), "purpose");

        using var response = await _http.PostAsync("files", content, ct);
        var responseJson = await ReadResponseAsync(response, ct);

        using var doc = JsonDocument.Parse(responseJson);
        if (!doc.RootElement.TryGetProperty("id", out var idElement))
        {
            throw new InvalidOperationException("OpenAI file upload did not return an id.");
        }

        var fileId = idElement.GetString();
        if (string.IsNullOrWhiteSpace(fileId))
        {
            throw new InvalidOperationException("OpenAI file upload returned an empty id.");
        }

        return fileId;
    }

    private async Task<string> PostJsonAsync(
        string path,
        object payload,
        CancellationToken ct)
    {
        using var content = JsonContent.Create(payload);
        using var response = await _http.PostAsync(path, content, ct);
        return await ReadResponseAsync(response, ct);
    }

    private static async Task<string> ReadResponseAsync(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI API error: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }

        return body;
    }

    private static List<Flashcard> ParseFlashcards(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var outputText = ExtractOutputText(doc.RootElement);
        if (string.IsNullOrWhiteSpace(outputText))
        {
            throw new InvalidOperationException("OpenAI response did not include any output text.");
        }

        var envelope = JsonSerializer.Deserialize<FlashcardEnvelope>(outputText, JsonOptions);
        return envelope?.Cards ?? new List<Flashcard>();
    }

    private static string ExtractOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputTextElement) &&
            outputTextElement.ValueKind == JsonValueKind.String)
        {
            return outputTextElement.GetString() ?? string.Empty;
        }

        if (!root.TryGetProperty("output", out var outputElement) ||
            outputElement.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var item in outputElement.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var contentElement) ||
                contentElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in contentElement.EnumerateArray())
            {
                if (!part.TryGetProperty("type", out var typeElement) ||
                    typeElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var type = typeElement.GetString();
                if (type is not ("output_text" or "text"))
                {
                    continue;
                }

                if (part.TryGetProperty("text", out var textElement) &&
                    textElement.ValueKind == JsonValueKind.String)
                {
                    builder.Append(textElement.GetString());
                }
            }
        }

        return builder.ToString();
    }

    private sealed class FlashcardEnvelope
    {
        [JsonPropertyName("cards")]
        public List<Flashcard> Cards { get; init; } = new();
    }
}
