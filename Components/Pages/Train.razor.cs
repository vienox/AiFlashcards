using FlashcardsAI.Services.Training;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace FlashcardsAI.Components.Pages;

public partial class Train
{
    private ElementReference CardRef;
    private bool IsFlipped { get; set; }
    private bool ShowingResults { get; set; }

    [Inject] public TrainingState TrainingState { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    private int Progress =>
        TrainingState.HasCards
            ? (int)Math.Round((TrainingState.CurrentIndex + 1) * 100d / TrainingState.Cards.Count)
            : 0;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (TrainingState.HasCards)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("flashcardsTraining.init", CardRef);
                await JSRuntime.InvokeVoidAsync("flashcardsTraining.resize", CardRef);
            }
            catch (JSException)
            {
            }
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (!TrainingState.HasCards || ShowingResults)
        {
            return;
        }

        switch (e.Key.ToLower())
        {
            case " ":
            case "spacebar":
                await FlipCardAsync();
                break;
            case "arrowleft":
                if (!IsFlipped)
                {
                    await Previous();
                }
                break;
            case "arrowright":
                if (!IsFlipped)
                {
                    await Next();
                }
                break;
            case "1":
            case "y":
                if (IsFlipped)
                {
                    await MarkCorrect();
                }
                break;
            case "2":
            case "n":
                if (IsFlipped)
                {
                    await MarkWrong();
                }
                break;
        }
    }

    private async Task FlipCardAsync()
    {
        if (!TrainingState.HasCards)
        {
            return;
        }

        IsFlipped = !IsFlipped;
        try
        {
            await JSRuntime.InvokeVoidAsync("flashcardsTraining.flip", CardRef);
        }
        catch (JSException)
        {
            // JS not available yet; rely on Blazor state.
        }
    }

    private async Task Next()
    {
        TrainingState.Next();
        IsFlipped = false;
        try
        {
            await JSRuntime.InvokeVoidAsync("flashcardsTraining.reset", CardRef);
            await JSRuntime.InvokeVoidAsync("flashcardsTraining.resize", CardRef);
        }
        catch (JSException)
        {
        }
    }

    private async Task Previous()
    {
        TrainingState.Previous();
        IsFlipped = false;
        try
        {
            await JSRuntime.InvokeVoidAsync("flashcardsTraining.reset", CardRef);
            await JSRuntime.InvokeVoidAsync("flashcardsTraining.resize", CardRef);
        }
        catch (JSException)
        {
        }
    }

    private async Task Shuffle()
    {
        TrainingState.Shuffle();
        IsFlipped = false;
        try
        {
            await JSRuntime.InvokeVoidAsync("flashcardsTraining.reset", CardRef);
            await JSRuntime.InvokeVoidAsync("flashcardsTraining.resize", CardRef);
        }
        catch (JSException)
        {
        }
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo("/");
    }

    private async Task MarkCorrect()
    {
        TrainingState.MarkCorrect();
        IsFlipped = false;

        if (!TrainingState.HasCards)
        {
            // If there are wrong cards, automatically replay them
            if (TrainingState.HasWrongCards)
            {
                TrainingState.ReplayWrongCards();
            }
            else
            {
                // All cards correct - show results
                ShowingResults = true;
            }
        }

        try
        {
            await JSRuntime.InvokeVoidAsync("flashcardsTraining.reset", CardRef);
            await JSRuntime.InvokeVoidAsync("flashcardsTraining.resize", CardRef);
        }
        catch (JSException)
        {
        }
    }

    private async Task MarkWrong()
    {
        TrainingState.MarkWrong();
        IsFlipped = false;

        if (!TrainingState.HasCards)
        {
            // If there are wrong cards, automatically replay them
            if (TrainingState.HasWrongCards)
            {
                TrainingState.ReplayWrongCards();
            }
            else
            {
                // All cards correct - show results
                ShowingResults = true;
            }
        }

        try
        {
            await JSRuntime.InvokeVoidAsync("flashcardsTraining.reset", CardRef);
            await JSRuntime.InvokeVoidAsync("flashcardsTraining.resize", CardRef);
        }
        catch (JSException)
        {
        }
    }

    private async Task ReplayWrongCards()
    {
        TrainingState.ReplayWrongCards();
        ShowingResults = false;
        IsFlipped = false;

        try
        {
            await JSRuntime.InvokeVoidAsync("flashcardsTraining.reset", CardRef);
            await JSRuntime.InvokeVoidAsync("flashcardsTraining.resize", CardRef);
        }
        catch (JSException)
        {
        }
    }
}
