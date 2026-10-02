using System.Text.Json.Serialization;
using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Dtos;

public class ResumoManualInput
{
    [JsonPropertyName("vgv")]
    public decimal Vgv { get; set; }

    [JsonPropertyName("vgc")]
    public decimal Vgc { get; set; }

    [JsonPropertyName("vendas")]
    public int Vendas { get; set; }

    [JsonPropertyName("obs")]
    public string? Obs { get; set; }
}

public class ResumoManualOutput
{
    [JsonPropertyName("vgv")]
    public decimal Vgv { get; set; }

    [JsonPropertyName("vgc")]
    public decimal Vgc { get; set; }

    [JsonPropertyName("vendas")]
    public int Vendas { get; set; }

    [JsonPropertyName("obs")]
    public string Obs { get; set; } = "";

    public static ResumoManualOutput De(ResumoManual? rm) => rm is null
        ? new ResumoManualOutput()
        : new ResumoManualOutput { Vgv = rm.Vgv, Vgc = rm.Vgc, Vendas = rm.Vendas, Obs = rm.Obs };
}

public class RegiaoInput
{
    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("nome")]
    public string? Nome { get; set; }

    [JsonPropertyName("resumo_manual")]
    public ResumoManualInput? ResumoManual { get; set; }
}

/// <summary>Espelho do RegiaoSerializer — {id, slug, nome, resumo_manual}.</summary>
public class RegiaoOutput
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = "";

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = "";

    [JsonPropertyName("resumo_manual")]
    public ResumoManualOutput? ResumoManual { get; set; }
}