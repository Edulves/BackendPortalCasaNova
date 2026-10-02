using PainelFelizCasaNova.Api.Middleware;

namespace PainelFelizCasaNova.Api;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandlingMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<ErrorHandlingMiddleware>();

    public static IApplicationBuilder UseRateLimitMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<RateLimitMiddleware>();
}