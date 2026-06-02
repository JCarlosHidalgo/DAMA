namespace Backend.Application.Results;

public abstract record ProcessQrCallbackResult
{
    private ProcessQrCallbackResult() { }

    public sealed record Processed : ProcessQrCallbackResult;

    public sealed record DebtNotFound : ProcessQrCallbackResult;
}
