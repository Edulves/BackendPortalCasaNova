using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using PainelFelizCasaNova.Api.Models;
using PainelFelizCasaNova.Api.Services;

namespace PainelFelizCasaNova.Api.Auth;

public static class Politicas
{
    public const string PodeLer = "PodeLer";
    public const string PodeEscrever = "PodeEscrever";
    public const string PodeAdministrar = "PodeAdministrar";
    public const string AdminOrg = "AdminOrg";

    public static AuthorizationOptions Registrar(AuthorizationOptions options)
    {
        options.AddPolicy(PodeLer, p => p.AddRequirements(new PermissaoRequirement(PermissaoNivel.Ler)));
        options.AddPolicy(PodeEscrever, p => p.AddRequirements(new PermissaoRequirement(PermissaoNivel.Escrever)));
        options.AddPolicy(PodeAdministrar, p => p.AddRequirements(new PermissaoRequirement(PermissaoNivel.Administrar)));
        options.AddPolicy(AdminOrg, p => p.AddRequirements(new PermissaoRequirement(PermissaoNivel.Ler, exigirAdminSempre: true)));
        return options;
    }
}

public enum PermissaoNivel
{
    Ler = 1,
    Escrever = 2,
    Administrar = 3,
}

public class PermissaoRequirement : IAuthorizationRequirement
{
    public PermissaoNivel Nivel { get; }
    public bool ExigirAdminSempre { get; }

    public PermissaoRequirement(PermissaoNivel nivel, bool exigirAdminSempre = false)
    {
        Nivel = nivel;
        ExigirAdminSempre = exigirAdminSempre;
    }
}

/// <summary>
/// Espelho de core/permissions.py: usuário autenticado com organização; escrita
/// conforme papel; admin sempre (IsAdminOrg) quando exigido.
/// </summary>
public class PermissaoHandler : AuthorizationHandler<PermissaoRequirement>
{
    private readonly IHttpContextAccessor _http;

    public PermissaoHandler(IHttpContextAccessor http) => _http = http;

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        PermissaoRequirement requirement)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var papel = user.FindFirstValue(TokenService.Claims.Papel);
        var orgClaim = user.FindFirstValue(TokenService.Claims.OrganizacaoId);
        var temOrganizacao = int.TryParse(orgClaim, out var orgId) && orgId > 0;

        if (requirement.ExigirAdminSempre)
        {
            if (papel == Models.Papel.Admin) context.Succeed(requirement);
            else context.Fail();
            return Task.CompletedTask;
        }

        if (!temOrganizacao)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var metodo = _http.HttpContext?.Request.Method ?? "GET";
        var ehSeguro = metodo is "GET" or "HEAD" or "OPTIONS";
        if (ehSeguro)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var nivelPapel = Models.Papel.Nivel(papel);
        if (nivelPapel >= (int)requirement.Nivel)
            context.Succeed(requirement);
        else
            context.Fail();

        return Task.CompletedTask;
    }
}

public static class ClaimsExtensions
{
    public static int? ObterUsuarioId(this ClaimsPrincipal user)
    {
        var v = user.FindFirstValue(TokenService.Claims.UserId)
            ?? user.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return v is not null && int.TryParse(v, out var id) ? id : null;
    }

    public static int? ObterOrganizacaoId(this ClaimsPrincipal user)
    {
        var v = user.FindFirstValue(TokenService.Claims.OrganizacaoId);
        return v is not null && int.TryParse(v, out var id) && id > 0 ? id : null;
    }
}