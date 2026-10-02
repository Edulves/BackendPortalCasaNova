using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Auth;
using PainelFelizCasaNova.Api.Dtos;
using PainelFelizCasaNova.Api.Models;
using PainelFelizCasaNova.Api.Services;

namespace PainelFelizCasaNova.Api.Controllers;

/// <summary>
/// GET /api/usuarios/
/// — Lista de usuários da organização (admin only).
/// </summary>
[Route("api/usuarios")]
[Authorize(Policy = Politicas.PodeAdministrar)]
public class UsuariosController : BaseApiController
{
    private readonly IPasswordHasher<Usuario> _hasher;

    public UsuariosController(IPasswordHasher<Usuario> hasher) => _hasher = hasher;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var usuarios = await Db.Usuarios
            .AsNoTracking()
            .Where(u => u.OrganizacaoId == OrganizacaoId.Value)
            .OrderBy(u => u.Username)
            .Select(u => new UsuarioListResponse
            {
                Id = u.Id,
                Username = u.Username,
                Papel = u.Papel,
                Email = u.Email,
                AtivoAte = u.AtivoAte,
                CriadoEm = u.CriadoEm,
            })
            .ToListAsync();

        return Ok(new { results = usuarios });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UsuarioCreateRequest request)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        if (string.IsNullOrWhiteSpace(request.Username))
            throw new ApiException(400, "Username é obrigatório.", ApiCodes.ValidationError,
                ErroCampo("username", "Campo obrigatório."));

        var existe = await Db.Usuarios.AnyAsync(u =>
            u.OrganizacaoId == OrganizacaoId.Value && u.Username == request.Username);
        if (existe)
            throw new ApiException(400, "Usuário já existe.", ApiCodes.ValidationError,
                ErroCampo("username", "Já existe usuário com este username."));

        var user = new Usuario
        {
            Username = request.Username.Trim(),
            Email = request.Email?.Trim() ?? "",
            Papel = request.Papel ?? Papel.Visualizador,
            OrganizacaoId = OrganizacaoId.Value,
        };

        if (!string.IsNullOrWhiteSpace(request.Senha))
        {
            var erros = SenhaValidator.Validar(request.Senha, request.Username);
            if (erros.Count > 0)
                throw new ApiException(400, string.Join("; ", erros), ApiCodes.ValidationError,
                    ErroCampo("senha", string.Join("; ", erros)));
            user.PasswordHash = _hasher.HashPassword(user, request.Senha);
        }

        Db.Usuarios.Add(user);
        await Db.SaveChangesAsync();
        return StatusCode(201, UsuarioToResponse(user));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Retrieve([FromRoute] int id)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var user = await Db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && u.OrganizacaoId == OrganizacaoId.Value);
        if (user is null)
            throw new ApiException(404, "Usuário não encontrado.", ApiCodes.NotFound);

        return Ok(UsuarioToResponse(user));
    }

    private static UsuarioListResponse UsuarioToResponse(Usuario u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Papel = u.Papel,
        Email = u.Email,
        AtivoAte = u.AtivoAte,
        CriadoEm = u.CriadoEm,
    };
}