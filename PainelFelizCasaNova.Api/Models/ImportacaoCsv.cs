namespace PainelFelizCasaNova.Api.Models;

/// <summary>Espelho do model Django <c>ImportacaoCSV</c>.</summary>
public class ImportacaoCsv
{
    public int Id { get; set; }
    public int OrganizacaoId { get; set; }
    public Organizacao Organizacao { get; set; } = null!;

    public string LegadoId { get; set; } = "";
    public string Label { get; set; } = "";
    public string Url { get; set; } = "";

    public int? RegiaoId { get; set; }
    public Regiao? Regiao { get; set; }
    public string RegiaoNome { get; set; } = "";

    public bool Auto { get; set; }

    /// <summary>Formato YYYY-MM.</summary>
    public string? MesRef { get; set; }

    public DateTime? UltimaBusca { get; set; }

    // Status de execução do import
    public string Status { get; set; } = "pendente"; // pendente, executando, concluído, erro
    public int VendasProcessadas { get; set; }
    public int VendasMergidas { get; set; }
    public int VendasPerdidas { get; set; }
    public string? ErroMsg { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}