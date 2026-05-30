namespace DAMA.Software.MySqlUnitOfWork;

public interface ITransactionScope : ITransactionContext, IAsyncDisposable
{
    Task CommitAsync();
}
