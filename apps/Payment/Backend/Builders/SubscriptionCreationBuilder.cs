using System.Globalization;
using System.Text.Json;

using Backend.Dtos.External.Todotix;
using Backend.Dtos.QrPayments.Output;
using Backend.Entities;
using Backend.Entities.Subscriptions;
using Backend.Entities.Todotix;
using Backend.Options;
using Backend.Services.Abstract;

using Microsoft.Extensions.Options;

namespace Backend.Builders;

public class SubscriptionCreationBuilder : ISubscriptionCreationBuilder
{
    private readonly ICallbackSignature _callbackSignature;
    private readonly IOptions<TodotixOptions> _todotixOptions;

    public SubscriptionCreationBuilder(ICallbackSignature callbackSignature,
                                       IOptions<TodotixOptions> todotixOptions)
    {
        _callbackSignature = callbackSignature;
        _todotixOptions = todotixOptions;
    }

    private string TodotixCallbackUrl => _todotixOptions.Value.CallbackUrl;

    public PendingSubscriptionPayment BuildPendingPayment(Guid debtIdentifier, Guid tenantId, SubscriptionPlan plan, DateTime expiresAtUtc)
    {
        return new PendingSubscriptionPayment
        {
            Id = debtIdentifier,
            TenantId = tenantId,
            Level = plan.Level,
            Cost = plan.Price,
            Currency = plan.Currency,
            QrImageUrl = null,
            ExpiresAt = expiresAtUtc
        };
    }

    public RegisterDebtRequest BuildTodotixRequest(Guid debtIdentifier, string? email, SubscriptionPlan plan, string tenantTimezone, string description, DateTime expiresAtUtc, string appKey)
    {
        TimeZoneInfo tenantZone = TimeZoneInfo.FindSystemTimeZoneById(tenantTimezone);
        DateTime expirationDate = TimeZoneInfo.ConvertTimeFromUtc(expiresAtUtc, tenantZone);

        return new RegisterDebtRequest
        {
            Appkey = appKey,
            EmailCliente = string.IsNullOrEmpty(email) ? null : email,
            IdentificadorDeuda = debtIdentifier.ToString(),
            Descripcion = description,
            CallbackUrl = BuildSignedCallbackUrl(debtIdentifier),
            FechaVencimiento = expirationDate.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            LineasDetalleDeuda = new List<RegisterDebtLine>
            {
                new RegisterDebtLine
                {
                    Concepto = description,
                    Cantidad = 1,
                    CostoUnitario = plan.Price,
                    DescuentoUnitario = 0
                }
            }
        };
    }

    public TodotixOutboxEvent BuildOutboxEvent(Guid debtIdentifier, Guid tenantId, RegisterDebtRequest todotixRequest)
    {
        return new TodotixOutboxEvent
        {
            Id = debtIdentifier,
            PendingId = debtIdentifier,
            TenantId = tenantId,
            DebtKind = DebtKind.TenantSubscription,
            PayloadJson = JsonSerializer.Serialize(todotixRequest),
            OccurredAt = DateTime.UtcNow,
            Status = "Pending"
        };
    }

    public QrDebtPendingDto BuildPendingDebtDto(Guid debtIdentifier, bool alreadyGenerated = false)
    {
        return new QrDebtPendingDto
        {
            IdentificadorDeuda = debtIdentifier,
            Status = "Pending",
            AlreadyGenerated = alreadyGenerated
        };
    }

    private string BuildSignedCallbackUrl(Guid transactionId)
    {
        string signature = _callbackSignature.Sign(transactionId.ToString("D"));
        string signatureParameter = "sig=" + Uri.EscapeDataString(signature);

        UriBuilder uriBuilder = new UriBuilder(TodotixCallbackUrl);
        string existingQuery = uriBuilder.Query.TrimStart('?');
        uriBuilder.Query = string.IsNullOrEmpty(existingQuery) ? signatureParameter : existingQuery + "&" + signatureParameter;
        return uriBuilder.Uri.ToString();
    }
}
