using System.Net;
using System.Net.Mail;

namespace PainelFelizCasaNova.Api.Services;

/// <summary>
/// Envio de e-mail — espelho do EMAIL_BACKEND do Django.
/// Em desenvolvimento (console/debug) apenas loga o link; com SMTP envia de verdade.
/// </summary>
public class MailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<MailService> _logger;

    public MailService(IConfiguration config, ILogger<MailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public bool UsaConsole =>
        bool.TryParse(_config["EMAIL_USE_CONSOLE"], out var b) ? b : true;

    public string? FrontendBaseUrl =>
        _config["FRONTEND_BASE_URL"] ?? "http://localhost:8080";

    public string DefaultFromEmail =>
        _config["DEFAULT_FROM_EMAIL"] ?? "noreply@felizcasanova.local";

    /// <summary>Envia (ou loga) o e-mail de reset. Devolve o link para debug.</summary>
    public void EnviarResetSenha(string destinatario, string username, string link)
    {
        var subject = "Redefinir senha — Feliz Casa Nova";
        var body = $"Olá {username},\n\nPara redefinir sua senha, abra o link:\n{link}\n\n"
                 + "Se você não pediu isso, ignore este e-mail.\n";

        if (UsaConsole)
        {
            _logger.LogInformation(
                "EMAIL_CONSOLE to={To} subject={Subject}\n{Body}\nRESET_LINK={Link}",
                destinatario, subject, body, link);
            return;
        }

        try
        {
            using var client = new SmtpClient(
                _config["EMAIL_HOST"] ?? "",
                int.TryParse(_config["EMAIL_PORT"], out var p) ? p : 587)
            {
                Credentials = !string.IsNullOrEmpty(_config["EMAIL_HOST_USER"])
                    ? new NetworkCredential(
                        _config["EMAIL_HOST_USER"], _config["EMAIL_HOST_PASSWORD"])
                    : null,
                EnableSsl = (_config["EMAIL_USE_TLS"] ?? "1") != "0",
            };
            client.Send(DefaultFromEmail, destinatario, subject, body);
            _logger.LogInformation("password_reset_email_sent user={User}", username);
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "password_reset_email_failed user={User}", username);
            _logger.LogInformation("password_reset_link user={User} link={Link}", username, link);
        }
    }
}