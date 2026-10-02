using System.Globalization;
using System.Text.Json;

namespace PainelFelizCasaNova.Api.Services.Importacao;

public partial class DadosJsonImportService
{
    private static string? GetString(JsonElement el, string prop) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var p)
        && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static int? GetInt(JsonElement el, string prop) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var p)
        ? (p.ValueKind == JsonValueKind.Number ? (int?)p.GetInt32()
            : p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out var i) ? i : null)
        : null;

    private static bool GetBool(JsonElement el, string prop) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var p)
        && p.ValueKind == JsonValueKind.True;

    private static List<string>? GetStringList(JsonElement el, string prop)
    {
        if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty(prop, out var p)
            || p.ValueKind != JsonValueKind.Array) return null;
        return p.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
    }

    private static decimal GetDecimal(JsonElement el, string prop) =>
        GetDecimalNullable(el, prop) ?? 0m;

    private static decimal? GetDecimalNullable(JsonElement el, string prop)
    {
        if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty(prop, out var p)) return null;
        if (p.ValueKind == JsonValueKind.Number) return p.GetDecimal();
        if (p.ValueKind == JsonValueKind.String &&
            decimal.TryParse(p.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
            return d;
        return null;
    }

    private static DateOnly? ParseDate(string? s) =>
        s is null ? null : (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var d) ? d : null);
}