using System.Text.Json.Serialization;

namespace Backend.Dtos.External.Todotix;

public class ConsultDebtData
{
    [JsonPropertyName("identificador")] public string? Identificador { get; set; }

    [JsonPropertyName("deuda_expirada")] public bool DeudaExpirada { get; set; }

    [JsonPropertyName("pagado")] public bool Pagado { get; set; }

    [JsonPropertyName("pago_anulado")] public bool PagoAnulado { get; set; }

    [JsonPropertyName("fecha_pago")] public string? FechaPago { get; set; }

    [JsonPropertyName("callback_exitoso")] public bool CallbackExitoso { get; set; }
}
