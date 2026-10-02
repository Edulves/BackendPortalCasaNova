using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Auth;
using PainelFelizCasaNova.Api.Dtos;
using PainelFelizCasaNova.Api.Models;
using PainelFelizCasaNova.Api.Services;
using PainelFelizCasaNova.Api.Services.Importacao;

namespace PainelFelizCasaNova.Api.Controllers;

[Route("api/importacoes-csv")]
[Authorize(Policy = Politicas.PodeEscrever)]
public class ImportacaoCsvController : BaseApiController
{
    private readonly CsvImportService _csvImport;
    private readonly DadosJsonImportService _jsonImport;

    public ImportacaoCsvController(CsvImportService csvImport, DadosJsonImportService jsonImport)
    {
        _csvImport = csvImport;
        _jsonImport = jsonImport;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var imports = await Db.ImportacoesCsv
            .AsNoTracking()
            .Where(i => i.OrganizacaoId == OrganizacaoId.Value)
            .OrderByDescending(i => i.CriadoEm)
            .Select(i => new ImportacaoCsvResponse
            {
                Id = i.Id,
                Status = i.Status,
                VendasProcessadas = i.VendasProcessadas,
                VendasMergidas = i.VendasMergidas,
                VendasPerdidas = i.VendasPerdidas,
                ErroMsg = i.ErroMsg,
                CriadoEm = i.CriadoEm,
            })
            .ToListAsync();

        return Ok(new { results = imports });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ImportacaoCsvCreateRequest request)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        if (string.IsNullOrWhiteSpace(request.Url))
            throw new ApiException(400, "URL é obrigatória.", ApiCodes.ValidationError,
                ErroCampo("url", "Campo obrigatório."));

        var imp = new ImportacaoCsv
        {
            Url = request.Url,
            Status = "pendente",
            OrganizacaoId = OrganizacaoId.Value,
        };

        Db.ImportacoesCsv.Add(imp);
        await Db.SaveChangesAsync();
        return StatusCode(201, ImportacaoCsvToResponse(imp));
    }

    [HttpPost("{id}/executar")]
    public async Task<IActionResult> Executar([FromRoute] int id)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var imp = await Db.ImportacoesCsv
            .FirstOrDefaultAsync(i => i.Id == id && i.OrganizacaoId == OrganizacaoId.Value);
        if (imp is null)
            throw new ApiException(404, "Importação não encontrada.", ApiCodes.NotFound);

        imp.Status = "executando";
        await Db.SaveChangesAsync();

        try
        {
            await _csvImport.ImportarDeUrlAsync(imp.Url, OrganizacaoId.Value, imp.Id);
            imp.Status = "concluído";
        }
        catch (Exception ex)
        {
            imp.Status = "erro";
            imp.ErroMsg = ex.Message[..Math.Min(500, ex.Message.Length)];
        }

        await Db.SaveChangesAsync();
        return Ok(ImportacaoCsvToResponse(imp));
    }

    [HttpPost("{id}/limpar")]
    [Authorize(Policy = Politicas.PodeAdministrar)]
    public async Task<IActionResult> Limpar([FromRoute] int id)
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var imp = await Db.ImportacoesCsv
            .FirstOrDefaultAsync(i => i.Id == id && i.OrganizacaoId == OrganizacaoId.Value);
        if (imp is null)
            throw new ApiException(404, "Importação não encontrada.", ApiCodes.NotFound);

        var importIds = await Db.Vendas
            .Where(v => v.ImportacaoCsvId == id)
            .Select(v => v.Id)
            .ToListAsync();

        if (importIds.Count > 0)
        {
            var vendas = await Db.Vendas.Where(v => importIds.Contains(v.Id)).ToListAsync();
            Db.Vendas.RemoveRange(vendas);
        }

        imp.Status = "pendente";
        imp.VendasProcessadas = 0;
        imp.VendasMergidas = 0;
        imp.ErroMsg = null;

        await Db.SaveChangesAsync();
        return Ok(ImportacaoCsvToResponse(imp));
    }

    private static ImportacaoCsvResponse ImportacaoCsvToResponse(ImportacaoCsv imp) => new()
    {
        Id = imp.Id,
        Status = imp.Status,
        VendasProcessadas = imp.VendasProcessadas,
        VendasMergidas = imp.VendasMergidas,
        VendasPerdidas = imp.VendasPerdidas,
        ErroMsg = imp.ErroMsg,
        CriadoEm = imp.CriadoEm,
    };
}