using System.Data;

using Backend.DB.Injectors;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Utils;

public static class DBInjector
{
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

        injector = new SuccessQrPaymentInjector();
        injector.InjectData(connection);

        injector = new FailedQrPaymentInjector();
        injector.InjectData(connection);

        injector = new PendingQrPaymentInjector();
        injector.InjectData(connection);

        injector = new SuccessSubscriptionPaymentInjector();
        injector.InjectData(connection);
    }
}
