using System.Collections.Concurrent;
using System.Security.Claims;
using PainelFelizCasaNova.Api.Services;

namespace PainelFelizCasaNova.Api.Middleware;

/// <summary>
/// Rate limit em memória — espelho do DRF (LoginRateThrottle por IP,
/// BurstAnonRateThrottle e BurstUserRateThrottle). Janela fixa de 60s.
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _log;
    private readonly bool _habilitado;
    private readonly int _loginPorMinuto;
    private readonly int _anonPorMinuto;
    private readonly int _userPorMinuto;

    private static readonly ConcurrentDictionary<string, (int Contador, long JanelaInicio)> Janelas = new();

    public RateLimitMiddleware(RequestDelegate next, IConfiguration config, ILogger<RateLimitMiddleware> log)
    {
        _next = next;
        _log = log;
        _habilitado = (config["RATE_LIMIT_ENABLED"] ?? "1") != "0";
        _loginPorMinuto = LegadoInt(config["DRF_THROTTLE_LOGIN"], 20);
        _anonPorMinuto = LegadoInt(config["DRF_THROTTLE_ANON"], 60);
        _userPorMinuto = LegadoInt(config["DRF_THROTTLE_USER"], 300);
    }

    private static int LegadoInt(string? valor, int padrao)
    {
        if (string.IsNullOrWhiteSpace(valor)) return padrao;
        if (valor.EndsWith("/min") && int.TryParse(valor[..^4], out var n)) return n;
        return int.TryParse(valor, out var n2) ? n2 : padrao;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_habilitado)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.ToString();
        if (!path.StartsWith("/api"))
        {
            await _next(context);
            return;
        }

        var agora = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var janelaAtual = agora / 60;

        // Login + password-reset: por IP (LoginRateThrottle)
        if (path.StartsWith("/api/auth/token") || path.StartsWith("/api/auth/password-reset"))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "?";
            if (!Permitir($"login:{ip}", _loginPorMinuto, janelaAtual))
            {
                await EscreverThrottledAsync(context, "login");
                return;
            }
        }

        // Usuário autenticado: por user id (BurstUserRateThrottle)
        var userId = context.User?.FindFirstValue(TokenService.Claims.UserId);
        if (!string.IsNullOrEmpty(userId))
        {
            if (!Permitir($"user:{userId}", _userPorMinuto, janelaAtual))
            {
                await EscreverThrottledAsync(context, "user");
                return;
            }
        }
        else if (context.Request.Method is "POST" or "PUT" or "PATCH" or "DELETE")
        {
            // Anônimo em escrita: por IP (BurstAnonRateThrottle)
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "?";
            if (!Permitir($"anon:{ip}", _anonPorMinuto, janelaAtual))
            {
                await EscreverThrottledAsync(context, "anon");
                return;
            }
        }

        await _next(context);
    }

    private static bool Permitir(string chave, int limite, long janelaAtual)
    {
        if (limite <= 0) return true;
        var agora = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var atual = Janelas.AddOrUpdate(chave,
            _ => (1, janelaAtual),
            (_, entrada) =>
            {
                if (entrada.JanelaInicio != janelaAtual)
                    return (1, janelaAtual);
                return (entrada.Contador + 1, entrada.JanelaInicio);
            });
        _ = agora;
        return atual.Contador <= limite;
    }

    private async Task EscreverThrottledAsync(HttpContext context, string escopo)
    {
        _log.LogWarning("throttled escopo={Escopo} path={Path}", escopo, context.Request.Path);
        var msg = "Muitas tentativas. Aguarde um momento.";
        await ErrorEnvelope.EscreverAsync(context, 429, ApiCodes.Throttled, msg, msg);
    }
}