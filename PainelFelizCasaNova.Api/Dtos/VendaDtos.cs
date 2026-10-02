using System.Text.Json.Serialization;

namespace PainelFelizCasaNova.Api.Dtos;

/// <summary>Entrada de venda — aceita os aliases do serializer DRF (regiao_id/empreendimento_id,
/// dataAssinatura/data_assinatura, blocoQuadra/bloco_quadra, empreendimento/construtora).</summary>
public class VendaInput
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("legado_id")]
    public string? LegadoId { get; set; }

    [JsonPropertyName("regiao_id")]
    public int? RegiaoId { get; set; }

    [JsonPropertyName("empreendimento_id")]
    public int? EmpreendimentoId { get; set; }

    [JsonPropertyName("regiao")]
    public string? Regiao { get; set; }

    [JsonPropertyName("numero")]
    public string? Numero { get; set; }

    [JsonPropertyName("dataAssinatura")]
    public DateOnly? DataAssinatura { get; set; }

    [JsonPropertyName("data_assinatura")]
    public DateOnly? DataAssinaturaSnake { get; set; }

    [JsonPropertyName("dataEntrada")]
    public DateOnly? DataEntrada { get; set; }

    [JsonPropertyName("data_entrada")]
    public DateOnly? DataEntradaSnake { get; set; }

    [JsonPropertyName("mes")]
    public string? Mes { get; set; }

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

    [JsonPropertyName("blocoQuadra")]
    public string? BlocoQuadra { get; set; }

    [JsonPropertyName("bloco_quadra")]
    public string? BlocoQuadraSnake { get; set; }

    [JsonPropertyName("unidade")]
    public string? Unidade { get; set; }

    [JsonPropertyName("valor")]
    public decimal? Valor { get; set; }

    [JsonPropertyName("percentual")]
    public decimal? Percentual { get; set; }

    [JsonPropertyName("comissao")]
    public decimal? Comissao { get; set; }

    [JsonPropertyName("corretor")]
    public string? Corretor { get; set; }

    [JsonPropertyName("origem")]
    public string? Origem { get; set; }

    [JsonPropertyName("financiado")]
    public string? Financiado { get; set; }

    [JsonPropertyName("exemplo")]
    public bool? Exemplo { get; set; }

    /// <summary>Região resolvida (prioridade regiao_id; senão empreendimento_id).</summary>
    public int? RegiaoIdResolvido => RegiaoId ?? EmpreendimentoId;
    public DateOnly? DataAssinaturaResolvida => DataAssinatura ?? DataAssinaturaSnake;
    public DateOnly? DataEntradaResolvida => DataEntrada ?? DataEntradaSnake;
    public string? BlocoQuadraResolvido => BlocoQuadra ?? BlocoQuadraSnake;
    public string? ConstrutoraResolvida => Construtora ?? Empreendimento;
}

/// <summary>Saída de venda — espelho do VendaSerializer.to_representation (misto snake/camel).</summary>
public class VendaOutput
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("legado_id")]
    public string LegadoId { get; set; } = "";

    [JsonPropertyName("regiao_id")]
    public int? RegiaoId { get; set; }

    [JsonPropertyName("regiao")]
    public string Regiao { get; set; } = "";

    [JsonPropertyName("regiao_nome")]
    public string RegiaoNome { get; set; } = "";

    [JsonPropertyName("numero")]
    public string Numero { get; set; } = "";

    [JsonPropertyName("dataAssinatura")]
    public DateOnly? DataAssinatura { get; set; }

    [JsonPropertyName("dataEntrada")]
    public DateOnly? DataEntrada { get; set; }

    [JsonPropertyName("mes")]
    public string? Mes { get; set; }

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

    [JsonPropertyName("blocoQuadra")]
    public string BlocoQuadra { get; set; } = "";

    [JsonPropertyName("bloco_quadra")]
    public string BlocoQuadraSnake { get; set; } = "";

    [JsonPropertyName("unidade")]
    public string Unidade { get; set; } = "";

    [JsonPropertyName("valor")]
    public decimal Valor { get; set; }

    [JsonPropertyName("percentual")]
    public decimal? Percentual { get; set; }

    [JsonPropertyName("comissao")]
    public decimal Comissao { get; set; }

    [JsonPropertyName("corretor")]
    public string Corretor { get; set; } = "";

    [JsonPropertyName("origem")]
    public string Origem { get; set; } = "";

    [JsonPropertyName("financiado")]
    public string Financiado { get; set; } = "";

    [JsonPropertyName("exemplo")]
    public bool Exemplo { get; set; }
}