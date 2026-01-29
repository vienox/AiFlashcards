using FlashcardsAI.Models;

namespace FlashcardsAI.Services.Training;

public sealed class TrainingState
{
    private readonly List<Flashcard> _cards = new();

    public IReadOnlyList<Flashcard> Cards => _cards;
    public int CurrentIndex { get; private set; }
    public bool HasCards => _cards.Count > 0;
    public Flashcard? CurrentCard => _cards.Count == 0 ? null : _cards[CurrentIndex];

    public void SetCards(IEnumerable<Flashcard> cards)
    {
        _cards.Clear();
        if (cards is not null)
        {
            _cards.AddRange(cards);
        }

        CurrentIndex = 0;
    }

    public void Clear()
    {
        _cards.Clear();
        CurrentIndex = 0;
    }

    public void Next()
    {
        if (_cards.Count == 0)
        {
            return;
        }

        CurrentIndex = (CurrentIndex + 1) % _cards.Count;
    }

    public void Previous()
    {
        if (_cards.Count == 0)
        {
            return;
        }

        CurrentIndex = (CurrentIndex - 1 + _cards.Count) % _cards.Count;
    }

    public void Shuffle()
    {
        if (_cards.Count < 2)
        {
            return;
        }

        for (var i = _cards.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }

        CurrentIndex = 0;
    }
}
