using System.Data;

using Backend.DB.Injectors;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Utils;

public static class DBInjector
{
    public static void TruncateAllTables(MySqlConnection connection)
    {
        MySqlCommand command = new MySqlCommand("TruncateAllTables", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.ExecuteNonQuery();
    }

    public static void InjectData(MySqlConnection connection)
    {
        IDataInjector injector = new ScheduledClassAttendanceInjector();
        injector.InjectData(connection);

        injector = new UniqueClassAttendanceInjector();
        injector.InjectData(connection);

        injector = new StudentRemainClassesInjector();
        injector.InjectData(connection);
    }
}
