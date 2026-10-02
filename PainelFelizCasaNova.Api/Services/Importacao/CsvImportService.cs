using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Data;
using PainelFelizCasaNova.Api.Dtos;
using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Services.Importacao;

/// <summary>
/// Importação CSV/XLSX — porta de <c>core/services/csv_import.py</c>.
/// </summary>
public partial class CsvImportService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<CsvImportService> _log;

    public CsvImportService(AppDbContext db, IHttpClientFactory http, ILogger<CsvImportService> log)
    {
        _db = db;
        _http = http;
        _log = log;
    }

    public async Task<Regiao?> ResolverRegiaoAsync(Organizacao org, string? nome)
    {
        nome = (nome ?? "").Trim();
        if (string.IsNullOrEmpty(nome)) return null;

        var existente = await _db.Regioes.FirstOrDefaultAsync(r =>
            r.OrganizacaoId == org.Id && r.Nome.ToLower() == nome.ToLower());
        if (existente is not null) return existente;

        var baseSlug = CsvHelpers.Slugificar(nome);
        var slug = baseSlug;
        var n = 2;
        while (await _db.Regioes.AnyAsync(r => r.OrganizacaoId == org.Id && r.Slug == slug))
        {
            slug = $"{baseSlug}-{n}";
            n++;
        }

        var regiao = new Regiao { Organizacao = org, Slug = slug, Nome = nome };
        _db.Regioes.Add(regiao);
        await _db.SaveChangesAsync();
        return regiao;
    }

    public async Task<Dictionary<string, object>> AtribuirRegiaoPendenteAsync(int orgId, Regiao regiao, List<int>? ids)
    {
        var qs = _db.Vendas.Where(v => v.OrganizacaoId == orgId && v.RegiaoId == null);
        if (ids is { Count: > 0 })
        {
            var set = ids.ToHashSet();
            qs = qs.Where(v => set.Contains(v.Id));
        }
        var candidatas = await qs.ToListAsync();
        foreach (var v in candidatas) v.RegiaoId = regiao.Id;
        await _db.SaveChangesAsync();
        return new Dictionary<string, object> { ["atualizadas"] = candidatas.Count, ["regiao"] = regiao.Nome };
    }
}