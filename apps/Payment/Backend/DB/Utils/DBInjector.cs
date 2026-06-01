using System.Data;

using Backend.DB.Injectors;
using Backend.Security;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Utils;

public static class DBInjector
{
    private const string TenantExampleId = "c4c1c44d-7b5c-414a-b8df-a5537357ee30";

    public static void TruncateAllTables(MySqlConnection connection)
    {
        MySqlCommand truncateCommand = new MySqlCommand("TruncateAllTables", connection);
        truncateCommand.CommandType = CommandType.StoredProcedure;
        truncateCommand.ExecuteNonQuery();
    }

    public static void InjectData(MySqlConnection connection)
    {
        IDataInjector injector = new DebtTemplateInjector();
        injector.InjectData(connection);
        InjectTenantExamplePaymentCredentials(connection);
    }

    private static void InjectTenantExamplePaymentCredentials(MySqlConnection connection)
    {
        string? applicationKey = Environment.GetEnvironmentVariable("TODOTIX_APPKEY");
        string? encryptionKeyBase64 = Environment.GetEnvironmentVariable("TODOTIX_APPKEY_ENCRYPTION_KEY");
        if (string.IsNullOrWhiteSpace(applicationKey) || string.IsNullOrWhiteSpace(encryptionKeyBase64))
        {
            return;
        }

        IAppKeyCipher appKeyCipher = new AppKeyCipher(Convert.FromBase64String(encryptionKeyBase64));
        string encryptedAppKey = appKeyCipher.Encrypt(applicationKey);

        MySqlCommand upsertCommand = new MySqlCommand("UpsertPaymentCredentialsForTenant", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        upsertCommand.Parameters.AddWithValue("@tenantId", TenantExampleId);
        upsertCommand.Parameters.AddWithValue("@todotixAppKey", encryptedAppKey);
        upsertCommand.ExecuteNonQuery();
    }
}
