using System.Text.Json.Serialization;

namespace PainelFelizCasaNova.Api.Dtos;

public class VendaPerdidaInput
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("legado_id")]
    public string? LegadoId { get; set; }

    [JsonPropertyName("mes")]
    public string? Mes { get; set; }

    [JsonPropertyName("mesNum")]
    public int? MesNum { get; set; }

    [JsonPropertyName("mes_num")]
    public int? MesNumSnake { get; set; }

    [JsonPropertyName("ano")]
    public int Ano { get; set; }

    [JsonPropertyName("cliente")]
    public string? Cliente { get; set; }

    [JsonPropertyName("telefone")]
    public string? Telefone { get; set; }

    [JsonPropertyName("construtora")]
    public string? Construtora { get; set; }

    [JsonPropertyName("empreendimento")]
    public string? Empreendimento { get; set; }

    [JsonPropertyName("produto")]
    public string? Produto { get; set; }

    [JsonPropertyName("corretor")]
    public string? Corretor { get; set; }

    [JsonPropertyName("motivo")]
    public string? Motivo { get; set; }

    [JsonPropertyName("financiado")]
    public string? Financiado { get; set; }

    [JsonPropertyName("estrategia")]
    public string? Estrategia { get; set; }

    public int MesNumResolvido => MesNum ?? MesNumSnake ?? 0;
    public string ConstrutoraResolvida => Construtora ?? Empreendimento ?? "";
}

public class VendaPerdidaOutput
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("legado_id")]
    public string LegadoId { get; set; } = "";

    [JsonPropertyName("mes")]
    public string Mes { get; set; } = "";

    [JsonPropertyName("mesNum")]
    public int MesNum { get; set; }

    [JsonPropertyName("mes_num")]
    public int MesNumSnake { get; set; }

    [JsonPropertyName("ano")]
    public int Ano { get; set; }

    [JsonPropertyName("cliente")]
    public string Cliente { get; set; } = "";

    [JsonPropertyName("telefone")]
    public string Telefone { get; set; } = "";

    [JsonPropertyName("construtora")]
    public string Construtora { get; set; } = "";

    [JsonPropertyName("empreendimento")]
    public string Empreendimento { get; set; } = "";

    [JsonPropertyName("produto")]
    public string Produto { get; set; } = "";

    [JsonPropertyName("corretor")]
    public string Corretor { get; set; } = "";

    [JsonPropertyName("motivo")]
    public string Motivo { get; set; } = "";

    [JsonPropertyName("financiado")]
    public string Financiado { get; set; } = "";

    [JsonPropertyName("estrategia")]
    public string Estrategia { get; set; } = "";
}