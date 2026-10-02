using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Services.Importacao;

/// <summary>Linha de venda parseada de CSV/planilha — espelho do dict do Python.</summary>
public class VendaRow
{
    public Regiao? Regiao { get; set; }
    public string RegiaoNome { get; set; } = "";
    public string Numero { get; set; } = "";
    public DateOnly? DataAssinatura { get; set; }
    public DateOnly? DataEntrada { get; set; }
    public string? Mes { get; set; }
    public string Cliente { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Construtora { get; set; } = "";
    public string Produto { get; set; } = "";
    public string BlocoQuadra { get; set; } = "";
    public string Unidade { get; set; } = "";
    public decimal? Valor { get; set; }
    public decimal? Percentual { get; set; }
    public decimal? Comissao { get; set; }
    public string Corretor { get; set; } = "";
    public string Origem { get; set; } = "";
    public string Financiado { get; set; } = "";
}