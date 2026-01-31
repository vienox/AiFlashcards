using FlashcardsAI.Data;
using FlashcardsAI.Models;
using Microsoft.EntityFrameworkCore;

namespace FlashcardsAI.Services.Data;

public class FlashcardStore
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public FlashcardStore(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
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

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.DisplayName == normalized, ct);

        if (account is null)
        {
            account = new Account { DisplayName = normalized };
            db.Accounts.Add(account);
        }

        deck.Account = account;
        db.Decks.Add(deck);

        await db.SaveChangesAsync(ct);

        return deck;
    }
}
