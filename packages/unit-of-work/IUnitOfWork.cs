namespace DAMA.Software.MySqlUnitOfWork;

public interface IUnitOfWork
{
    Task<ITransactionScope> BeginAsync();
}
