using System.Text.Json;

using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Dtos.External.Todotix;
using Backend.Entities.Todotix;
using Backend.Results.Todotix;
using Backend.Services.Abstract.Todotix;

namespace Backend.Services.Concrete.Todotix;

public sealed class TodotixDebtPublisher(
    ITodotixClient todotixClient,
    IPendingQrPaymentDao pendingQrPaymentDao) : IPaymentDebtPublisher
{
    public async Task<PublishOutcome> PublishAsync(TodotixOutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        RegisterDebtRequest? registerDebtRequest;
        try
        {
            registerDebtRequest = JsonSerializer.Deserialize<RegisterDebtRequest>(outboxEvent.PayloadJson);
        }
        catch (JsonException jsonException)
        {
            return new PublishOutcome.PermanentFailure($"outbox payload not deserializable: {jsonException.Message}");
        }

        if (registerDebtRequest is null)
        {
            return new PublishOutcome.PermanentFailure("outbox payload deserialized as null");
        }

        if (outboxEvent.Attempts > 0)
        {
            PublishOutcome? alreadyRegisteredOutcome = await SkipIfAlreadyRegisteredAsync(outboxEvent.PendingId);
            if (alreadyRegisteredOutcome is not null)
            {
                return alreadyRegisteredOutcome;
            }
        }

        RegisterDebtResponse registerDebtResponse;
        try
        {
            registerDebtResponse = await todotixClient.RegisterDebtAsync(registerDebtRequest);
        }
        catch (Exception todotixException) when (todotixException is not OperationCanceledException)
        {
            return new PublishOutcome.TransientFailure(todotixException.Message);
        }

        if (registerDebtResponse.Error == 0 && !string.IsNullOrEmpty(registerDebtResponse.QrSimpleUrl))
        {
            await pendingQrPaymentDao.UpdateQrImageUrlAsync(outboxEvent.PendingId, registerDebtResponse.QrSimpleUrl);
            return new PublishOutcome.Success();
        }

        if (registerDebtResponse.Existente != 0)
        {
            return new PublishOutcome.Success();
        }

        string errorMessage = $"Todotix error={registerDebtResponse.Error} mensaje={registerDebtResponse.Mensaje} existente={registerDebtResponse.Existente}";
        return new PublishOutcome.TransientFailure(errorMessage);
    }

    private async Task<PublishOutcome?> SkipIfAlreadyRegisteredAsync(Guid debtIdentifier)
    {
        try
        {
            bool alreadyRegistered = await todotixClient.DebtExistsAsync(debtIdentifier);
            return alreadyRegistered ? new PublishOutcome.Success() : null;
        }
        catch (Exception consultException) when (consultException is not OperationCanceledException)
        {
            return new PublishOutcome.TransientFailure($"existence check failed: {consultException.Message}");
        }
    }
}
