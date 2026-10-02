using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Auth;
using PainelFelizCasaNova.Api.Data;
using PainelFelizCasaNova.Api.Dtos;
using PainelFelizCasaNova.Api.Models;
using PainelFelizCasaNova.Api.Services;

namespace PainelFelizCasaNova.Api.Controllers;

/// <summary>
/// GET/POST /api/vendas-perdidas/
/// GET/PATCH/DELETE /api/vendas-perdidas/{id}/
/// — CRUD de vendas perdidas com filtros.
/// Espelho de VendaPerdidaViewSet.
/// </summary>
[Route("api/vendas-perdidas")]
[Authorize(Policy = Politicas.PodeLer)]
public class VendasPerdidasController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery(Name = "regiao_slug")] string? regiao_slug,
        [FromQuery] string? cliente,
        [FromQuery] int? mes,
        [FromQuery] int? ano,
        [FromQuery] int? offset = 0,
        [FromQuery] int? limit = 100)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        offset = offset ?? 0;
        limit = Math.Min(limit ?? 100, 1000);

        var q = Db.VendasPerdidas.AsNoTracking()
            .Where(v => v.OrganizacaoId == OrganizacaoId.Value);

        if (!string.IsNullOrEmpty(regiao_slug))
            q = q.Where(v => v.Regiao != null && v.Regiao.Slug == regiao_slug);
        if (!string.IsNullOrEmpty(cliente))
            q = q.Where(v => v.Cliente.ToLower().Contains(cliente.ToLower()));
        if (mes.HasValue)
            q = q.Where(v => v.MesNum == mes.Value);
        if (ano.HasValue)
            q = q.Where(v => v.Ano == ano.Value);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(v => v.Ano)
            .ThenByDescending(v => v.MesNum)
            .ThenBy(v => v.Cliente)
            .Skip(offset.Value)
            .Take(limit.Value)
            .Select(v => new VendaPerdidaListResponse
            {
                Id = v.Id,
                Cliente = v.Cliente,
                Mes = v.MesNum,
                Ano = v.Ano,
                Motivo = v.Motivo,
                RegiaoSlug = v.Regiao != null ? v.Regiao.Slug : null,
            })
            .ToListAsync();

        return Ok(new { count = total, results = items });
    }

    [HttpPost]
    [Authorize(Policy = Politicas.PodeEscrever)]
    public async Task<IActionResult> Create([FromBody] VendaPerdidaCreateRequest request)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var regiao = await Db.Regioes
            .FirstOrDefaultAsync(r => r.OrganizacaoId == OrganizacaoId.Value && r.Id == request.RegiaoId);
        if (regiao is null)
            throw new ApiException(400, "Região inválida.", ApiCodes.ValidationError,
                ErroCampo("regiao_id", "Região não encontrada."));

        var vp = new VendaPerdida
        {
            OrganizacaoId = OrganizacaoId.Value,
            Cliente = request.Cliente ?? "",
            MesNum = request.Mes,
            Ano = request.Ano,
            Motivo = request.Motivo ?? "",
            RegiaoId = request.RegiaoId,
        };

        Db.VendasPerdidas.Add(vp);
        await Db.SaveChangesAsync();
        return StatusCode(201, VendaPerdidaToResponse(vp));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Retrieve([FromRoute] int id)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var vp = await Db.VendasPerdidas.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id && v.OrganizacaoId == OrganizacaoId.Value);
        if (vp is null)
            throw new ApiException(404, "Venda perdida não encontrada.", ApiCodes.NotFound);

        return Ok(VendaPerdidaToResponse(vp));
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = Politicas.PodeEscrever)]
    public async Task<IActionResult> Update([FromRoute] int id)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var vp = await Db.VendasPerdidas
            .FirstOrDefaultAsync(v => v.Id == id && v.OrganizacaoId == OrganizacaoId.Value);
        if (vp is null)
            throw new ApiException(404, "Venda perdida não encontrada.", ApiCodes.NotFound);

        var (request, presentes) = await LerCorpoAsync<VendaPerdidaUpdateRequest>();
        if (request is null)
            throw new ApiException(400, "Corpo da requisição inválido.");
        if (presentes.Contains("cliente")) vp.Cliente = request.Cliente ?? vp.Cliente;
        if (presentes.Contains("mes")) vp.MesNum = request.Mes ?? vp.MesNum;
        if (presentes.Contains("ano")) vp.Ano = request.Ano ?? vp.Ano;
        if (presentes.Contains("motivo")) vp.Motivo = request.Motivo ?? vp.Motivo;
        vp.AtualizadoEm = DateTime.UtcNow;

        await Db.SaveChangesAsync();
        return Ok(VendaPerdidaToResponse(vp));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Politicas.PodeEscrever)]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var vp = await Db.VendasPerdidas
            .FirstOrDefaultAsync(v => v.Id == id && v.OrganizacaoId == OrganizacaoId.Value);
        if (vp is null)
            throw new ApiException(404, "Venda perdida não encontrada.", ApiCodes.NotFound);

        Db.VendasPerdidas.Remove(vp);
        await Db.SaveChangesAsync();
        return StatusCode(204);
    }

    private static VendaPerdidaDetailResponse VendaPerdidaToResponse(VendaPerdida vp) => new()
    {
        Id = vp.Id,
        Cliente = vp.Cliente,
        Mes = vp.MesNum,
        Ano = vp.Ano,
        Motivo = vp.Motivo,
        RegiaoId = vp.RegiaoId ?? 0,
        CriadoEm = vp.CriadoEm,
        AtualizadoEm = vp.AtualizadoEm,
    };
}