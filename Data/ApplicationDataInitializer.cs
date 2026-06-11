using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using uis_bachelor_sustainability_webapp.Models;

namespace uis_bachelor_sustainability_webapp.Data;

public static class ApplicationDataInitializer
{
    public static void Initialize(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AdminUser>>();

        if (app.Environment.IsEnvironment("Testing"))
        {
            db.Database.EnsureCreated();
        }
        else
        {
            db.Database.Migrate();
        }

        EnsureAdminBootstrapAccount(db, passwordHasher, app.Configuration, app.Logger);

        if (!app.Environment.IsEnvironment("Testing"))
        {
            DemoBrandSeeder.Seed(db);
        }
    }

    private static string NormalizeUsername(string username)
    {
        return username.Trim().ToUpperInvariant();
    }

    private static void EnsureAdminBootstrapAccount(AppDbContext db, IPasswordHasher<AdminUser> passwordHasher, IConfiguration configuration, ILogger logger)
    {
        var bootstrapUser =
            configuration["ADMIN_BOOTSTRAP_USER"] ??
            Environment.GetEnvironmentVariable("ADMIN_BOOTSTRAP_USER") ??
            configuration["ADMIN_USER"] ??
            Environment.GetEnvironmentVariable("ADMIN_USER");

        var bootstrapPassword =
            configuration["ADMIN_BOOTSTRAP_PASSWORD"] ??
            Environment.GetEnvironmentVariable("ADMIN_BOOTSTRAP_PASSWORD") ??
            configuration["ADMIN_PASSWORD"] ??
            Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(bootstrapUser) || string.IsNullOrWhiteSpace(bootstrapPassword))
        {
            if (!db.AdminUsers.Any())
            {
                logger.LogWarning("No admin users exist and no bootstrap credentials were configured.");
            }
            return;
        }

        var normalizedUsername = NormalizeUsername(bootstrapUser);
        var exists = db.AdminUsers.Any(user => user.NormalizedUsername == normalizedUsername);
        if (exists)
        {
            return;
        }

        var adminUser = new AdminUser
        {
            Username = bootstrapUser.Trim(),
            NormalizedUsername = normalizedUsername,
            PasswordHash = string.Empty,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, bootstrapPassword);
        db.AdminUsers.Add(adminUser);
        db.SaveChanges();
        logger.LogInformation("Bootstrap admin user {User} was created.", adminUser.Username);
    }
}
