using FlashcardsAI.Services.Training;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FlashcardsAI.Components.Pages;

public partial class Train
{
    private ElementReference CardRef;
    private bool IsFlipped { get; set; }

    [Inject] public TrainingState TrainingState { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (TrainingState.HasCards)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("flashcardsTraining.init", CardRef);
            }
            catch (JSException)
            {
            }
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
        }
        catch (JSException)
        {
        }
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo("/");
    }
}
