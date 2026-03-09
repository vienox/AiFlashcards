using System.IO;
using System.Linq;
using FlashcardsAI.Models;
using FlashcardsAI.Services.Ai;
using FlashcardsAI.Services.Data;
using FlashcardsAI.Services.TextExtraction;
using FlashcardsAI.Services.Training;
using FlashcardsAI.Components.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace FlashcardsAI.Components.Pages;

public partial class Home
{
    private readonly List<Flashcard> _cards = new();
    private List<Deck> _savedDecks = new();

    private string SourceText { get; set; } = string.Empty;
    private IBrowserFile? SelectedFile { get; set; }
    private int CardCount { get; set; } = 8;
    private bool IsBusy { get; set; }
    private string? ErrorMessage { get; set; }
    private string? InfoMessage { get; set; }
    private InputModeKind _inputMode = InputModeKind.Text;
    private string DeckFilter { get; set; } = string.Empty;

    [CascadingParameter] private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    [Inject] public IAiFlashcardGenerator AiGenerator { get; set; } = default!;
    [Inject] public ITextExtractor TextExtractor { get; set; } = default!;
    [Inject] public TrainingState TrainingState { get; set; } = default!;
    [Inject] public FlashcardStore FlashcardStore { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    [Inject] public IDialogService DialogService { get; set; } = default!;

    private IReadOnlyList<Flashcard> Cards => _cards;
    private IReadOnlyList<Deck> SavedDecks => _savedDecks;
    private IReadOnlyList<Deck> VisibleDecks =>
        string.IsNullOrWhiteSpace(DeckFilter)
            ? _savedDecks
            : _savedDecks
                .Where(deck => DeckMatchesFilter(deck, DeckFilter))
                .ToList();

    protected override async Task OnInitializedAsync()
    {
        await LoadSavedDecksAsync();
    }

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

        var authState = await AuthenticationStateTask;
        var user = authState.User;
        if (user?.Identity is null || !user.Identity.IsAuthenticated)
        {
            ErrorMessage = "You must be logged in.";
            return;
        }

        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            ErrorMessage = "Unable to determine user ID.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        InfoMessage = null;

        try
        {
            var options = new GenerateOptions { Count = CardCount };
            List<Flashcard> result;
            string sourceTextForTitle;

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

                    sourceTextForTitle = text;
                    result = await AiGenerator.GenerateAsync(text, options);
                }
                else
                {
                    sourceTextForTitle = Path.GetFileNameWithoutExtension(SelectedFile.Name);
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

                sourceTextForTitle = text;
                result = await AiGenerator.GenerateAsync(text, options);
            }

            _cards.Clear();
            _cards.AddRange(result);
            TrainingState.SetCards(_cards);

            // Generate AI title
            var deckTitle = await AiGenerator.GenerateDeckTitleAsync(sourceTextForTitle);

            var deck = new Deck
            {
                Title = deckTitle,
                SourceName = InputMode == InputModeKind.File ? SelectedFile?.Name : "Text input",
                Cards = _cards.ToList()
            };

            await FlashcardStore.SaveDeckAsync(userId, deck);
            await LoadSavedDecksAsync();
            InfoMessage = $"Generated and saved {Cards.Count} flashcards: \"{deckTitle}\".";
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

    private async Task LoadSavedDecksAsync()
    {
        try
        {
            var authState = await AuthenticationStateTask;
            var user = authState.User;
            if (user?.Identity is null || !user.Identity.IsAuthenticated)
            {
                _savedDecks.Clear();
                return;
            }

            var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                _savedDecks.Clear();
                return;
            }

            _savedDecks = await FlashcardStore.GetUserDecksAsync(userId);
        }
        catch
        {
            _savedDecks.Clear();
        }
    }

    private void LoadDeckAndTrain(Deck deck)
    {
        if (deck?.Cards is null || deck.Cards.Count == 0)
        {
            return;
        }

        TrainingState.SetCards(deck.Cards);
        NavigationManager.NavigateTo("/train");
    }

    private async Task DeleteDeckAsync(Deck deck)
    {
        if (deck is null || IsBusy)
        {
            return;
        }

        var result = await DialogService.ShowMessageBox(
            "Delete Deck",
            $"Are you sure you want to delete \"{deck.Title}\"? This action cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");

        if (result != true)
        {
            return;
        }

        try
        {
            var authState = await AuthenticationStateTask;
            var user = authState.User;
            if (user?.Identity is null || !user.Identity.IsAuthenticated)
            {
                return;
            }

            var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            var success = await FlashcardStore.DeleteDeckAsync(deck.Id, userId);
            if (success)
            {
                await LoadSavedDecksAsync();
                InfoMessage = $"Deleted \"{deck.Title}\".";
            }
        }
        catch
        {
            ErrorMessage = "Failed to delete deck.";
        }
    }

    private async Task EditDeckTitleAsync(Deck deck)
    {
        if (deck is null || IsBusy)
        {
            return;
        }

        var parameters = new DialogParameters
        {
            { "Title", "Edit Deck Name" },
            { "Text", deck.Title },
            { "Label", "Deck Name" }
        };

        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<SimpleTextDialog>("Edit Deck Name", parameters, options);
        var result = await dialog.Result;

        if (result.Canceled || result.Data is not string newTitle || string.IsNullOrWhiteSpace(newTitle))
        {
            return;
        }

        try
        {
            var authState = await AuthenticationStateTask;
            var user = authState.User;
            if (user?.Identity is null || !user.Identity.IsAuthenticated)
            {
                return;
            }

            var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            var success = await FlashcardStore.UpdateDeckTitleAsync(deck.Id, userId, newTitle);
            if (success)
            {
                await LoadSavedDecksAsync();
                InfoMessage = $"Renamed to \"{newTitle}\".";
            }
        }
        catch
        {
            ErrorMessage = "Failed to update deck name.";
        }
    }

    private static bool DeckMatchesFilter(Deck deck, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        var term = filter.Trim();
        if (term.Length == 0)
        {
            return true;
        }

        return (deck.Title?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
            || (deck.SourceName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false);
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
