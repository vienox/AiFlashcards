using FlashcardsAI.Models;
using Microsoft.EntityFrameworkCore;

namespace FlashcardsAI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Deck> Decks => Set<Deck>();
    public DbSet<Flashcard> Flashcards => Set<Flashcard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(entity =>
        {
            entity.Property(a => a.DisplayName)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(a => a.DisplayName)
                .IsUnique();
        });

        modelBuilder.Entity<Deck>(entity =>
        {
            entity.Property(d => d.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(d => d.SourceName)
                .HasMaxLength(200);

            entity.HasOne(d => d.Account)
                .WithMany(a => a.Decks)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Flashcard>(entity =>
        {
            entity.Property(f => f.Front)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(f => f.Back)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(f => f.Tag)
                .HasMaxLength(100);

            entity.HasOne(f => f.Deck)
                .WithMany(d => d.Cards)
                .HasForeignKey(f => f.DeckId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
