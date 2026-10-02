using System.Text;
using System.Text.RegularExpressions;
using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Services.Importacao;

public static partial class CsvHelpers
{
    public static List<VendaRow> TabelaParaVendas(List<List<object?>> linhas, Regiao? regiao, string? mesRef)
    {
        if (linhas.Count == 0) return new List<VendaRow>();

        Dictionary<string, int>? mapa = null;
        var inicio = 0;
        for (var i = 0; i < linhas.Count; i++)
        {
            var encontrado = MapaColunas(linhas[i].Select(CelulaParaTexto).ToList());
            if (encontrado is not null)
            {
                mapa = encontrado;
                inicio = i + 1;
                break;
            }
        }

        var saida = new List<VendaRow>();
        var fonte = mapa is not null ? linhas.Skip(inicio).ToList() : linhas;

        foreach (var cols in fonte)
        {
            if (!cols.Any(c => !string.IsNullOrEmpty(CelulaParaTexto(c)))) continue;
            if (LinhaEhCabecalho(cols)) continue;
            if (mapa is null && Regex.IsMatch(
                    string.Join(" ", cols.Select(CelulaParaTexto)),
                    @"assinatura|cliente", RegexOptions.IgnoreCase))
                continue;

            var row = LinhaParaVenda(cols, mapa, regiao, mesRef);
            if (row is null) continue;
            var clienteNorm = NormalizarCabecalho(row.Cliente);
            if (clienteNorm is "cliente" or "nome") continue;
            saida.Add(row);
        }
        return saida;
    }

    public static List<VendaRow> ParseCsvParaVendas(string text, Regiao regiao, string? mesRefFonte)
    {
        var linhas = new List<List<object?>>();
        foreach (var line in text.Split('\n'))
        {
            var l = line.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(l)) continue;
            linhas.Add(ParseCsvLine(l).Cast<object?>().ToList());
        }
        return TabelaParaVendas(linhas, regiao, mesRefFonte);
    }

    public static List<List<object?>> LerCsvBytes(byte[] raw)
    {
        string text;
        var utf8Strict = new UTF8Encoding(false, true);
        try { text = utf8Strict.GetString(raw); }
        catch (DecoderFallbackException)
        {
            try { text = Encoding.GetEncoding("Windows-1252").GetString(raw); }
            catch (Exception) { text = Encoding.Latin1.GetString(raw); }
        }
        text = text.TrimStart('\uFEFF');

        var amostra = text.Length > 2000 ? text[..2000] : text;
        var delim = amostra.Count(c => c == ';') > amostra.Count(c => c == ',') ? ';' : ',';

        var linhas = new List<List<object?>>();
        foreach (var line in text.Split('\n'))
        {
            var l = line.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(l)) continue;
            if (delim == ',')
                linhas.Add(ParseCsvLine(l).Cast<object?>().ToList());
            else
                linhas.Add(l.Split(';').Select(c => (object?)c.Trim().Trim('"')).ToList());
        }
        return linhas;
    }

    public static string Slugificar(string nome)
    {
        var s = RemoverAcentos((nome ?? "").Trim().ToLowerInvariant());
        s = Regex.Replace(s, @"[^a-z0-9\s_-]", "");
        s = Regex.Replace(s, @"[\s_-]+", "-").Trim('-');
        return string.IsNullOrEmpty(s) ? "regiao" : s;
    }
}