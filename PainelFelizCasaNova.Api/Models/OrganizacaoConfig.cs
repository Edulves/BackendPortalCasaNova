using System.Text.Json;

namespace PainelFelizCasaNova.Api.Models;

/// <summary>Espelho do model Django <c>OrganizacaoConfig</c>.</summary>
public class OrganizacaoConfig
{
    public int Id { get; set; }
    public int OrganizacaoId { get; set; }
    public Organizacao Organizacao { get; set; } = null!;

    public int AlertaDiasSemAssinatura { get; set; } = 15;

    /// <summary>Campos obrigatórios — armazenado como JSON. Ex.: ["cliente","regiao","produto","valor","corretor"].</summary>
    public List<string> CamposObrigatorios { get; set; } = new();

    public string? LogoDataUrl { get; set; }
    public string? LogoBase64 { get; set; }
    public string? LogoMimeType { get; set; }
    public DateTime? AtualizadoEm { get; set; }

    // Converter EF usados no DbContext (mantém serialização simples).
    private static readonly JsonSerializerOptions CamposJsonOpts = new();

    public static List<string> DeserializarCampos(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, CamposJsonOpts) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }

    public static string SerializarCampos(List<string> campos) =>
        JsonSerializer.Serialize(campos ?? new List<string>(), CamposJsonOpts);
}