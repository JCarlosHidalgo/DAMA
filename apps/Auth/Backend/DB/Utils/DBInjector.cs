using System.Data;

using Backend.DB.Injectors;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Utils;

public static class DBInjector
{
    public static void TruncateAllTables(MySqlConnection connection)
    {
        MySqlCommand com = new MySqlCommand("TruncateAllTables", connection);
        com.CommandType = CommandType.StoredProcedure;
        com.ExecuteNonQuery();
    }

    public static void InjectData(MySqlConnection connection)
    {
        IDataInjector injector = new TenantDataInjector();
        injector.InjectData(connection);

        injector = new TenantAllowedServicesDataInjector();
        injector.InjectData(connection);

        injector = new UserDataInjector();
        injector.InjectData(connection);

        injector = new TenantDomainDataInjector();
        injector.InjectData(connection);
    }
}
