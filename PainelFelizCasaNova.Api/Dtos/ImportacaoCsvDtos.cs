using System.Text.Json.Serialization;

namespace PainelFelizCasaNova.Api.Dtos;

public class ImportacaoCsvInput
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("legado_id")]
    public string? LegadoId { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("regiao")]
    public int? Regiao { get; set; }

    [JsonPropertyName("empreendimento")]
    public int? Empreendimento { get; set; }

    [JsonPropertyName("regiao_nome")]
    public string? RegiaoNome { get; set; }

    [JsonPropertyName("auto")]
    public bool Auto { get; set; }

    [JsonPropertyName("mes_ref")]
    public string? MesRef { get; set; }

    [JsonPropertyName("mesRef")]
    public string? MesRefCamel { get; set; }

    [JsonPropertyName("ultima_busca")]
    public DateTime? UltimaBusca { get; set; }

    [JsonPropertyName("ultimaBusca")]
    public DateTime? UltimaBuscaCamel { get; set; }

    public int? RegiaoResolvido => Regiao ?? Empreendimento;
    public string? MesRefResolvido => MesRef ?? MesRefCamel;
    public DateTime? UltimaBuscaResolvida => UltimaBusca ?? UltimaBuscaCamel;
}

public class ImportacaoCsvOutput
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("legado_id")]
    public string LegadoId { get; set; } = "";

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("regiao")]
    public int? Regiao { get; set; }

    [JsonPropertyName("empreendimento")]
    public int? Empreendimento { get; set; }

    [JsonPropertyName("regiao_nome")]
    public string RegiaoNome { get; set; } = "";

    [JsonPropertyName("auto")]
    public bool Auto { get; set; }

    [JsonPropertyName("mes_ref")]
    public string? MesRef { get; set; }

    [JsonPropertyName("ultima_busca")]
    public DateTime? UltimaBusca { get; set; }
}

/// <summary>Resultado de operações de importação (novas/atualizadas/conflitos/etc).</summary>
public class ImportacaoResult
{
    [JsonPropertyName("novas")]
    public int Novas { get; set; }

    [JsonPropertyName("atualizadas")]
    public int Atualizadas { get; set; }

    [JsonPropertyName("conflitos")]
    public int Conflitos { get; set; }

    [JsonPropertyName("erro")]
    public string? Erro { get; set; }

    [JsonPropertyName("linhas")]
    public int Linhas { get; set; }

    [JsonPropertyName("regioes")]
    public List<string>? Regioes { get; set; }

    [JsonPropertyName("sem_regiao")]
    public int SemRegiao { get; set; }

    [JsonPropertyName("perdidas_novas")]
    public int PerdidasNovas { get; set; }

    [JsonPropertyName("perdidas_atualizadas")]
    public int PerdidasAtualizadas { get; set; }
}