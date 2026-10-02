using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PainelFelizCasaNova.Api.Services.Importacao;

/// <summary>Helpers de CSV — porta de <c>core/services/csv_import.py</c>.</summary>
public static partial class CsvHelpers
{
    public static readonly string[] CsvHostAllow = { "docs.google.com", "spreadsheets.google.com" };

    public static readonly Dictionary<string, int> MesesNomeNum = new()
    {
        ["janeiro"] = 1, ["fevereiro"] = 2, ["marco"] = 3, ["abril"] = 4, ["maio"] = 5,
        ["junho"] = 6, ["julho"] = 7, ["agosto"] = 8, ["setembro"] = 9, ["outubro"] = 10,
        ["novembro"] = 11, ["dezembro"] = 12,
    };

    public static (bool Ok, string Erro) UrlCsvPermitida(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return (false, "URL inválida.");
        if (u.Scheme != "https") return (false, "Use um link https:// (Google Sheets publicado).");
        var host = (u.Host ?? "").ToLowerInvariant();
        var ok = CsvHostAllow.Any(h => host == h || host.EndsWith("." + h, StringComparison.Ordinal));
        if (!ok) return (false, "Host não permitido. Use docs.google.com ou spreadsheets.google.com.");
        return (true, "");
    }

    private static readonly Regex BrDateRe = new(@"^(\d{1,2})/(\d{1,2})/(\d{4})$");

    public static DateOnly? BrDateToIso(string? s)
    {
        s = (s ?? "").Trim();
        var m = BrDateRe.Match(s);
        if (!m.Success) return null;
        var dia = int.Parse(m.Groups[1].Value);
        var mes = int.Parse(m.Groups[2].Value);
        var ano = int.Parse(m.Groups[3].Value);
        try { return new DateOnly(ano, mes, dia); }
        catch (ArgumentOutOfRangeException) { return null; }
    }

    public static decimal? BrMoneyToNumber(string? s)
    {
        s = Regex.Replace(s ?? "", @"[R$\s]", "");
        if (string.IsNullOrEmpty(s)) return null;
        var hasComma = s.Contains(',');
        var hasDot = s.Contains('.');
        if (hasComma && hasDot)
        {
            s = s.Replace(".", "").Replace(",", ".");
        }
        else if (hasComma && !hasDot)
        {
            s = s.Replace(",", ".");
        }
        else if (hasDot && !hasComma)
        {
            var parts = s.Split('.');
            if (parts.Length > 2 || (parts.Length == 2 && parts[1].Length == 3))
                s = s.Replace(".", "");
        }
        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    public static List<string> ParseCsvLine(string line)
    {
        var lista = new List<string>();
        var cur = new StringBuilder();
        var inQuote = false;
        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuote = !inQuote;
                continue;
            }
            if (c == ',' && !inQuote)
            {
                lista.Add(cur.ToString());
                cur.Clear();
                continue;
            }
            cur.Append(c);
        }
        lista.Add(cur.ToString());
        return lista;
    }

    public static bool TextoPareceCsvVendas(string? text)
    {
        var lines = (text ?? "").Split('\n')
            .Select(l => l.TrimEnd('\r')).Where(l => l.Trim().Length > 0).ToList();
        if (lines.Count == 0) return false;
        if (Regex.IsMatch(lines[0], @"assinatura|cliente", RegexOptions.IgnoreCase))
            return lines.Count >= 2;
        return ParseCsvLine(lines[0]).Count >= 9;
    }
}