using System.Text.Json;

namespace PainelFelizCasaNova.Api.Services;

/// <summary>Espelho do <c>core/analytics.py</c> — eventos de produto em logs estruturados.</summary>
public class AnalyticsService
{
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(ILogger<AnalyticsService> logger) => _logger = logger;

    public void TrackEvent(string name, int? userId = null, int? orgId = null,
        params (string Key, object? Value)[] props)
    {
        var payload = new Dictionary<string, object?>
        {
            ["event"] = name,
        };
        if (userId is not null) payload["user_id"] = userId.Value;
        if (orgId is not null) payload["org_id"] = orgId.Value;
        foreach (var (key, value) in props)
        {
            payload[key] = value;
        }
        _logger.LogInformation("analytics {Payload}",
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
    }
}