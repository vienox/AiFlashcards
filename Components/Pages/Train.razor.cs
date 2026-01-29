using FlashcardsAI.Services.Training;
using Microsoft.AspNetCore.Components;

namespace FlashcardsAI.Components.Pages;

public partial class Train
{
    private bool IsFlipped { get; set; }

    [Inject] public TrainingState TrainingState { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    private void ToggleFlip()
    {
        if (!TrainingState.HasCards)
        {
            return;
        }

        IsFlipped = !IsFlipped;
    }

    private void Next()
    {
        TrainingState.Next();
        IsFlipped = false;
    }

    private void Previous()
    {
        TrainingState.Previous();
        IsFlipped = false;
    }

    private void Shuffle()
    {
        TrainingState.Shuffle();
        IsFlipped = false;
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo("/");
    }
}
