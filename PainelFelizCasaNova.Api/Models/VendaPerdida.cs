namespace PainelFelizCasaNova.Api.Models;

/// <summary>Espelho do model Django <c>VendaPerdida</c>.</summary>
public class VendaPerdida
{
    public int Id { get; set; }
    public int OrganizacaoId { get; set; }
    public Organizacao Organizacao { get; set; } = null!;

    public int? RegiaoId { get; set; }
    public Regiao? Regiao { get; set; }

    public string LegadoId { get; set; } = "";
    public string Mes { get; set; } = "";
    public int MesNum { get; set; }
    public int Ano { get; set; }
    public string Cliente { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Construtora { get; set; } = "";
    public string Produto { get; set; } = "";
    public string Corretor { get; set; } = "";
    public string Motivo { get; set; } = "";
    public string Financiado { get; set; } = "";
    public string Estrategia { get; set; } = "";

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}