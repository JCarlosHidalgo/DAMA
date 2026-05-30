using System.Text.Json.Serialization;

namespace Backend.Dtos.External.Todotix;

public class RegisterDebtLine
{
    [JsonPropertyName("concepto")] public string Concepto { get; set; } = string.Empty;

    [JsonPropertyName("cantidad")] public int Cantidad { get; set; }

    [JsonPropertyName("costo_unitario")] public int CostoUnitario { get; set; }

    [JsonPropertyName("descuento_unitario")] public int DescuentoUnitario { get; set; }
}
