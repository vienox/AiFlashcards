using FlashcardsAI.Models;

namespace FlashcardsAI.Services.Training;

public sealed class TrainingState
{
    private readonly List<Flashcard> _cards = new();
    private readonly List<Flashcard> _wrongCards = new();

    public IReadOnlyList<Flashcard> Cards => _cards;
    public int CurrentIndex { get; private set; }
    public bool HasCards => _cards.Count > 0;
    public Flashcard? CurrentCard => _cards.Count == 0 ? null : _cards[CurrentIndex];
    public int CorrectCount { get; private set; }
    public int TotalCardsAtStart { get; private set; }

    public void SetCards(IEnumerable<Flashcard> cards)
    {
        _cards.Clear();
        _wrongCards.Clear();
        if (cards is not null)
        {
            _cards.AddRange(cards);
        }

        CurrentIndex = 0;
        CorrectCount = 0;
        TotalCardsAtStart = _cards.Count;
    }

    public void Clear()
    {
        _cards.Clear();
        _wrongCards.Clear();
        CurrentIndex = 0;
        CorrectCount = 0;
        TotalCardsAtStart = 0;
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

    public void MarkCorrect()
    {
        if (_cards.Count == 0)
        {
            return;
        }

        CorrectCount++;
        _cards.RemoveAt(CurrentIndex);

        if (CurrentIndex >= _cards.Count && _cards.Count > 0)
        {
            CurrentIndex = 0;
        }
    }

    public void MarkWrong()
    {
        if (_cards.Count == 0)
        {
            return;
        }

        _wrongCards.Add(_cards[CurrentIndex]);
        _cards.RemoveAt(CurrentIndex);

        if (CurrentIndex >= _cards.Count && _cards.Count > 0)
        {
            CurrentIndex = 0;
        }
    }

    public void ReplayWrongCards()
    {
        if (_wrongCards.Count == 0)
        {
            return;
        }

        _cards.Clear();
        _cards.AddRange(_wrongCards);
        _wrongCards.Clear();
        CurrentIndex = 0;
        CorrectCount = 0;
        TotalCardsAtStart = _cards.Count;
    }

    public bool HasWrongCards => _wrongCards.Count > 0;
    public int WrongCardsCount => _wrongCards.Count;
}
