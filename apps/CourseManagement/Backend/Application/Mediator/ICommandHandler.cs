namespace Backend.Application.Mediator;

public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> Handle(TCommand command);
}
