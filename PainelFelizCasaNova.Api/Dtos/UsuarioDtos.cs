using System.Text.Json.Serialization;

namespace PainelFelizCasaNova.Api.Dtos;

public class InviteUserRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; } = "";

    [JsonPropertyName("papel")]
    public string? Papel { get; set; }
}

/// <summary>Espelho do UsuarioPublicSerializer.</summary>
public class UsuarioPublicResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("papel")]
    public string Papel { get; set; } = "";

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
}

public class HealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "ok";

    [JsonPropertyName("service")]
    public string Service { get; set; } = "painel-feliz-casa-nova";

    [JsonPropertyName("database")]
    public string? Database { get; set; }

    [JsonPropertyName("database_detail")]
    public string? DatabaseDetail { get; set; }
}