using System.Globalization;
using System.Text.Json;

using Backend.Dtos.External.Todotix;
using Backend.Dtos.QrPayments.Output;
using Backend.Entities;
using Backend.Entities.DebtTemplates;
using Backend.Entities.QrPayments;
using Backend.Entities.Todotix;
using Backend.Events;
using Backend.Options;
using Backend.Services.Abstract;

using Microsoft.Extensions.Options;

namespace Backend.Builders;

public class QrPaymentCreationBuilder : IQrPaymentCreationBuilder
{
    private readonly ICallbackSignature _callbackSignature;
    private readonly IOptions<TodotixOptions> _todotixOptions;

    public QrPaymentCreationBuilder(ICallbackSignature callbackSignature,
                                    IOptions<TodotixOptions> todotixOptions)
    {
        _callbackSignature = callbackSignature;
        _todotixOptions = todotixOptions;
    }

    private string TodotixApplicationKey => _todotixOptions.Value.ApplicationKey;
    private string TodotixCallbackUrl => _todotixOptions.Value.CallbackUrl;

    public PendingQrPayment BuildPendingPayment(Guid debtIdentifier, Guid tenantId, Guid studentId, Guid templateId, DebtTemplate template, DateTime expiresAtUtc)
    {
        return new PendingQrPayment
        {
            Id = debtIdentifier,
            TenantId = tenantId,
            StudentId = studentId,
            TemplateId = templateId,
            ClassQuantity = template.ClassQuantity,
            Cost = template.Cost,
            QrImageUrl = null,
            ExpiresAt = expiresAtUtc
        };
    }

    public RegisterDebtRequest BuildTodotixRequest(Guid debtIdentifier, string? email, DebtTemplate template, string tenantTimezone, string description, DateTime expiresAtUtc)
    {
        TimeZoneInfo tenantZone = TimeZoneInfo.FindSystemTimeZoneById(tenantTimezone);
        DateTime expirationDate = TimeZoneInfo.ConvertTimeFromUtc(expiresAtUtc, tenantZone);

        return new RegisterDebtRequest
        {
            Appkey = TodotixApplicationKey,
            EmailCliente = string.IsNullOrEmpty(email) ? null : email,
            IdentificadorDeuda = debtIdentifier.ToString(),
            Descripcion = description,
            CallbackUrl = BuildSignedCallbackUrl(debtIdentifier),
            FechaVencimiento = expirationDate.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            LineasDetalleDeuda = new List<RegisterDebtLine>
            {
                new RegisterDebtLine
                {
                    Concepto = template.Description,
                    Cantidad = 1,
                    CostoUnitario = template.Cost,
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
            PayloadJson = JsonSerializer.Serialize(todotixRequest),
            OccurredAt = DateTime.UtcNow,
            Status = "Pending"
        };
    }

    public ExpirationOutboxEvent BuildExpirationOutboxEvent(Guid debtIdentifier, Guid tenantId, Guid studentId, DateTime availableAtUtc)
    {
        Guid eventId = Guid.NewGuid();
        DateTime occurredAt = DateTime.UtcNow;

        DebtExpiredEvent domainEvent = new DebtExpiredEvent
        {
            EventId = eventId,
            EventType = "DebtExpired",
            OccurredAt = occurredAt,
            AggregateId = debtIdentifier,
            Data = new DebtExpiredData
            {
                PendingId = debtIdentifier,
                TenantId = tenantId,
                StudentId = studentId
            }
        };

        return new ExpirationOutboxEvent
        {
            Id = eventId,
            AggregateId = debtIdentifier,
            EventType = "DebtExpired",
            RoutingKey = "debt.expired",
            Payload = JsonSerializer.Serialize(domainEvent),
            OccurredAt = occurredAt,
            AvailableAt = availableAtUtc
        };
    }

    public QrDebtPendingDto BuildPendingDebtDto(Guid debtIdentifier)
    {
        return new QrDebtPendingDto
        {
            IdentificadorDeuda = debtIdentifier,
            Status = "Pending"
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
