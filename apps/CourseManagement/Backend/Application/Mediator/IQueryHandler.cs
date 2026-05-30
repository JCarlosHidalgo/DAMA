namespace Backend.Application.Mediator;

public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> Handle(TQuery query);
}
