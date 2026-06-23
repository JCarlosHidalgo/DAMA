using System.Text.Json;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Backend.ExternalCheck;

public static class ReadinessResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = false
    };

    public static Task WriteAsync(HttpContext httpContext, HealthReport healthReport)
    {
        httpContext.Response.ContentType = "application/json";

        var payload = new
        {
            status = healthReport.Status.ToString(),
            totalDurationMs = healthReport.TotalDuration.TotalMilliseconds,
            checks = healthReport.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                error = entry.Value.Exception?.Message
            })
        };

        return httpContext.Response.WriteAsync(JsonSerializer.Serialize(payload, SerializerOptions));
    }
}
