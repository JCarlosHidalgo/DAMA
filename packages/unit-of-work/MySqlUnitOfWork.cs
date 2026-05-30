using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace DAMA.Software.MySqlUnitOfWork;

public sealed class MySqlUnitOfWork : IUnitOfWork
{
    private readonly MySqlConnection _connection;

    public MySqlUnitOfWork(MySqlConnection connection)
    {
        _connection = connection;
    }

    public async Task<ITransactionScope> BeginAsync()
    {
        await MySQLRetryPolicy.EnsureOpenAsync(_connection);
        MySqlTransaction transaction = (MySqlTransaction)await _connection.BeginTransactionAsync();
        return new MySqlTransactionScope(transaction);
    }
}
