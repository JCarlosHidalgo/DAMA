namespace DAMA.Software.MySqlUnitOfWork;

public static class UnitOfWorkExtensions
{
    public static async Task<TResult> RunInTransactionAsync<TResult>(
        this IUnitOfWork unitOfWork,
        Func<ITransactionContext, Task<(TResult Result, bool ShouldCommit)>> work)
    {
        await using ITransactionScope scope = await unitOfWork.BeginAsync();

        (TResult result, bool shouldCommit) = await work(scope);
        if (shouldCommit)
        {
            await scope.CommitAsync();
        }

        return result;
    }

    public static async Task RunInTransactionAsync(
        this IUnitOfWork unitOfWork,
        Func<ITransactionContext, Task> work)
    {
        await using ITransactionScope scope = await unitOfWork.BeginAsync();
        await work(scope);
        await scope.CommitAsync();
    }
}
