using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PainelFelizCasaNova.Api.Services.Importacao;

public static partial class CsvHelpers
{
    private static readonly Regex TitleCaseRe = new(@"(^|[\s/])([a-zà-ú])");

    public static string TitleCase(string? s)
    {
        if (string.IsNullOrEmpty(s)) return s ?? "";
        var lower = s.ToLowerInvariant();
        return TitleCaseRe.Replace(lower, m => m.Groups[1].Value + m.Groups[2].Value.ToUpperInvariant());
    }

    public static string? InferMesRef(string? label, string? mesRef = null)
    {
        if (mesRef is not null && Regex.IsMatch(mesRef, @"^\d{4}-\d{2}$")) return mesRef;
        label ??= "";

        var m = Regex.Match(label, @"(20\d{2})-(\d{2})");
        if (m.Success) return $"{m.Groups[1].Value}-{m.Groups[2].Value}";

        m = Regex.Match(label, @"(\d{1,2})/(20\d{2})");
        if (m.Success) return $"{m.Groups[2].Value}-{int.Parse(m.Groups[1].Value):00}";

        m = Regex.Match(label,
            @"(janeiro|fevereiro|marco|abril|maio|junho|julho|agosto|setembro|outubro|novembro|dezembro)\s*/?\s*(20\d{2})",
            RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var key = RemoverAcentos(m.Groups[1].Value.ToLowerInvariant());
            if (MesesNomeNum.TryGetValue(key, out var n))
                return $"{m.Groups[2].Value}-{n:00}";
        }
        return null;
    }

    public static string RemoverAcentos(string s)
    {
        var formD = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString();
    }

    public static string NormalizarCabecalho(string? valor)
    {
        var s = RemoverAcentos((valor ?? "").Trim().ToLowerInvariant());
        s = s.Replace("º", "o").Replace("°", "");
        s = Regex.Replace(s, @"[^a-z0-9% ]+", " ");
        s = Regex.Replace(s, @"\s+", " ").Trim();
        return s;
    }

    public static string CelulaParaTexto(object? val)
    {
        return val switch
        {
            null => "",
            DateTime dt => dt.ToString("dd/MM/yyyy"),
            DateTimeOffset dto => dto.DateTime.ToString("dd/MM/yyyy"),
            DateOnly d => d.ToString("dd/MM/yyyy"),
            decimal dec => dec.ToString(CultureInfo.InvariantCulture),
            double dbl when Math.Abs(dbl) < 1e12 && dbl == Math.Truncate(dbl)
                => Convert.ToInt64(dbl).ToString(CultureInfo.InvariantCulture),
            float flt when Math.Abs(flt) < 1e12 && flt == Math.Truncate(flt)
                => Convert.ToInt64(flt).ToString(CultureInfo.InvariantCulture),
            _ => (val.ToString() ?? "").Trim(),
        };
    }

    public static int? AnoDoNome(string nome)
    {
        var m = Regex.Match(nome ?? "", @"(20\d{2})");
        return m.Success ? int.Parse(m.Groups[1].Value) : null;
    }

    public static string? MesDaFolha(string nome, int? ano)
    {
        var mes = InferMesRef(nome, null);
        if (mes is not null) return mes;
        var chave = NormalizarCabecalho(nome).Split(' ')[0];
        if (MesesNomeNum.TryGetValue(chave, out var n) && ano is not null && ano.Value > 0)
            return $"{ano.Value}-{n:00}";
        return null;
    }

    public static bool FolhaResumo(string nome) =>
        NormalizarCabecalho(nome).StartsWith("resumo");

    public static bool FolhaPerdidas(string nome) =>
        NormalizarCabecalho(nome).Contains("perdid");

    public static bool ValorPreenchido(object? val) =>
        val is not null && !(val is string s && s == "");
}