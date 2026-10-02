using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Services;

/// <summary>
/// Emissão/validação de JWT nos moldes do djangorestframework-simplejwt:
/// access lifetime 8h, refresh 7d, claims user_id/papel/organizacao_id.
/// </summary>
public class TokenService
{
    public static class Claims
    {
        public const string TipoToken = "token_type";
        public const string UserId = "user_id";
        public const string Papel = "papel";
        public const string OrganizacaoId = "organizacao_id";
    }

    private readonly byte[] _segredo;
    private readonly TimeSpan _accessLifetime;
    private readonly TimeSpan _refreshLifetime;
    private readonly string _issuer;
    private readonly string _audience;

    public TokenService(IConfiguration config)
    {
        var secret = config["JWT_SECRET"]
            ?? config["DJANGO_SECRET_KEY"]
            ?? "dev-insecure-change-me-feliz-casa-nova-painel-32b";
        _segredo = Encoding.UTF8.GetBytes(secret);
        _accessLifetime = TimeSpan.FromHours(config.GetValue("JWT_ACCESS_HOURS", 8));
        _refreshLifetime = TimeSpan.FromDays(config.GetValue("JWT_REFRESH_DAYS", 7));
        _issuer = config["JWT_ISSUER"] ?? "painel-feliz-casa-nova";
        _audience = config["JWT_AUDIENCE"] ?? "painel-feliz-casa-nova";
    }

    private string Emitir(Dictionary<string, object> claims, DateTime exp)
    {
        var now = DateTimeOffset.UtcNow;
        var securityClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
        };
        foreach (var (key, value) in claims)
        {
            securityClaims.Add(new Claim(key, Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? ""));
        }

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(_segredo), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: securityClaims,
            notBefore: now.UtcDateTime,
            expires: exp,
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GerarAccess(Usuario user, DateTime? exp = null)
    {
        var claims = new Dictionary<string, object>
        {
            [Claims.TipoToken] = "access",
            [JwtRegisteredClaimNames.Sub] = user.Id,
            [Claims.UserId] = user.Id,
            [Claims.Papel] = user.Papel,
            [Claims.OrganizacaoId] = user.OrganizacaoId ?? 0,
        };
        return Emitir(claims, exp ?? DateTime.UtcNow.Add(_accessLifetime));
    }

    public string GerarRefresh(Usuario user, DateTime? exp = null)
    {
        var claims = new Dictionary<string, object>
        {
            [Claims.TipoToken] = "refresh",
            [JwtRegisteredClaimNames.Sub] = user.Id,
            [Claims.UserId] = user.Id,
            [Claims.Papel] = user.Papel,
            [Claims.OrganizacaoId] = user.OrganizacaoId ?? 0,
        };
        return Emitir(claims, exp ?? DateTime.UtcNow.Add(_refreshLifetime));
    }

    /// <summary>Valida um token e retorna os claims (null se inválido).</summary>
    public ClaimsPrincipal? Validar(string token, out JwtSecurityToken? securityToken)
    {
        securityToken = null;
        try
        {
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(_segredo),
                NameClaimType = JwtRegisteredClaimNames.Sub,
                RoleClaimType = Claims.Papel,
                ClockSkew = TimeSpan.FromSeconds(30),
            };
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, parameters, out var validated);
            securityToken = validated as JwtSecurityToken;
            return principal;
        }
        catch (Exception)
        {
            return null;
        }
    }
}