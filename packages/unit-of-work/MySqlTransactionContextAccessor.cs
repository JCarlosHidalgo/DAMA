using MySql.Data.MySqlClient;

namespace DAMA.Software.MySqlUnitOfWork;

public static class MySqlTransactionContextAccessor
{
    public static MySqlTransaction Unwrap(ITransactionContext context)
    {
        if (context is MySqlTransactionScope scope)
        {
            return scope.Transaction;
        }

        throw new InvalidOperationException("ITransactionContext is not a MySqlTransactionScope.");
    }
}
