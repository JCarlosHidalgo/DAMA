namespace Backend.Application.Infrastructure;

public abstract record IdempotentInsertOutcome<TEntity> where TEntity : class
{
    private IdempotentInsertOutcome() { }

    public sealed record Inserted(TEntity Entity) : IdempotentInsertOutcome<TEntity>;

    public sealed record Replayed(TEntity Prior) : IdempotentInsertOutcome<TEntity>;

    public sealed record InsertFailed : IdempotentInsertOutcome<TEntity>;
}
