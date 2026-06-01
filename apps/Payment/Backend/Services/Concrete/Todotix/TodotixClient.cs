using System.Text.Json;

using Backend.Dtos.External.Todotix;
using Backend.Services.Abstract.Todotix;

namespace Backend.Services.Concrete.Todotix;

public sealed class TodotixClient(HttpClient httpClient,
                                  ILogger<TodotixClient> logger) : ITodotixClient
{
    public async Task<RegisterDebtResponse> RegisterDebtAsync(RegisterDebtRequest request)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync("/rest/deuda/registrar", request);
        response.EnsureSuccessStatusCode();
        RegisterDebtResponse? responseBody = await response.Content.ReadFromJsonAsync<RegisterDebtResponse>();
        return responseBody ?? throw new InvalidOperationException("Todotix RegisterDebt returned empty body.");
    }

    public async Task<bool> DebtExistsAsync(Guid debtIdentifier, string appKey)
    {
        ConsultDebtRequest request = new ConsultDebtRequest
        {
            Appkey = appKey,
            Identificador = debtIdentifier.ToString("D")
        };

        HttpResponseMessage response = await httpClient.PostAsJsonAsync("/rest/deuda/consultar_deudas/por_identificador", request);
        response.EnsureSuccessStatusCode();
        ConsultDebtResponse? responseBody = await response.Content.ReadFromJsonAsync<ConsultDebtResponse>();

        if (responseBody is null)
        {
            throw new InvalidOperationException("Todotix ConsultDebt returned empty body.");
        }

        return responseBody.Error == 0 && responseBody.Datos is not null;
    }

    public async Task<TodotixDebtState> ConsultDebtAsync(Guid debtIdentifier, string appKey)
    {
        ConsultDebtRequest request = new ConsultDebtRequest
        {
            Appkey = appKey,
            Identificador = debtIdentifier.ToString("D")
        };

        HttpResponseMessage response = await httpClient.PostAsJsonAsync("/rest/deuda/consultar_deudas/por_identificador", request);
        response.EnsureSuccessStatusCode();
        string rawBody = await response.Content.ReadAsStringAsync();

        ConsultDebtResponse? responseBody;
        try
        {
            responseBody = JsonSerializer.Deserialize<ConsultDebtResponse>(rawBody);
        }
        catch (JsonException deserializationException)
        {
            logger.LogWarning(
                deserializationException,
                "Todotix ConsultDebt {DebtId} body did not deserialize as ConsultDebtResponse. RawBody={RawBody}",
                debtIdentifier,
                rawBody);
            throw;
        }

        if (responseBody is null)
        {
            throw new InvalidOperationException("Todotix ConsultDebt returned empty body.");
        }

        bool paid = IsPaid(responseBody);
        if (!paid)
        {
            logger.LogWarning(
                "Todotix ConsultDebt {DebtId} returned Unpaid. Error={Error} Existente={Existente} Pagado={Pagado} PagoAnulado={PagoAnulado} RawBody={RawBody}",
                debtIdentifier,
                responseBody.Error,
                responseBody.Existente,
                responseBody.Datos?.Pagado,
                responseBody.Datos?.PagoAnulado,
                rawBody);
        }

        return paid ? TodotixDebtState.Paid : TodotixDebtState.Unpaid;
    }

    private static bool IsPaid(ConsultDebtResponse debtStatus)
    {
        return debtStatus.Error == 0
            && debtStatus.Datos is { Pagado: true, PagoAnulado: false };
    }
}
