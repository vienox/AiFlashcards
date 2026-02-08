using FlashcardsAI.Data;
using FlashcardsAI.Models;
using Microsoft.AspNetCore.Identity;
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
        string userId,
        Deck deck,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        // Create account if not exists (using userId as both Id and DisplayName)
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == Guid.Parse(userId), ct);

        if (account is null)
        {
            account = new Account 
            { 
                Id = Guid.Parse(userId),
                DisplayName = userId 
            };
            _db.Accounts.Add(account);
        }

        deck.AccountId = account.Id;
        deck.Account = account;
        _db.Decks.Add(deck);

        await _db.SaveChangesAsync(ct);

        return deck;
    }

    public async Task<List<Deck>> GetUserDecksAsync(
        string userId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new List<Deck>();
        }

        var accountId = Guid.Parse(userId);
        return await _db.Decks
            .Where(d => d.AccountId == accountId)
            .Include(d => d.Cards)
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<Deck?> GetDeckByIdAsync(
        Guid deckId,
        CancellationToken ct = default)
    {
        return await _db.Decks
            .Include(d => d.Cards)
            .FirstOrDefaultAsync(d => d.Id == deckId, ct);
    }
}
