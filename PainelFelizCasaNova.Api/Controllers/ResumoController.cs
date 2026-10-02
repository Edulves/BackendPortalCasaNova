using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Auth;
using PainelFelizCasaNova.Api.Services;

namespace PainelFelizCasaNova.Api.Controllers;

/// <summary>
/// GET /api/resumo/
/// GET /api/resumo/{regiao_slug}/
/// — Espelho de ResumoViewSet (list, retrieve).
/// </summary>
[Route("api/resumo")]
[Authorize(Policy = Politicas.PodeLer)]
public class ResumoController : BaseApiController
{
    private readonly ResumoService _resumo;

    public ResumoController(ResumoService resumo) => _resumo = resumo;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var resultado = await _resumo.CalcularResumoGeralAsync(OrganizacaoId.Value);
        return Ok(resultado);
    }

    [HttpGet("{regiao_slug}")]
    public async Task<IActionResult> Retrieve([FromRoute] string regiao_slug)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var regiao = await Db.Regioes
            .FirstOrDefaultAsync(r => r.OrganizacaoId == OrganizacaoId.Value && r.Slug == regiao_slug);
        if (regiao is null)
            throw new ApiException(404, "Região não encontrada.", ApiCodes.NotFound);

        var resultado = await _resumo.CalcularResumoRegiaoAsync(regiao);
        return Ok(resultado);
    }
}