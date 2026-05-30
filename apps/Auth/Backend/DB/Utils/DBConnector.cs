using MySql.Data.MySqlClient;

namespace Backend.DB.Utils;

public static class DBConnector
{
    public static string GetConnectionString()
    {
        string fromEnvironment = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? "";
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "";
        var config =
            new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("dbsettings.json", true)
            .AddJsonFile($"dbsettings.{environment}.json", true)
            .Build();

        return config["DbSettings:ConnectionUrl"] ?? "";
    }

    public static MySqlConnection CreateConnection()
    {
        var connection = new MySqlConnection(GetConnectionString());
        connection.Open();
        return connection;
    }
}
