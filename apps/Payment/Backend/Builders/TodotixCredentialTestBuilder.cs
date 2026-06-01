using System.Globalization;

using Backend.Dtos.External.Todotix;
using Backend.Options;
using Backend.Services.Abstract;

using Microsoft.Extensions.Options;

namespace Backend.Builders;

public class TodotixCredentialTestBuilder : ITodotixCredentialTestBuilder
{
    private const string TestDescription = "Prueba de credencial";
    private const string TestEmail = "example@email.com";

    private readonly ICallbackSignature _callbackSignature;
    private readonly IOptions<TodotixOptions> _todotixOptions;

    public TodotixCredentialTestBuilder(ICallbackSignature callbackSignature,
                                        IOptions<TodotixOptions> todotixOptions)
    {
        _callbackSignature = callbackSignature;
        _todotixOptions = todotixOptions;
    }

    private string TodotixCallbackUrl => _todotixOptions.Value.CallbackUrl;

    public RegisterDebtRequest BuildCredentialTestRequest(string appKey, string tenantTimezone)
    {
        Guid debtIdentifier = Guid.NewGuid();
        TimeZoneInfo tenantZone = TimeZoneInfo.FindSystemTimeZoneById(tenantTimezone);
        DateTime expirationDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.AddDays(1), tenantZone);

        return new RegisterDebtRequest
        {
            Appkey = appKey,
            EmailCliente = TestEmail,
            IdentificadorDeuda = debtIdentifier.ToString(),
            Descripcion = TestDescription,
            CallbackUrl = BuildSignedCallbackUrl(debtIdentifier),
            FechaVencimiento = expirationDate.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            LineasDetalleDeuda = new List<RegisterDebtLine>
            {
                new RegisterDebtLine
                {
                    Concepto = TestDescription,
                    Cantidad = 1,
                    CostoUnitario = 1,
                    DescuentoUnitario = 0
                }
            }
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
