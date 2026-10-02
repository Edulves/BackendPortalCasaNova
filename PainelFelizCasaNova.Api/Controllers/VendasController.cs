using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Auth;
using PainelFelizCasaNova.Api.Dtos;
using PainelFelizCasaNova.Api.Models;
using PainelFelizCasaNova.Api.Services;

namespace PainelFelizCasaNova.Api.Controllers;

/// <summary>
/// GET/POST /api/vendas/
/// GET/PATCH/DELETE /api/vendas/{id}/
/// — CRUD de vendas com filtros (regiao_slug, cliente, mes, etc).
/// Espelho de VendaViewSet.
/// </summary>
[Route("api/vendas")]
[Authorize(Policy = Politicas.PodeLer)]
public class VendasController : BaseApiController
{
    private readonly VendasService _vendas;

    public VendasController(VendasService vendas) => _vendas = vendas;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery(Name = "regiao_slug")] string? regiao_slug,
        [FromQuery] string? cliente,
        [FromQuery] int? mes,
        [FromQuery] int? ano,
        [FromQuery] string? unidade,
        [FromQuery] string? construtora,
        [FromQuery] int? offset = 0,
        [FromQuery] int? limit = 100)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        offset = offset ?? 0;
        limit = Math.Min(limit ?? 100, 1000);

        var q = Db.Vendas.AsNoTracking()
            .Where(v => v.OrganizacaoId == OrganizacaoId.Value);

        if (!string.IsNullOrEmpty(regiao_slug))
            q = q.Where(v => v.Regiao != null && v.Regiao.Slug == regiao_slug);
        if (!string.IsNullOrEmpty(cliente))
            q = q.Where(v => v.Cliente.ToLower().Contains(cliente.ToLower()));
        if (mes.HasValue)
        {
            var mesStr = mes.Value.ToString("D2"); // "01" a "12"
            q = q.Where(v => v.Mes != null && v.Mes.Contains("-" + mesStr));
        }
        if (ano.HasValue)
        {
            var anoStr = ano.Value.ToString();
            q = q.Where(v => v.Mes != null && v.Mes.StartsWith(anoStr));
        }
        if (!string.IsNullOrEmpty(unidade))
            q = q.Where(v => v.Unidade.ToLower().Contains(unidade.ToLower()));
        if (!string.IsNullOrEmpty(construtora))
            q = q.Where(v => v.Construtora.ToLower().Contains(construtora.ToLower()));

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(v => v.Mes)
            .ThenBy(v => v.Cliente)
            .Skip(offset.Value)
            .Take(limit.Value)
            .Select(v => new VendaListResponse
            {
                Id = v.Id,
                Cliente = v.Cliente,
                Numero = v.Numero,
                Mes = ExtrairMesDoMesRef(v.Mes),
                Ano = ExtrairAnoDoMesRef(v.Mes),
                Unidade = v.Unidade,
                Construtora = v.Construtora,
                Assinada = v.DataAssinatura != null,
                Valor = v.Valor,
                RegiaoSlug = v.Regiao != null ? v.Regiao.Slug : null,
            })
            .ToListAsync();

        return Ok(new { count = total, results = items });
    }

    [HttpPost]
    [Authorize(Policy = Politicas.PodeEscrever)]
    public async Task<IActionResult> Create([FromBody] VendaCreateRequest request)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var regiao = await Db.Regioes
            .FirstOrDefaultAsync(r => r.OrganizacaoId == OrganizacaoId.Value && r.Id == request.RegiaoId);
        if (regiao is null)
            throw new ApiException(400, "Região inválida.", ApiCodes.ValidationError,
                ErroCampo("regiao_id", "Região não encontrada."));

        var venda = new Models.Venda
        {
            OrganizacaoId = OrganizacaoId.Value,
            RegiaoId = request.RegiaoId,
            Cliente = request.Cliente ?? "",
            Numero = request.Numero ?? "",
            Telefone = request.Telefone ?? "",
            Unidade = request.Unidade ?? "",
            Construtora = request.Construtora ?? "",
            Produto = request.Produto ?? "",
            BlocoQuadra = request.BlocoQuadra ?? "",
            Valor = request.Valor,
            Percentual = request.Percentual,
            Comissao = request.Comissao,
            Corretor = request.Corretor ?? "",
            Origem = request.Origem ?? "",
            Financiado = request.Financiado ?? "",
            Mes = $"{request.Ano:D4}-{request.Mes:D2}",
            DataAssinatura = request.DataAssinatura ?? (request.Assinada ? DateOnly.FromDateTime(DateTime.UtcNow) : null),
            DataEntrada = request.DataEntrada,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow,
        };

        Db.Vendas.Add(venda);
        await Db.SaveChangesAsync();
        return StatusCode(201, VendaToResponse(venda));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Retrieve([FromRoute] int id)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var venda = await Db.Vendas.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id && v.OrganizacaoId == OrganizacaoId.Value);
        if (venda is null)
            throw new ApiException(404, "Venda não encontrada.", ApiCodes.NotFound);

        return Ok(VendaToResponse(venda));
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = Politicas.PodeEscrever)]
    public async Task<IActionResult> Update([FromRoute] int id)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var venda = await Db.Vendas
            .FirstOrDefaultAsync(v => v.Id == id && v.OrganizacaoId == OrganizacaoId.Value);
        if (venda is null)
            throw new ApiException(404, "Venda não encontrada.", ApiCodes.NotFound);

        var (request, presentes) = await LerCorpoAsync<VendaUpdateRequest>();
        if (request is null)
            throw new ApiException(400, "Corpo da requisição inválido.");
        if (presentes.Contains("Cliente") || presentes.Contains("cliente")) venda.Cliente = request.Cliente ?? venda.Cliente;
        if (presentes.Contains("Numero") || presentes.Contains("numero")) venda.Numero = request.Numero ?? venda.Numero;
        if (presentes.Contains("Telefone") || presentes.Contains("telefone")) venda.Telefone = request.Telefone ?? venda.Telefone;
        if (presentes.Contains("Unidade") || presentes.Contains("unidade")) venda.Unidade = request.Unidade ?? venda.Unidade;
        if (presentes.Contains("Construtora") || presentes.Contains("construtora")) venda.Construtora = request.Construtora ?? venda.Construtora;
        if (presentes.Contains("Produto") || presentes.Contains("produto")) venda.Produto = request.Produto ?? venda.Produto;
        if (presentes.Contains("BlocoQuadra") || presentes.Contains("blocoQuadra")) venda.BlocoQuadra = request.BlocoQuadra ?? venda.BlocoQuadra;
        if ((presentes.Contains("Valor") || presentes.Contains("valor")) && request.Valor.HasValue) venda.Valor = request.Valor.Value;
        if (presentes.Contains("Percentual") || presentes.Contains("percentual")) venda.Percentual = request.Percentual ?? venda.Percentual;
        if ((presentes.Contains("Comissao") || presentes.Contains("comissao")) && request.Comissao.HasValue) venda.Comissao = request.Comissao.Value;
        if (presentes.Contains("Corretor") || presentes.Contains("corretor")) venda.Corretor = request.Corretor ?? venda.Corretor;
        if (presentes.Contains("Origem") || presentes.Contains("origem")) venda.Origem = request.Origem ?? venda.Origem;
        if (presentes.Contains("Financiado") || presentes.Contains("financiado")) venda.Financiado = request.Financiado ?? venda.Financiado;
        if ((presentes.Contains("RegiaoId") || presentes.Contains("regiaoId")) && request.RegiaoId.HasValue) venda.RegiaoId = request.RegiaoId.Value;
        if ((presentes.Contains("Mes") || presentes.Contains("mes")) && request.Mes.HasValue && (presentes.Contains("Ano") || presentes.Contains("ano")) && request.Ano.HasValue)
            venda.Mes = $"{request.Ano.Value:D4}-{request.Mes.Value:D2}";
        if (presentes.Contains("DataAssinatura") || presentes.Contains("dataAssinatura"))
            venda.DataAssinatura = request.DataAssinatura;
        else if ((presentes.Contains("Assinada") || presentes.Contains("assinada")) && request.Assinada.HasValue)
            venda.DataAssinatura = request.Assinada.Value ? DateOnly.FromDateTime(DateTime.UtcNow) : null;
        if (presentes.Contains("DataEntrada") || presentes.Contains("dataEntrada"))
            venda.DataEntrada = request.DataEntrada;
        venda.AtualizadoEm = DateTime.UtcNow;

        await Db.SaveChangesAsync();
        return Ok(VendaToResponse(venda));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Politicas.PodeEscrever)]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var venda = await Db.Vendas
            .FirstOrDefaultAsync(v => v.Id == id && v.OrganizacaoId == OrganizacaoId.Value);
        if (venda is null)
            throw new ApiException(404, "Venda não encontrada.", ApiCodes.NotFound);

        Db.Vendas.Remove(venda);
        await Db.SaveChangesAsync();
        return StatusCode(204);
    }

    private static VendaDetailResponse VendaToResponse(Venda v) => new()
    {
        Id = v.Id,
        Cliente = v.Cliente,
        Numero = v.Numero,
        Mes = ExtrairMesDoMesRef(v.Mes),
        Ano = ExtrairAnoDoMesRef(v.Mes),
        Unidade = v.Unidade,
        Construtora = v.Construtora,
        Assinada = v.DataAssinatura != null,
        Valor = v.Valor,
        RegiaoId = v.RegiaoId ?? 0,
        Telefone = v.Telefone,
        Produto = v.Produto,
        BlocoQuadra = v.BlocoQuadra,
        Percentual = v.Percentual,
        Comissao = v.Comissao,
        Corretor = v.Corretor,
        Origem = v.Origem,
        Financiado = v.Financiado,
        DataAssinatura = v.DataAssinatura,
        DataEntrada = v.DataEntrada,
        CriadoEm = v.CriadoEm,
        AtualizadoEm = v.AtualizadoEm,
    };

    /// <summary>Extrai o mês de um campo Mes no formato "YYYY-MM".</summary>
    private static int ExtrairMesDoMesRef(string? mesRef)
    {
        if (string.IsNullOrEmpty(mesRef) || mesRef.Length < 7) return 0;
        return int.TryParse(mesRef.Substring(5, 2), out var m) ? m : 0;
    }

    /// <summary>Extrai o ano de um campo Mes no formato "YYYY-MM".</summary>
    private static int ExtrairAnoDoMesRef(string? mesRef)
    {
        if (string.IsNullOrEmpty(mesRef) || mesRef.Length < 4) return 0;
        return int.TryParse(mesRef.Substring(0, 4), out var a) ? a : 0;
    }
}