using Backend.Application.Callbacks;
using Backend.Application.Commands;
using Backend.Application.Mediator;
using Backend.Application.Results;

namespace Backend.Application.Handlers;

public sealed class ProcessQrCallbackCommandHandler
    : ICommandHandler<ProcessQrCallbackCommand, ProcessQrCallbackResult>
{
    private readonly IEnumerable<IDebtCallbackStrategy> _strategies;

    public ProcessQrCallbackCommandHandler(IEnumerable<IDebtCallbackStrategy> strategies)
    {
        _strategies = strategies;
    }

    public async Task<ProcessQrCallbackResult> Handle(ProcessQrCallbackCommand command)
    {
        foreach (IDebtCallbackStrategy strategy in _strategies)
        {
            if (await strategy.TryHandleAsync(command.TransactionId))
            {
                return new ProcessQrCallbackResult.Processed();
            }
        }

        return new ProcessQrCallbackResult.DebtNotFound();
    }
}
