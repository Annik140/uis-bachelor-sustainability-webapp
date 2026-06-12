using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using uis_bachelor_sustainability_webapp.Models;

namespace uis_bachelor_sustainability_webapp.Data;

public static class ApplicationDataInitializer
{
    private const string SeedingModeConfigKey = "Seeding:Mode";
    private const int MinAdminPasswordLength = 6;

    public static void Initialize(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AdminUser>>();

        try
        {
            if (app.Environment.IsEnvironment("Testing"))
            {
                db.Database.EnsureCreated();
            }
            else
            {
                db.Database.Migrate();
            }
        }
        catch (Exception ex)
        {
            app.Logger.LogCritical(ex, "Failed to initialize database. Migrations or database creation failed.");
            throw;
        }

        try
        {
            EnsureAdminBootstrapAccount(db, passwordHasher, app.Configuration, app.Logger);

            if (!app.Environment.IsEnvironment("Testing"))
            {
                ApplyBrandSeeding(db, app.Configuration, app.Logger);
            }
        }
        catch (Exception ex)
        {
            app.Logger.LogCritical(ex, "Failed to initialize application data (admin or brand seeding).");
            throw;
        }
    }

    private static void ApplyBrandSeeding(AppDbContext db, IConfiguration configuration, ILogger logger)
    {
        var rawMode = configuration[SeedingModeConfigKey];
        var mode = (rawMode ?? "none").Trim().ToLowerInvariant();

        switch (mode)
        {
            case "none":
                logger.LogInformation("Brand seeding disabled. Set {ConfigKey}=Demo or Real to enable.", SeedingModeConfigKey);
                return;
            case "demo":
                DemoBrandSeeder.Seed(db);
                logger.LogInformation("Demo brand seeding completed.");
                return;
            case "real":
                RealBrandSeeder.Seed(db, logger);
                return;
            default:
                logger.LogWarning("Unknown seeding mode '{Mode}'. Supported modes: None, Demo, Real. Falling back to None.", rawMode);
                return;
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

        if (bootstrapPassword.Length < MinAdminPasswordLength)
        {
            logger.LogCritical("Bootstrap admin password is too short. It must be at least {MinLength} characters.", MinAdminPasswordLength);
            throw new InvalidOperationException($"Bootstrap admin password must be at least {MinAdminPasswordLength} characters.");
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
