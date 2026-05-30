using System.Text.Json.Serialization;

namespace Backend.Dtos.External.Todotix;

public class ConsultDebtRequest
{
    [JsonPropertyName("appkey")] public string Appkey { get; set; } = string.Empty;

    [JsonPropertyName("identificador")] public string Identificador { get; set; } = string.Empty;
}
