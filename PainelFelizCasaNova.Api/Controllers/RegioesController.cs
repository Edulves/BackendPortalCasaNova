using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Auth;
using PainelFelizCasaNova.Api.Dtos;
using PainelFelizCasaNova.Api.Models;
using PainelFelizCasaNova.Api.Services;

namespace PainelFelizCasaNova.Api.Controllers;

[Route("api/regioes")]
[Authorize(Policy = Politicas.PodeLer)]
public class RegioesController : BaseApiController
{
    public RegioesController() { }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var regioes = await Db.Regioes
            .AsNoTracking()
            .Where(r => r.OrganizacaoId == OrganizacaoId.Value)
            .OrderBy(r => r.Nome)
            .Select(r => new RegiaoDtoResponse
            {
                Id = r.Id,
                Nome = r.Nome,
                Slug = r.Slug,
            })
            .ToListAsync();

        return Ok(new { results = regioes });
    }

    [HttpPost]
    [Authorize(Policy = Politicas.PodeAdministrar)]
    public async Task<IActionResult> Create([FromBody] RegiaoDtoCreate request)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        if (string.IsNullOrWhiteSpace(request.Nome))
            throw new ApiException(400, "Nome é obrigatório.", ApiCodes.ValidationError,
                ErroCampo("nome", "Campo obrigatório."));

        var slug = SlugHelper.ToSlug(request.Nome);
        var existe = await Db.Regioes.AnyAsync(r =>
            r.OrganizacaoId == OrganizacaoId.Value && r.Slug == slug);
        if (existe)
            throw new ApiException(400, "Região já existe.", ApiCodes.ValidationError,
                ErroCampo("nome", "Já existe região com este nome."));

        var regiao = new Regiao
        {
            Nome = request.Nome.Trim(),
            Slug = slug,
            OrganizacaoId = OrganizacaoId.Value,
        };

        Db.Regioes.Add(regiao);
        await Db.SaveChangesAsync();
        return StatusCode(201, RegiaoDtoToResponse(regiao));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Retrieve([FromRoute] int id)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var regiao = await Db.Regioes.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.OrganizacaoId == OrganizacaoId.Value);
        if (regiao is null)
            throw new ApiException(404, "Região não encontrada.", ApiCodes.NotFound);

        return Ok(RegiaoDtoToResponse(regiao));
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = Politicas.PodeAdministrar)]
    public async Task<IActionResult> Update([FromRoute] int id)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var regiao = await Db.Regioes
            .FirstOrDefaultAsync(r => r.Id == id && r.OrganizacaoId == OrganizacaoId.Value);
        if (regiao is null)
            throw new ApiException(404, "Região não encontrada.", ApiCodes.NotFound);

        var (request, presentes) = await LerCorpoAsync<RegiaoDtoUpdate>();
        if (request is null)
            throw new ApiException(400, "Corpo da requisição inválido.");
        if (presentes.Contains("nome") && !string.IsNullOrWhiteSpace(request.Nome))
        {
            var slug = SlugHelper.ToSlug(request.Nome);
            var existe = await Db.Regioes.AnyAsync(r =>
                r.OrganizacaoId == OrganizacaoId.Value && r.Slug == slug && r.Id != id);
            if (existe)
                throw new ApiException(400, "Região já existe.", ApiCodes.ValidationError,
                    ErroCampo("nome", "Já existe região com este nome."));
            regiao.Nome = request.Nome.Trim();
            regiao.Slug = slug;
        }

        await Db.SaveChangesAsync();
        return Ok(RegiaoDtoToResponse(regiao));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Politicas.PodeAdministrar)]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var regiao = await Db.Regioes
            .FirstOrDefaultAsync(r => r.Id == id && r.OrganizacaoId == OrganizacaoId.Value);
        if (regiao is null)
            throw new ApiException(404, "Região não encontrada.", ApiCodes.NotFound);

        var temVendas = await Db.Vendas.AnyAsync(v => v.RegiaoId == id);
        if (temVendas)
            throw new ApiException(400, "Não é possível remover região com vendas.",
                ApiCodes.ValidationError);

        Db.Regioes.Remove(regiao);
        await Db.SaveChangesAsync();
        return StatusCode(204);
    }

    [HttpPatch("{id}/resumo-manual")]
    [Authorize(Policy = Politicas.PodeEscrever)]
    public async Task<IActionResult> UpdateResumoManual([FromRoute] int id,
        [FromBody] ResumoManualUpdateRequest request)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var regiao = await Db.Regioes
            .Include(r => r.ResumoManual)
            .FirstOrDefaultAsync(r => r.Id == id && r.OrganizacaoId == OrganizacaoId.Value);
        if (regiao is null)
            throw new ApiException(404, "Região não encontrada.", ApiCodes.NotFound);

        regiao.ResumoManual ??= new ResumoManual { RegiaoId = id };
        regiao.ResumoManual.VgvManual = request.VgvManual ?? regiao.ResumoManual.VgvManual;
        regiao.ResumoManual.VgcManual = request.VgcManual ?? regiao.ResumoManual.VgcManual;

        await Db.SaveChangesAsync();
        var resp = new { vgv_manual = regiao.ResumoManual.VgvManual, vgc_manual = regiao.ResumoManual.VgcManual };
        return Ok(resp);
    }

    private static RegiaoDtoResponse RegiaoDtoToResponse(Regiao r) => new()
    {
        Id = r.Id,
        Nome = r.Nome,
        Slug = r.Slug,
    };
}