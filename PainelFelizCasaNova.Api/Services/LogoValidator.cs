using System.Text.RegularExpressions;

namespace PainelFelizCasaNova.Api.Services;

/// <summary>Espelho do <c>core/validators/logo.py</c> — logo em data URL base64.</summary>
public static class LogoValidator
{
    public const int LogoMaxBytes = 500 * 1024;

    private static readonly Regex LogoDataUrlRe = new(
        @"^data:image/(png|jpe?g|gif|webp|svg\+xml);base64,",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Retorna o valor limpo ou lança ValidationException com a mensagem.</summary>
    public static string? Validar(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var trimmed = value.Trim();
        if (!LogoDataUrlRe.IsMatch(trimmed))
            throw new ValidationException("Logo deve ser PNG, JPEG, GIF, WebP ou SVG em base64.");
        try
        {
            var raw = Convert.FromBase64String(trimmed.Split(',', 2)[1]);
            if (raw.Length > LogoMaxBytes)
                throw new ValidationException($"Logo excede {LogoMaxBytes / 1024} KB.");
        }
        catch (FormatException)
        {
            throw new ValidationException("Base64 do logo inválido.");
        }
        return trimmed;
    }
}

/// <summary>Erro de validação de campo — vira 400 {campo: [msgs]} no envelope da API.</summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}