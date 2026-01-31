using FlashcardsAI.Components;
using FlashcardsAI.Data;
using FlashcardsAI.Services.Ai;
using FlashcardsAI.Services.Data;
using FlashcardsAI.Services.TextExtraction;
using FlashcardsAI.Services.Training;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;


var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddHttpClient<IAiFlashcardGenerator, OpenAiFlashcardGenerator>(client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
});
builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=flashcards.db";
    options.UseSqlite(connectionString);
});
builder.Services.AddScoped<ITextExtractor, FileTextExtractor>();
builder.Services.AddScoped<TrainingState>();
builder.Services.AddScoped<FlashcardStore>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = dbFactory.CreateDbContext();
    if (db.Database.GetMigrations().Any())
    {
        db.Database.Migrate();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
