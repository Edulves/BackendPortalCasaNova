using System.Text.Json.Serialization;

namespace PainelFelizCasaNova.Api.Dtos;

public class OrganizacaoConfigInput
{
    [JsonPropertyName("alerta_dias_sem_assinatura")]
    public int? AlertaDiasSemAssinatura { get; set; }

    [JsonPropertyName("alertaDiasSemAssinatura")]
    public int? AlertaDiasSemAssinaturaCamel { get; set; }

    [JsonPropertyName("campos_obrigatorios")]
    public List<string>? CamposObrigatorios { get; set; }

    [JsonPropertyName("camposObrigatorios")]
    public List<string>? CamposObrigatoriosCamel { get; set; }

    [JsonPropertyName("logo_data_url")]
    public string? LogoDataUrl { get; set; }

    [JsonPropertyName("logoDataUrl")]
    public string? LogoDataUrlCamel { get; set; }

    public int? AlertaDiasResolvido => AlertaDiasSemAssinatura ?? AlertaDiasSemAssinaturaCamel;
    public List<string>? CamposResolvidos => CamposObrigatorios ?? CamposObrigatoriosCamel;
    public string? LogoResolvido => LogoDataUrl ?? LogoDataUrlCamel;
}

/// <summary>Espelho do OrganizacaoConfigSerializer (resposta do /api/config/).</summary>
public class OrganizacaoConfigOutput
{
    [JsonPropertyName("alerta_dias_sem_assinatura")]
    public int AlertaDiasSemAssinatura { get; set; }

    [JsonPropertyName("campos_obrigatorios")]
    public List<string> CamposObrigatorios { get; set; } = new();

    [JsonPropertyName("logo_data_url")]
    public string? LogoDataUrl { get; set; }

    [JsonPropertyName("atualizado_em")]
    public DateTime? AtualizadoEm { get; set; }
}