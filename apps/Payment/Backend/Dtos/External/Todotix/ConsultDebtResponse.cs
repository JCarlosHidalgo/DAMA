using System.Text.Json.Serialization;

namespace Backend.Dtos.External.Todotix;

public class ConsultDebtResponse
{
    [JsonPropertyName("error")] public int Error { get; set; }

    [JsonPropertyName("existente")] public int Existente { get; set; }

    [JsonPropertyName("mensaje")] public string? Mensaje { get; set; }

    [JsonPropertyName("datos")] public ConsultDebtData? Datos { get; set; }
}
