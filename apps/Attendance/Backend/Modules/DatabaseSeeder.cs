using Backend.DB.Utils;

namespace Backend.Modules;

public static class DatabaseSeeder
{
    public static void SeedIfEnabled()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("SEED_DB"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        using var seedConnection = DBConnector.CreateConnection();
        DBInjector.TruncateAllTables(seedConnection);
        DBInjector.InjectData(seedConnection);
    }
}
