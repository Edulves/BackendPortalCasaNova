namespace PainelFelizCasaNova.Api.Services;

/// <summary>
/// Porta simplificada dos AUTH_PASSWORD_VALIDATORS do Django
/// (MinimumLength, CommonPassword, NumericPassword, UserAttributeSimilarity).
/// </summary>
public static class SenhaValidator
{
    private static readonly HashSet<string> SenhasComuns = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "123456", "12345678", "123456789", "qwerty", "abc123",
        "admin", "admin123", "administrador", "password1", "senha", "senha123",
        "1234567890", "iloveyou", "monkey", "dragon",
    };

    public static List<string> Validar(string senha, string? username = null)
    {
        var erros = new List<string>();
        if (string.IsNullOrEmpty(senha) || senha.Length < 8)
            erros.Add("Esta senha é muito curta. Ela precisa conter pelo menos 8 caracteres.");
        if (senha is not null && senha.All(char.IsDigit))
            erros.Add("Esta senha é inteiramente numérica.");
        if (senha is not null && SenhasComuns.Contains(senha))
            erros.Add("Esta senha é muito comum.");
        if (username is not null && senha is not null)
        {
            var u = username.Trim().ToLowerInvariant();
            var s = senha.ToLowerInvariant();
            if (u.Length >= 3 && s.Contains(u, StringComparison.Ordinal))
                erros.Add("A senha é muito parecida com o nome de usuário.");
        }
        return erros;
    }
}