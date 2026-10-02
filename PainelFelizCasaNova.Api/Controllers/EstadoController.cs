using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Auth;
using PainelFelizCasaNova.Api.Services;

namespace PainelFelizCasaNova.Api.Controllers;

/// <summary>
/// GET /api/estado/
/// — Snapshot JSON completo da organização (regiões, vendas, etc).
/// Espelho de EstadoViewSet (para backup/export).
/// </summary>
[Route("api/estado")]
[Authorize(Policy = Politicas.PodeAdministrar)]
public class EstadoController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var org = await Db.Organizacoes
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == OrganizacaoId.Value);
        if (org is null)
            throw new ApiException(404, "Organização não encontrada.", ApiCodes.NotFound);

        var regioes = await Db.Regioes
            .AsNoTracking()
            .Where(r => r.OrganizacaoId == OrganizacaoId.Value)
            .ToListAsync();

        var vendas = await Db.Vendas
            .AsNoTracking()
            .Where(v => v.OrganizacaoId == OrganizacaoId.Value)
            .ToListAsync();

        var vendasPerdidas = await Db.VendasPerdidas
            .AsNoTracking()
            .Where(v => v.OrganizacaoId == OrganizacaoId.Value)
            .ToListAsync();

        var estado = new
        {
            organizacao = new
            {
                id = org.Id,
                nome = org.Nome,
                slug = org.Slug,
            },
            regioes = regioes.Select(r => new
            {
                id = r.Id,
                nome = r.Nome,
                slug = r.Slug,
            }),
            vendas = vendas.Select(v => new
            {
                id = v.Id,
                cliente = v.Cliente,
                numero = v.Numero,
                telefone = v.Telefone,
                mes = v.Mes,
                unidade = v.Unidade,
                construtora = v.Construtora,
                produto = v.Produto,
                blocoQuadra = v.BlocoQuadra,
                valor = v.Valor,
                percentual = v.Percentual,
                comissao = v.Comissao,
                corretor = v.Corretor,
                origem = v.Origem,
                financiado = v.Financiado,
                assinada = v.DataAssinatura != null,
                dataAssinatura = v.DataAssinatura,
                dataEntrada = v.DataEntrada,
                regiao_id = v.RegiaoId,
                exemplo = v.Exemplo,
            }),
            vendasPerdidas = vendasPerdidas.Select(vp => new
            {
                id = vp.Id,
                cliente = vp.Cliente,
                mes = vp.Mes,
                ano = vp.Ano,
                motivo = vp.Motivo,
                regiao_id = vp.RegiaoId,
            }),
        };

        Response.Headers.Add("Content-Disposition", $"attachment; filename=\"estado-{org.Slug}.json\"");
        return Ok(estado);
    }
}