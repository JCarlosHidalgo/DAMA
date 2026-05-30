using System.Text.Json.Serialization;

namespace Backend.Dtos.External.Todotix;

public class RegisterDebtRequest
{
    [JsonPropertyName("appkey")] public string Appkey { get; set; } = string.Empty;

    [JsonPropertyName("email_cliente")] public string? EmailCliente { get; set; }

    [JsonPropertyName("identificador_deuda")] public string IdentificadorDeuda { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")] public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("callback_url")] public string CallbackUrl { get; set; } = string.Empty;

    [JsonPropertyName("fecha_vencimiento")] public string? FechaVencimiento { get; set; }

    [JsonPropertyName("lineas_detalle_deuda")] public List<RegisterDebtLine> LineasDetalleDeuda { get; set; } = new();
}
