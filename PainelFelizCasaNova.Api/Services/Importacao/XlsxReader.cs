using ClosedXML.Excel;

namespace PainelFelizCasaNova.Api.Services.Importacao;

/// <summary>Leitura de planilhas .xlsx — equivalente ao openpyxl (read_only + data_only).</summary>
public static class XlsxReader
{
    public static List<(string Nome, List<List<object?>> Linhas)> LerXlsxBytes(byte[] raw)
    {
        using var stream = new MemoryStream(raw);
        using var wb = new XLWorkbook(stream);
        var folhas = new List<(string, List<List<object?>>)>();

        foreach (var ws in wb.Worksheets)
        {
            var linhas = new List<List<object?>>();
            var range = ws.RangeUsed();
            if (range is null) continue;

            foreach (var row in range.Rows())
            {
                var lista = new List<object?>();
                foreach (var cell in row.Cells())
                {
                    lista.Add(ValorCelula(cell));
                }
                if (lista.All(c => c is null || string.IsNullOrEmpty(c.ToString()?.Trim())))
                    continue;
                linhas.Add(lista);
            }
            if (linhas.Count > 0) folhas.Add((ws.Name, linhas));
        }
        return folhas;
    }

    private static object? ValorCelula(IXLCell cell)
    {
        var value = cell.HasFormula ? cell.CachedValue : cell.Value;
        if (value.IsBlank) return null;
        if (value.IsDateTime) return value.GetDateTime();
        if (value.IsNumber) return (double)value.GetNumber();
        if (value.IsBoolean) return value.GetBoolean();
        if (value.IsText) return value.GetText();
        if (value.IsError) return value.GetError().ToString();
        return value.ToString();
    }
}