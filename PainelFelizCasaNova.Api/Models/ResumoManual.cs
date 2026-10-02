namespace PainelFelizCasaNova.Api.Models;

/// <summary>Espelho do model Django <c>ResumoManual</c>.</summary>
public class ResumoManual
{
    public int Id { get; set; }
    public int RegiaoId { get; set; }
    public Regiao Regiao { get; set; } = null!;

    public decimal Vgv { get; set; }
    public decimal Vgc { get; set; }
    public int Vendas { get; set; }
    public string Obs { get; set; } = "";
    
    // Aliases para compatibilidade com controllers
    public decimal VgvManual { get => Vgv; set => Vgv = value; }
    public decimal VgcManual { get => Vgc; set => Vgc = value; }
}