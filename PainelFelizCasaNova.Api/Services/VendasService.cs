using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Services;

/// <summary>Espelho do <c>core/services/vendas.py</c> — VGV/VGC e filtros.</summary>
public class VendasService
{
    public static IQueryable<Venda> QsVendasOrg(IQueryable<Venda> vendas, int organizacaoId) =>
        vendas.Where(v => v.OrganizacaoId == organizacaoId);

    public static IQueryable<Venda> QsNaoExemplo(IQueryable<Venda> qs) =>
        qs.Where(v => !v.Exemplo);

    public static IQueryable<Venda> QsFechadas(IQueryable<Venda> qs) =>
        QsNaoExemplo(qs).Where(v => v.DataAssinatura != null);

    public static decimal SumVgv(IEnumerable<Venda> qs) => qs.Sum(v => v.Valor);

    public static decimal SumVgc(IEnumerable<Venda> qs) => qs.Sum(v => v.Comissao);

    /// <summary>Resumo manual só conta se a aba ainda não tem vendas lançadas (não-exemplo).</summary>
    public static bool UsarResumoManual(Regiao regiao, int vendasNaoExemplo) =>
        regiao.ResumoManual is not null && vendasNaoExemplo == 0;

    public static string? MesRefDaVenda(Venda venda, string? mesRefFonte = null)
    {
        if (!string.IsNullOrEmpty(venda.Mes) && venda.Mes.Length == 7) return venda.Mes;
        if (venda.DataAssinatura is { } ass) return ass.ToString("yyyy-MM");
        if (venda.DataEntrada is { } ent) return ent.ToString("yyyy-MM");
        return mesRefFonte;
    }
}