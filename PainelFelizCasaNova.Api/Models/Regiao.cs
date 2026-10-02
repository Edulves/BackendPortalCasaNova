namespace PainelFelizCasaNova.Api.Models;

/// <summary>'Aba/polo (ex.: Lauro de Freitas) — equivale a regioes[] do JSON legado.'</summary>
public class Regiao
{
    public int Id { get; set; }
    public int OrganizacaoId { get; set; }
    public Organizacao Organizacao { get; set; } = null!;
    public string Slug { get; set; } = "";
    public string Nome { get; set; } = "";

    public ResumoManual? ResumoManual { get; set; }
    public List<Venda> Vendas { get; set; } = new();
    public List<VendaPerdida> VendasPerdidas { get; set; } = new();
    public List<ImportacaoCsv> FontesCsv { get; set; } = new();
}