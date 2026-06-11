namespace uis_bachelor_sustainability_webapp.Data;

public static class RealBrandSeeder
{
    public static void Seed(AppDbContext db, ILogger logger)
    {
        // Placeholder mode so production choice can be configured now.
        // Real brand seeding can be implemented from exported DB data later.
        logger.LogInformation("Real brand seeding mode selected, but no real brand seed set is configured yet.");
    }
}
