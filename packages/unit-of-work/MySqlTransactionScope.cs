using MySql.Data.MySqlClient;

namespace DAMA.Software.MySqlUnitOfWork;

public sealed class MySqlTransactionScope : ITransactionScope
{
    private readonly MySqlTransaction _transaction;
    private bool _committed;

    internal MySqlTransactionScope(MySqlTransaction transaction)
    {
        _transaction = transaction;
    }

    internal MySqlTransaction Transaction => _transaction;

    public async Task CommitAsync()
    {
        await _transaction.CommitAsync();
        _committed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_committed)
        {
            await _transaction.RollbackAsync();
        }
        await _transaction.DisposeAsync();
    }
}
