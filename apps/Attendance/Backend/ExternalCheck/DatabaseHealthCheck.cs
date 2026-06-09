using Backend.DB.Utils;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using MySql.Data.MySqlClient;

namespace Backend.ExternalCheck;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using MySqlConnection connection = new MySqlConnection(DBConnector.GetConnectionString());
            await connection.OpenAsync(cancellationToken);

            await using MySqlCommand command = new MySqlCommand("SELECT 1;", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("Database connection succeeded.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Database connection failed.", exception);
        }
    }
}
