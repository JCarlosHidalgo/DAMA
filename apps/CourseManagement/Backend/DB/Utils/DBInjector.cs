using System.Data;

using Backend.DB.Injectors;
using Backend.DB.Injectors.Courses;
using Backend.DB.Injectors.Scheduleds;
using Backend.DB.Injectors.Uniques;

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
        IDataInjector injector = new CourseInjector();
        injector.InjectData(connection);

        injector = new ScheduledClassInjector();
        injector.InjectData(connection);

        injector = new UniqueClassInjector();
        injector.InjectData(connection);

        injector = new ScheduledClassTeacherInjector();
        injector.InjectData(connection);

        injector = new UniqueClassTeacherInjector();
        injector.InjectData(connection);

        injector = new CourseIdempotencyInjector();
        injector.InjectData(connection);
    }
}
