using System.Reflection;

using MySql.Data.MySqlClient;

namespace Test.Infrastructure;

public static class MySqlExceptionFactory
{
    public static MySqlException DuplicateKey(string message = "duplicate") => WithErrorNumber(message, 1062);

    public static MySqlException WithErrorNumber(string message, int errorNumber)
    {
        ConstructorInfo constructor = typeof(MySqlException)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .First(c =>
            {
                ParameterInfo[] parameters = c.GetParameters();
                return parameters.Length == 2
                       && parameters[0].ParameterType == typeof(string)
                       && parameters[1].ParameterType == typeof(int);
            });
        return (MySqlException)constructor.Invoke([message, errorNumber]);
    }
}
