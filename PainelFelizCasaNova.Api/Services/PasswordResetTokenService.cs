using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Services;

/// <summary>
/// Porta fiel do <c>django.contrib.auth.tokens.PasswordResetTokenGenerator</c>
/// + urlsafe_base64. O esquema é HMAC-SHA256 com key_salt fixo e secret do app.
/// </summary>
public class PasswordResetTokenService
{
    private const string KeySalt = "django.contrib.auth.tokens.PasswordResetTokenGenerator";

    private readonly byte[] _secretBytes;

    public PasswordResetTokenService(IConfiguration config)
    {
        var secret = config["JWT_SECRET"]
            ?? config["DJANGO_SECRET_KEY"]
            ?? "dev-insecure-change-me-feliz-casa-nova-painel-32b";
        _secretBytes = Encoding.UTF8.GetBytes(secret);
    }

    private static long DiasDesdeEpoch() =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds() / (24 * 3600);

    private static string IntToBase36(long value)
    {
        const string chars = "0123456789abcdefghijklmnopqrstuvwxyz";
        if (value == 0) return "0";
        var sign = value < 0 ? "-" : "";
        long n = Math.Abs(value);
        var sb = new StringBuilder();
        while (n > 0)
        {
            sb.Insert(0, chars[(int)(n % 36)]);
            n /= 36;
        }
        return sign + sb;
    }

    private static long Base36ToInt(string s)
    {
        const string chars = "0123456789abcdefghijklmnopqrstuvwxyz";
        long v = 0;
        foreach (var c in s)
        {
            var idx = chars.IndexOf(c);
            if (idx < 0) throw new FormatException("base36 inválido");
            v = v * 36 + idx;
        }
        return v;
    }

    private static byte[] SaltedHmac(string keySalt, string value)
    {
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(keySalt + "signer"));
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
    }

    private string HashValue(Usuario user, long timestamp)
    {
        var loginTimestamp = user.LastLogin is { } l
            ? new DateTime(l.Ticks - (l.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc)
                .ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
            : "";
        var raw = string.Concat(
            user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            user.PasswordHash,
            loginTimestamp,
            timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture),
            user.IsActive ? "True" : "False");
        return raw;
    }

    public string GerarToken(Usuario user)
    {
        var ts = DiasDesdeEpoch();
        var tsB36 = IntToBase36(ts);
        var hash = Convert.ToHexString(SaltedHmac(KeySalt, HashValue(user, ts))).ToLowerInvariant();
        // Django usa hexdigest()[::2] — letras/bytes pares.
        var slice = new string(hash.Where((_, i) => i % 2 == 0).ToArray());
        return $"{tsB36}-{slice}";
    }

    public bool ChecarToken(Usuario user, string token)
    {
        if (user is null || string.IsNullOrWhiteSpace(token)) return false;
        var parts = token.Split('-');
        if (parts.Length != 2) return false;
        long ts;
        try
        {
            ts = Base36ToInt(parts[0]);
        }
        catch (FormatException)
        {
            return false;
        }

        var agora = DiasDesdeEpoch();
        foreach (var alt in new[] { ts, ts - 1, ts - 2 })
        {
            if (alt < 0) continue;
            var esperado = GerarTokenParaTimestamp(user, alt);
            if (FixedTimeEquals(esperado, token)) return true;
        }
        if (agora - ts > 2) return false; // expirado
        return false;
    }

    private string GerarTokenParaTimestamp(Usuario user, long ts)
    {
        var tsB36 = IntToBase36(ts);
        var hash = Convert.ToHexString(SaltedHmac(KeySalt, HashValue(user, ts))).ToLowerInvariant();
        var slice = new string(hash.Where((_, i) => i % 2 == 0).ToArray());
        return $"{tsB36}-{slice}";
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(ba, bb);
    }

    // --- urlsafe_base64 (Django) ---

    public static string UrlsafeBase64Encode(int pk)
    {
        var bytes = Encoding.UTF8.GetBytes(pk.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static int? UrlsafeBase64Decode(string value)
    {
        try
        {
            var s = value.Replace('-', '+').Replace('_', '/');
            s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
            var bytes = Convert.FromBase64String(s);
            var text = Encoding.UTF8.GetString(bytes);
            if (!Regex.IsMatch(text, @"^\d+$")) return null;
            return int.Parse(text);
        }
        catch (Exception)
        {
            return null;
        }
    }
}