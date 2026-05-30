namespace Backend.Application.Infrastructure;

public abstract record ClassCreationOutcome<TEntity> where TEntity : class
{
    private ClassCreationOutcome() { }

    public sealed record Created(TEntity Entity) : ClassCreationOutcome<TEntity>;

    public sealed record Replayed(TEntity Prior) : ClassCreationOutcome<TEntity>;

    public sealed record CourseMissing : ClassCreationOutcome<TEntity>;
}
