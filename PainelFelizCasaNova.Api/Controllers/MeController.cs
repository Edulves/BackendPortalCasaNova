using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Auth;
using PainelFelizCasaNova.Api.Dtos;
using PainelFelizCasaNova.Api.Services;

namespace PainelFelizCasaNova.Api.Controllers;

/// <summary>GET /api/me/ — Usuário autenticado.</summary>
[Route("api/me")]
[Authorize]
public class MeController : BaseApiController
{
    private readonly AuthService _auth;

    public MeController(AuthService auth) => _auth = auth;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var user = await Db.Usuarios.Include(u => u.Organizacao)
            .FirstOrDefaultAsync(u => u.Id == UsuarioId);
        if (user is null)
            throw new ApiException(401, "Usuário não encontrado.");

        var r = _auth.BuildTokenResponse(user);
        return Ok(new MeResponse
        {
            Id = user.Id,
            Username = user.Username,
            Papel = user.Papel,
            OrganizacaoId = user.OrganizacaoId,
            OrganizacaoNome = user.Organizacao?.Nome,
            PodeEscrever = user.PodeEscrever,
            PodeAdministrar = user.PodeAdministrar,
        });
    }
}