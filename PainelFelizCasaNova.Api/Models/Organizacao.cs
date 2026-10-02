namespace PainelFelizCasaNova.Api.Models;

/// <summary>Espelho do model Django <c>Organizacao</c>.</summary>
public class Organizacao
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Slug { get; set; } = "";
    public DateTime CriadoEm { get; set; }

    public OrganizacaoConfig? Config { get; set; }
    public List<Usuario> Usuarios { get; set; } = new();
    public List<Regiao> Regioes { get; set; } = new();
    public List<Venda> Vendas { get; set; } = new();
    public List<VendaPerdida> VendasPerdidas { get; set; } = new();
    public List<ImportacaoCsv> FontesCsv { get; set; } = new();
}