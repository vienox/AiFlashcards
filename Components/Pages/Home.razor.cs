using System.IO;
using FlashcardsAI.Models;
using FlashcardsAI.Services.Ai;
using FlashcardsAI.Services.TextExtraction;
using FlashcardsAI.Services.Training;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FlashcardsAI.Components.Pages;

public partial class Home
{
    private readonly List<Flashcard> _cards = new();

    private string SourceText { get; set; } = string.Empty;
    private IBrowserFile? SelectedFile { get; set; }
    private int CardCount { get; set; } = 8;
    private bool IsBusy { get; set; }
    private string? ErrorMessage { get; set; }
    private string? InfoMessage { get; set; }
    private InputModeKind _inputMode = InputModeKind.Text;

    [Inject] public IAiFlashcardGenerator AiGenerator { get; set; } = default!;
    [Inject] public ITextExtractor TextExtractor { get; set; } = default!;
    [Inject] public TrainingState TrainingState { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    private IReadOnlyList<Flashcard> Cards => _cards;

    private InputModeKind InputMode
    {
        get => _inputMode;
        set
        {
            if (_inputMode == value)
            {
                return;
            }

            _inputMode = value;
            ErrorMessage = null;
            InfoMessage = null;
        }
    }

    private string FileLabel =>
        SelectedFile is null
            ? "No file selected."
            : $"{SelectedFile.Name} ({FormatBytes(SelectedFile.Size)})";

    private async Task OnFileSelected(InputFileChangeEventArgs e)
    {
        SelectedFile = e.File;
        InputMode = InputModeKind.File;
        ErrorMessage = null;
        InfoMessage = null;
        await InvokeAsync(StateHasChanged);
    }

    private async Task GenerateAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        InfoMessage = null;

        try
        {
            var options = new GenerateOptions { Count = CardCount };
            List<Flashcard> result;

            if (InputMode == InputModeKind.File)
            {
                if (SelectedFile is null)
                {
                    ErrorMessage = "No file selected.";
                    return;
                }

                var extension = Path.GetExtension(SelectedFile.Name);
                if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    var text = await ExtractFromFileAsync();
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        if (string.IsNullOrWhiteSpace(ErrorMessage))
                        {
                            ErrorMessage = "Select a text file.";
                        }

                        return;
                    }

                    result = await AiGenerator.GenerateAsync(text, options);
                }
                else
                {
                    result = await AiGenerator.GenerateFromFileAsync(SelectedFile, options);
                }
            }
            else
            {
                var text = SourceText;
                if (string.IsNullOrWhiteSpace(text))
                {
                    ErrorMessage = "Paste some text to generate flashcards.";
                    return;
                }

                result = await AiGenerator.GenerateAsync(text, options);
            }

            _cards.Clear();
            _cards.AddRange(result);
            TrainingState.SetCards(_cards);
            InfoMessage = $"Generated {Cards.Count} flashcards.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to generate flashcards: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<string> ExtractFromFileAsync()
    {
        if (SelectedFile is null)
        {
            ErrorMessage = "No file selected.";
            return string.Empty;
        }

        var result = await TextExtractor.ExtractAsync(SelectedFile);
        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            ErrorMessage = result.ErrorMessage;
        }

        return result.Text;
    }

    private void ClearAll()
    {
        SourceText = string.Empty;
        SelectedFile = null;
        _cards.Clear();
        TrainingState.Clear();
        ErrorMessage = null;
        InfoMessage = null;
        InputMode = InputModeKind.Text;
    }

    private void GoToTraining()
    {
        if (Cards.Count == 0)
        {
            return;
        }

        TrainingState.SetCards(_cards);
        NavigationManager.NavigateTo("/train");
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024d:0.0} KB";
        }

        return $"{bytes / (1024d * 1024d):0.0} MB";
    }

    private enum InputModeKind
    {
        Text,
        File
    }
}
