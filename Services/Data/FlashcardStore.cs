using FlashcardsAI.Data;
using FlashcardsAI.Models;
using Microsoft.EntityFrameworkCore;

namespace FlashcardsAI.Services.Data;

public class FlashcardStore
{
    private readonly AppDbContext _db;

    public FlashcardStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Deck> SaveDeckAsync(
        string accountName,
        Deck deck,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accountName))
        {
            throw new ArgumentException("Account name is required.", nameof(accountName));
        }

        var normalized = accountName.Trim();

        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.DisplayName == normalized, ct);

        if (account is null)
        {
            account = new Account { DisplayName = normalized };
            _db.Accounts.Add(account);
        }

        deck.Account = account;
        _db.Decks.Add(deck);

        await _db.SaveChangesAsync(ct);

        return deck;
    }
}
