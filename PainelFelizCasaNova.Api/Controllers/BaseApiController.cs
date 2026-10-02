using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PainelFelizCasaNova.Api.Auth;
using PainelFelizCasaNova.Api.Data;
using PainelFelizCasaNova.Api.Services;

namespace PainelFelizCasaNova.Api.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected int? UsuarioId => User.ObterUsuarioId();
    protected int? OrganizacaoId => User.ObterOrganizacaoId();

    protected AppDbContext Db => HttpContext.RequestServices.GetRequiredService<AppDbContext>();

    /// <summary>
    /// Lê o corpo JSON manualmente (sem [FromBody]) e retorna o DTO + as chaves
    /// presentes — necessário para distinguir null explícito de campo ausente.
    /// </summary>
    protected async Task<(T? Dado, HashSet<string> Presentes)> LerCorpoAsync<T>()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var json = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(json)) json = "{}";

        var presentes = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject()) presentes.Add(prop.Name);
            var dado = JsonSerializer.Deserialize<T>(json);
            return (dado, presentes);
        }
        catch (JsonException)
        {
            throw new ApiException(400, "JSON inválido.", ApiCodes.ValidationError,
                "Body inválido.");
        }
    }

    protected static Dictionary<string, object?> ErroCampo(string campo, string mensagem) =>
        new() { [campo] = new List<string> { mensagem } };
}