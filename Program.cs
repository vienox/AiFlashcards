using FlashcardsAI.Components;
using FlashcardsAI.Data;
using FlashcardsAI.Services.Ai;
using FlashcardsAI.Services.Data;
using FlashcardsAI.Services.TextExtraction;
using FlashcardsAI.Services.Training;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
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
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=flashcards.db";
    options.UseSqlite(connectionString);
});
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
});
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<ITextExtractor, FileTextExtractor>();
builder.Services.AddScoped<TrainingState>();
builder.Services.AddScoped<FlashcardStore>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/account/register", async (RegisterRequest request, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager) =>
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Username and password are required." });
        }

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return Results.BadRequest(new { error = "Passwords do not match." });
        }

        var user = new IdentityUser
        {
            UserName = request.UserName.Trim()
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
            return Results.BadRequest(new { error = errorMessage });
        }

        await signInManager.SignInAsync(user, isPersistent: false);

        return Results.Ok(new { returnUrl = AuthHelpers.GetSafeReturnUrl(request.ReturnUrl) });
    })
    .AllowAnonymous();

app.MapPost("/account/login", async (LoginRequest request, SignInManager<IdentityUser> signInManager) =>
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Username and password are required." });
        }

        var result = await signInManager.PasswordSignInAsync(
            request.UserName.Trim(),
            request.Password,
            request.RememberMe,
            lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return Results.BadRequest(new { error = "Invalid username or password." });
        }

        return Results.Ok(new { returnUrl = AuthHelpers.GetSafeReturnUrl(request.ReturnUrl) });
    })
    .AllowAnonymous();

app.MapPost("/account/logout", async (SignInManager<IdentityUser> signInManager) =>
    {
        await signInManager.SignOutAsync();
        return Results.Ok();
    })
    .RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static class AuthHelpers
{
    public static string GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        return returnUrl.StartsWith("/") ? returnUrl : "/";
    }
}

record RegisterRequest(string UserName, string Password, string ConfirmPassword, string? ReturnUrl);
record LoginRequest(string UserName, string Password, bool RememberMe, string? ReturnUrl);
