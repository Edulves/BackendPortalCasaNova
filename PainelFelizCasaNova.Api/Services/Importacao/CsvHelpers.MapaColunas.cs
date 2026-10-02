using System.Globalization;
using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Services.Importacao;

public static partial class CsvHelpers
{
    private static readonly Dictionary<string, string[]> AliasesColuna = new()
    {
        ["numero"] = new[] { "n", "no", "nº", "numero", "número", "#" },
        ["data_assinatura"] = new[] { "assinatura", "data assinatura", "data de assinatura", "dt assinatura" },
        ["cliente"] = new[] { "cliente", "nome do cliente", "nome" },
        ["telefone"] = new[] { "telefone", "tel", "fone", "celular", "whatsapp" },
        ["construtora"] = new[] { "empreendimento", "construtora", "incorporadora" },
        ["produto"] = new[] { "produto", "empreendimento produto" },
        ["bloco_quadra"] = new[] { "bl./qd.", "bl/qd", "bl. qd.", "bl qd", "bloco", "quadra", "bloco/quadra", "bloco quadra" },
        ["unidade"] = new[] { "unidade", "un", "apto", "apartamento" },
        ["valor"] = new[] { "valor", "vgv", "valor venda" },
        ["percentual"] = new[] { "%", "percentual", "% comissao", "% comissão", "perc" },
        ["comissao"] = new[] { "comissao", "comissão", "vgc", "valor comissao", "valor comissão" },
        ["corretor"] = new[] { "corretor", "consultor" },
        ["origem"] = new[] { "origem", "canal" },
        ["financiado"] = new[] { "financiado", "financiamento", "financiado ?" },
        ["regiao"] = new[] { "regiao", "região", "polo", "cidade" },
        ["mes"] = new[] { "mes", "mês", "mes ref", "mês ref", "referencia", "mes da desistencia" },
        ["motivo"] = new[] { "motivo", "motivo da perda" },
    };

    public static Dictionary<string, int>? MapaColunas(IReadOnlyList<string> cabecalho)
    {
        var norm = cabecalho.Select(NormalizarCabecalho).ToList();
        var mapa = new Dictionary<string, int>();
        foreach (var (campo, aliases) in AliasesColuna)
        {
            var chaves = aliases.Select(a => NormalizarCabecalho(a)).ToHashSet();
            for (var i = 0; i < norm.Count; i++)
            {
                if (chaves.Contains(norm[i]))
                {
                    mapa[campo] = i;
                    break;
                }
            }
        }
        return mapa.ContainsKey("cliente") ? mapa : null;
    }

    public static string Pegar(IReadOnlyList<object?> cols, Dictionary<string, int>? mapa, string campo, int pos)
    {
        if (mapa is not null && mapa.TryGetValue(campo, out var i))
            return i < cols.Count ? CelulaParaTexto(cols[i]) : "";
        if (mapa is null && 0 <= pos && pos < cols.Count)
            return CelulaParaTexto(cols[pos]);
        return "";
    }

    public static VendaRow? LinhaParaVenda(IReadOnlyList<object?> cols, Dictionary<string, int>? mapa,
        Regiao? regiao, string? mesRef)
    {
        if (mapa is null && cols.Count(c => !string.IsNullOrEmpty(CelulaParaTexto(c))) < 9)
            return null;

        var dataIso = BrDateToIso(Pegar(cols, mapa, "data_assinatura", 1));
        var mesCelula = Pegar(cols, mapa, "mes", -1);
        var mes = dataIso is { } d
            ? d.ToString("yyyy-MM")
            : InferMesRef(mesCelula, null) ?? mesRef;

        var rawValor = Pegar(cols, mapa, "valor", 8);
        var rawComissao = Pegar(cols, mapa, "comissao", 10);
        var rawPerc = Pegar(cols, mapa, "percentual", 9);
        var cliente = TitleCase(Pegar(cols, mapa, "cliente", 2));
        if (string.IsNullOrEmpty(cliente)) return null;

        decimal? perc = null;
        if (!string.IsNullOrEmpty(rawPerc))
        {
            var normPerc = rawPerc.Replace("%", "").Replace(",", ".").Trim();
            if (decimal.TryParse(normPerc, NumberStyles.Number, CultureInfo.InvariantCulture, out var p))
            {
                if (p != 0 && Math.Abs(p) <= 1) p *= 100;
                perc = p;
            }
        }

        return new VendaRow
        {
            Regiao = regiao,
            RegiaoNome = Pegar(cols, mapa, "regiao", -1),
            Numero = Pegar(cols, mapa, "numero", 0),
            DataAssinatura = dataIso,
            DataEntrada = null,
            Mes = mes,
            Cliente = cliente,
            Telefone = Pegar(cols, mapa, "telefone", 3),
            Construtora = Pegar(cols, mapa, "construtora", 4).ToUpperInvariant(),
            Produto = TitleCase(Pegar(cols, mapa, "produto", 5)),
            BlocoQuadra = Pegar(cols, mapa, "bloco_quadra", 6),
            Unidade = Pegar(cols, mapa, "unidade", 7),
            Valor = !string.IsNullOrEmpty(rawValor) ? BrMoneyToNumber(rawValor) : null,
            Percentual = perc,
            Comissao = !string.IsNullOrEmpty(rawComissao) ? BrMoneyToNumber(rawComissao) : null,
            Corretor = TitleCase(Pegar(cols, mapa, "corretor", 11)),
            Origem = TitleCase(Pegar(cols, mapa, "origem", 12)),
            Financiado = TitleCase(Pegar(cols, mapa, "financiado", 13)),
        };
    }

    private static bool LinhaEhCabecalho(IReadOnlyList<object?> cols) =>
        MapaColunas(cols.Select(CelulaParaTexto).ToList()) is not null;
}