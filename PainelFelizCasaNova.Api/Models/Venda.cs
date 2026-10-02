namespace PainelFelizCasaNova.Api.Models;

/// <summary>Espelho do model Django <c>Venda</c>.</summary>
public class Venda
{
    public int Id { get; set; }
    public int OrganizacaoId { get; set; }
    public Organizacao Organizacao { get; set; } = null!;

    public int? RegiaoId { get; set; }
    public Regiao? Regiao { get; set; }

    public int? ImportacaoCsvId { get; set; }
    public ImportacaoCsv? ImportacaoCsv { get; set; }

    public string LegadoId { get; set; } = "";
    public string Numero { get; set; } = "";
    public DateOnly? DataAssinatura { get; set; }
    public DateOnly? DataEntrada { get; set; }

    /// <summary>Formato YYYY-MM.</summary>
    public string? Mes { get; set; }

    public string Cliente { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Construtora { get; set; } = "";
    public string Produto { get; set; } = "";
    public string BlocoQuadra { get; set; } = "";
    public string Unidade { get; set; } = "";
    public decimal Valor { get; set; }
    public decimal? Percentual { get; set; }
    public decimal Comissao { get; set; }
    public string Corretor { get; set; } = "";
    public string Origem { get; set; } = "";
    public string Financiado { get; set; } = "";
    public bool Exemplo { get; set; }

    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}