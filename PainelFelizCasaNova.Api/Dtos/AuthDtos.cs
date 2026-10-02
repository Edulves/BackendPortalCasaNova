using System.Text.Json.Serialization;

namespace PainelFelizCasaNova.Api.Dtos;

public class TokenRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("password")]
    public string Password { get; set; } = "";
}

/// <summary>Resposta do login — espelho do CustomTokenObtainPairSerializer.</summary>
public class TokenResponse
{
    [JsonPropertyName("access")]
    public string Access { get; set; } = "";

    [JsonPropertyName("refresh")]
    public string Refresh { get; set; } = "";

    [JsonPropertyName("papel")]
    public string Papel { get; set; } = "";

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("organizacao_id")]
    public int? OrganizacaoId { get; set; }

    [JsonPropertyName("organizacao_nome")]
    public string? OrganizacaoNome { get; set; }

    [JsonPropertyName("pode_escrever")]
    public bool PodeEscrever { get; set; }

    [JsonPropertyName("pode_administrar")]
    public bool PodeAdministrar { get; set; }
}

public class RefreshRequest
{
    [JsonPropertyName("refresh")]
    public string Refresh { get; set; } = "";
}

public class RefreshResponse
{
    [JsonPropertyName("access")]
    public string Access { get; set; } = "";
}

public class PasswordResetRequest
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }
}

public class PasswordResetConfirmRequest
{
    [JsonPropertyName("uid")]
    public string Uid { get; set; } = "";

    [JsonPropertyName("token")]
    public string Token { get; set; } = "";

    [JsonPropertyName("new_password")]
    public string NewPassword { get; set; } = "";
}

/// <summary>Espelho do MessageResponseSerializer (code opcional, message, debug_link).</summary>
public class MessageResponse
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("debug_link")]
    public string? DebugLink { get; set; }
}

public class MeResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("papel")]
    public string Papel { get; set; } = "";

    [JsonPropertyName("organizacao_id")]
    public int? OrganizacaoId { get; set; }

    [JsonPropertyName("organizacao_nome")]
    public string? OrganizacaoNome { get; set; }

    [JsonPropertyName("pode_escrever")]
    public bool PodeEscrever { get; set; }

    [JsonPropertyName("pode_administrar")]
    public bool PodeAdministrar { get; set; }
}