using System.Text.Json.Serialization;

namespace Backend.Dtos.External.Todotix;

public class RegisterDebtResponse
{
    [JsonPropertyName("error")] public int Error { get; set; }

    [JsonPropertyName("existente")] public int Existente { get; set; }

    [JsonPropertyName("mensaje")] public string? Mensaje { get; set; }

    [JsonPropertyName("codigo_recaudacion")] public string? CodigoRecaudacion { get; set; }

    [JsonPropertyName("id_transaccion")] public string? IdTransaccion { get; set; }

    [JsonPropertyName("qr_simple_url")] public string? QrSimpleUrl { get; set; }

    [JsonPropertyName("monto_total")] public decimal? MontoTotal { get; set; }

    [JsonPropertyName("url_pasarela_pagos")] public string? UrlPasarelaPagos { get; set; }

    [JsonPropertyName("pagado")] public bool? Pagado { get; set; }
}
