using System.Text.Json;
using PainelFelizCasaNova.Api.Services;

namespace PainelFelizCasaNova.Api.Middleware;

/// <summary>
/// Conversor de erros para o envelope {code, message, detail} — espelho do
/// <c>core.exceptions.api_exception_handler</c> + handler404/429 do Django.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _log;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            if (context.Response.HasStarted) return;

            if (context.Response.StatusCode == 404)
            {
                var isApi = context.Request.Path.StartsWithSegments("/api");
                var msg = isApi ? "Recurso não encontrado." : "Página não encontrada.";
                await ErrorEnvelope.EscreverAsync(context, 404, ApiCodes.NotFound, msg, msg);
            }
            else if (context.Response.StatusCode is 401 or 403 or 429)
            {
                var msg = context.Response.StatusCode switch
                {
                    401 => "Credenciais não fornecidas ou inválidas.",
                    403 => "Você não tem permissão para executar esta ação.",
                    429 => "Muitas tentativas. Aguarde um momento.",
                    _ => "Erro na requisição",
                };
                await ErrorEnvelope.EscreverAsync(context, context.Response.StatusCode,
                    ApiCodes.ParaStatus(context.Response.StatusCode), msg, msg);
            }
        }
        catch (ApiException ex)
        {
            var message = ErrorEnvelope.Achatar(ex.Detail ?? ex.Message);
            await ErrorEnvelope.EscreverAsync(context, ex.StatusCode,
                ex.Code ?? ApiCodes.ParaStatus(ex.StatusCode), message, ex.Detail ?? ex.Message);
            if (ex.StatusCode >= 500)
                _log.LogError("api_error code={Code} status={Status} path={Path} msg={Message}",
                    ApiCodes.ParaStatus(ex.StatusCode), ex.StatusCode, context.Request.Path, message);
            else if (ex.StatusCode == 429)
                _log.LogWarning("throttled path={Path}", context.Request.Path);
        }
        catch (ValidationException ex)
        {
            await ErrorEnvelope.EscreverAsync(context, 400, ApiCodes.ValidationError, ex.Message, ex.Message);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "unhandled_error path={Path}", context.Request.Path);
            await ErrorEnvelope.EscreverAsync(context, 500, ApiCodes.ServerError,
                "Erro interno. Tente de novo ou avise o suporte.", "Erro interno.");
        }
    }
}

public static class ErrorEnvelope
{
    /// <summary>Achata o payload de erro (espelho de _flatten_errors do DRF).</summary>
    public static string Achatar(object? data)
    {
        switch (data)
        {
            case null:
                return "Erro na requisição";
            case string s:
                return s;
            case Dictionary<string, object?> dict:
                return AchatarDict(dict);
            case IDictionary<string, object?> idict:
                return AchatarDict(idict);
            case System.Collections.IEnumerable seq:
                return string.Join("; ", seq.Cast<object?>().Select(Achatar));
            default:
                return Convert.ToString(data) ?? "";
        }
    }

    private static string AchatarDict(IDictionary<string, object?> dict)
    {
        if (dict.Count == 1 && dict.TryGetValue("detail", out var d)) return Achatar(d);
        var partes = new List<string>();
        foreach (var kv in dict) partes.Add($"{kv.Key}: {Achatar(kv.Value)}");
        return string.Join("; ", partes);
    }

    public static async Task EscreverAsync(HttpContext context, int status, string code,
        string message, object? detail)
    {
        if (context.Response.HasStarted) return;
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        var payload = new Dictionary<string, object?>
        {
            ["code"] = code,
            ["message"] = message,
            ["detail"] = detail,
        };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
        await context.Response.WriteAsync(json);
    }
}