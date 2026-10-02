using System.Text.Json.Serialization;

namespace PainelFelizCasaNova.Api.Dtos;

/// <summary>Espelho do calcular_resumo_geral (chaves snake_case do DRF).</summary>
public class ResumoGeralOutput
{
    [JsonPropertyName("vgv")]
    public double Vgv { get; set; }

    [JsonPropertyName("vgc")]
    public double Vgc { get; set; }

    [JsonPropertyName("vendas_fechadas")]
    public int VendasFechadas { get; set; }

    [JsonPropertyName("vgv_linhas")]
    public double VgvLinhas { get; set; }

    [JsonPropertyName("vgc_linhas")]
    public double VgcLinhas { get; set; }

    [JsonPropertyName("vendas_linhas")]
    public int VendasLinhas { get; set; }

    [JsonPropertyName("manual_vgv")]
    public double ManualVgv { get; set; }

    [JsonPropertyName("manual_vgc")]
    public double ManualVgc { get; set; }

    [JsonPropertyName("manual_vendas")]
    public int ManualVendas { get; set; }

    [JsonPropertyName("regioes_resumo_manual")]
    public List<string> RegioesResumoManual { get; set; } = new();

    [JsonPropertyName("regioes_manual_ignorado")]
    public List<RegiaoManualIgnoradoOutput> RegioesManualIgnorado { get; set; } = new();
}

public class RegiaoManualIgnoradoOutput
{
    [JsonPropertyName("nome")]
    public string Nome { get; set; } = "";

    [JsonPropertyName("vendas_lancadas")]
    public int VendasLancadas { get; set; }
}

/// <summary>Resumo por região — {modo, vgv, vgc, vendas, ticket_medio, obs, vendas_lancadas, manual_ignorado}.</summary>
public class ResumoRegiaoOutput
{
    [JsonPropertyName("modo")]
    public string Modo { get; set; } = "linhas";

    [JsonPropertyName("vgv")]
    public double Vgv { get; set; }

    [JsonPropertyName("vgc")]
    public double Vgc { get; set; }

    [JsonPropertyName("vendas")]
    public int Vendas { get; set; }

    [JsonPropertyName("ticket_medio")]
    public double TicketMedio { get; set; }

    [JsonPropertyName("obs")]
    public string Obs { get; set; } = "";

    [JsonPropertyName("vendas_lancadas")]
    public int VendasLancadas { get; set; }

    [JsonPropertyName("manual_ignorado")]
    public bool ManualIgnorado { get; set; }
}

public class AlertaOutput
{
    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = "";

    [JsonPropertyName("sev")]
    public string Sev { get; set; } = "";

    [JsonPropertyName("titulo")]
    public string Titulo { get; set; } = "";

    [JsonPropertyName("detalhe")]
    public string Detalhe { get; set; } = "";

    [JsonPropertyName("venda_id")]
    public int VendaId { get; set; }
}