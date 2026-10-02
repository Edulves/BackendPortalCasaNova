using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Data;
using PainelFelizCasaNova.Api.Dtos;
using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Services;

/// <summary>
/// Autenticação JWT + reset de senha + convite de usuários.
/// Espelho de auth_views.py, CustomTokenObtainPairSerializer e TokenRefreshView.
/// </summary>
public class AuthService
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokens;
    private readonly PasswordResetTokenService _resetTokens;
    private readonly MailService _mail;
    private readonly AnalyticsService _analytics;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly ILogger<AuthService> _log;
    private readonly bool _debug;
    private readonly bool _consoleEmail;

    public AuthService(AppDbContext db, TokenService tokens, PasswordResetTokenService resetTokens,
        MailService mail, AnalyticsService analytics, IPasswordHasher<Usuario> hasher,
        IConfiguration config, ILogger<AuthService> log)
    {
        _db = db;
        _tokens = tokens;
        _resetTokens = resetTokens;
        _mail = mail;
        _analytics = analytics;
        _hasher = hasher;
        _log = log;
        _debug = (config["DJANGO_DEBUG"] ?? "1") == "1";
        _consoleEmail = mail.UsaConsole;
    }

    public async Task<TokenResponse> LoginAsync(TokenRequest request)
    {
        var login = request.Username?.Trim() ?? "";
        if (string.IsNullOrEmpty(login) || request.Password is null)
            throw new ApiException(401, "Não existe conta ativa com as credenciais fornecidas.");

        var usuario = await _db.Usuarios
            .Include(u => u.Organizacao)
            .FirstOrDefaultAsync(u => u.Username == login);
        if (usuario is null)
            usuario = await _db.Usuarios
                .Include(u => u.Organizacao)
                .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == login.ToLowerInvariant());

        if (usuario is null || !usuario.IsActive ||
            _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password) ==
            PasswordVerificationResult.Failed)
        {
            throw new ApiException(401, "Não existe conta ativa com as credenciais fornecidas.");
        }

        _analytics.TrackEvent("login", userId: usuario.Id, orgId: usuario.OrganizacaoId);

        return BuildTokenResponse(usuario);
    }

    public TokenResponse BuildTokenResponse(Usuario usuario)
    {
        var access = _tokens.GerarAccess(usuario);
        var refresh = _tokens.GerarRefresh(usuario);
        return new TokenResponse
        {
            Access = access,
            Refresh = refresh,
            Papel = usuario.Papel,
            Username = usuario.Username,
            OrganizacaoId = usuario.OrganizacaoId,
            OrganizacaoNome = usuario.Organizacao?.Nome,
            PodeEscrever = usuario.PodeEscrever,
            PodeAdministrar = usuario.PodeAdministrar,
        };
    }

    public async Task<RefreshResponse> RefreshAsync(string refresh)
    {
        if (string.IsNullOrWhiteSpace(refresh))
            throw new ApiException(401, "Token de refresh inválido ou expirado.");

        var principal = _tokens.Validar(refresh, out var tok);
        var userId = principal?.FindFirstValue(TokenService.Claims.UserId)
            ?? principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (principal is null || tok is null || userId is null ||
            tok.Claims.FirstOrDefault(c => c.Type == TokenService.Claims.TipoToken)?.Value != "refresh")
        {
            throw new ApiException(401, "Token de refresh inválido ou expirado.");
        }

        if (!int.TryParse(userId, out var id)) throw new ApiException(401, "Token inválido.");
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        if (usuario is null || !usuario.IsActive)
            throw new ApiException(401, "Conta não encontrada ou inativa.");

        // simplejwt copia os claims do refresh para o novo access.
        var papelClaim = tok.Claims.FirstOrDefault(c => c.Type == TokenService.Claims.Papel)?.Value;
        var orgClaim = tok.Claims.FirstOrDefault(c => c.Type == TokenService.Claims.OrganizacaoId)?.Value;
        if (papelClaim is null || !Models.Papel.EhValido(papelClaim)) papelClaim = usuario.Papel;
        usuario.Papel = papelClaim;
        if (int.TryParse(orgClaim, out var orgId)) usuario.OrganizacaoId = orgId;

        return new RefreshResponse { Access = _tokens.GerarAccess(usuario) };
    }

    public async Task<MessageResponse> RequestPasswordResetAsync(PasswordResetRequest request)
    {
        var email = request.Email?.Trim() ?? "";
        var username = request.Username?.Trim() ?? "";

        Usuario? usuario = null;
        if (!string.IsNullOrEmpty(email))
            usuario = await _db.Usuarios.FirstOrDefaultAsync(u =>
                u.Email != null && u.Email.ToLower() == email.ToLowerInvariant());
        if (usuario is null && !string.IsNullOrEmpty(username))
            usuario = await _db.Usuarios.FirstOrDefaultAsync(u =>
                u.Username.ToLower() == username.ToLowerInvariant());

        var okMsg = new MessageResponse
        {
            Code = ApiCodes.Ok,
            Message = "Se existir uma conta, enviamos instruções de redefinição. "
                    + "Em ambiente local, veja o e-mail no log do backend.",
        };

        if (usuario is null || !usuario.IsActive) return okMsg;

        var uid = PasswordResetTokenService.UrlsafeBase64Encode(usuario.Id);
        var token = _resetTokens.GerarToken(usuario);
        var front = _mail.FrontendBaseUrl.TrimEnd('/');
        var link = $"{front}/#reset={uid}.{token}";

        if (!string.IsNullOrWhiteSpace(usuario.Email))
            _mail.EnviarResetSenha(usuario.Email, usuario.Username, link);
        else
            _log.LogInformation("password_reset_link_no_email user={User} link={Link}",
                usuario.Username, link);

        if (_debug || _consoleEmail) okMsg.DebugLink = link;
        return okMsg;
    }

    public async Task<MessageResponse> ConfirmPasswordResetAsync(PasswordResetConfirmRequest request)
    {
        var uid = request.Uid?.Trim() ?? "";
        var token = request.Token?.Trim() ?? "";
        var newPassword = request.NewPassword ?? "";

        if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(token))
            throw new ApiException(400, "Link inválido ou expirado.", ApiCodes.InvalidToken);

        var uidInt = PasswordResetTokenService.UrlsafeBase64Decode(uid);
        if (uidInt is null)
            throw new ApiException(400, "Link inválido ou expirado.", ApiCodes.InvalidToken);

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == uidInt.Value);
        if (usuario is null || !_resetTokens.ChecarToken(usuario, token))
            throw new ApiException(400, "Link inválido ou expirado.", ApiCodes.InvalidToken);

        var erros = SenhaValidator.Validar(newPassword, usuario.Username);
        if (erros.Count > 0)
            throw new ApiException(400, "Erro na requisição", ApiCodes.ValidationError,
                new Dictionary<string, object?> { ["new_password"] = erros });

        usuario.PasswordHash = _hasher.HashPassword(usuario, newPassword);
        await _db.SaveChangesAsync();
        _log.LogInformation("password_reset_ok user={User}", usuario.Username);
        return new MessageResponse { Code = ApiCodes.Ok, Message = "Senha atualizada. Faça login." };
    }

    public async Task<UsuarioPublicResponse> InviteAsync(InviteUserRequest request, int organizacaoId)
    {
        var username = (request.Username ?? "").Trim();
        if (username.Length is < 1 or > 150)
            throw new ApiException(400, "Usuário inválido.", ApiCodes.ValidationError,
                new Dictionary<string, object?> { ["username"] = new[] { "Informe um usuário válido." } });

        var papel = string.IsNullOrEmpty(request.Papel) ? Models.Papel.Operador : request.Papel;
        if (!Models.Papel.EhValido(papel))
            throw new ApiException(400, "Papel inválido.", ApiCodes.ValidationError,
                new Dictionary<string, object?> { ["papel"] = new[] { "Escolha admin, operador ou visualizador." } });

        if (await _db.Usuarios.AnyAsync(u => u.Username.ToLower() == username.ToLowerInvariant()))
            throw new ApiException(400, "Usuário já existe.", ApiCodes.ValidationError);

        var erros = SenhaValidator.Validar(request.Password ?? "", username);
        if (erros.Count > 0)
            throw new ApiException(400, "Erro na requisição", ApiCodes.ValidationError,
                new Dictionary<string, object?> { ["password"] = erros });

        var user = new Usuario
        {
            Username = username,
            Email = request.Email?.Trim() ?? "",
            Papel = papel,
            OrganizacaoId = organizacaoId,
        };
        user.PasswordHash = _hasher.HashPassword(user, request.Password ?? "");
        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        _log.LogInformation("user_invited by={By} user={User} papel={Papel}",
            "?", user.Username, user.Papel);
        return new UsuarioPublicResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Papel = user.Papel,
            IsActive = user.IsActive,
        };
    }
}