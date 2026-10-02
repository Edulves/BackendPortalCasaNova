using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Data;
using PainelFelizCasaNova.Api.Dtos;
using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Services;

/// <summary>Espelho do <c>core/services/resumo.py</c> — resumo geral e por região.</summary>
public class ResumoService
{
    private readonly AppDbContext _db;

    public ResumoService(AppDbContext db) => _db = db;

    public async Task<ResumoGeralOutput> CalcularResumoGeralAsync(int organizacaoId)
    {
        var vendas = _db.Vendas.Where(v => v.OrganizacaoId == organizacaoId);
        var fechadas = await VendasService.QsFechadas(vendas).ToListAsync();
        var vgv = VendasService.SumVgv(fechadas);
        var vgc = VendasService.SumVgc(fechadas);
        var totalVendas = fechadas.Count;

        decimal manualVgv = 0m, manualVgc = 0m;
        var manualVendas = 0;
        var manuaisUsados = new List<string>();
        var manuaisIgnorados = new List<RegiaoManualIgnoradoOutput>();

        var regioes = await _db.Regioes
            .Where(r => r.OrganizacaoId == organizacaoId)
            .Include(r => r.ResumoManual)
            .OrderBy(r => r.Nome)
            .ToListAsync();

        foreach (var reg in regioes)
        {
            var rm = reg.ResumoManual;
            if (rm is null) continue;

            var n = await VendasService.QsNaoExemplo(_db.Vendas.Where(v => v.RegiaoId == reg.Id)).CountAsync();
            if (VendasService.UsarResumoManual(reg, n))
            {
                manualVgv += rm.Vgv;
                manualVgc += rm.Vgc;
                manualVendas += rm.Vendas;
                manuaisUsados.Add(reg.Nome);
            }
            else
            {
                manuaisIgnorados.Add(new RegiaoManualIgnoradoOutput
                {
                    Nome = reg.Nome,
                    VendasLancadas = n,
                });
            }
        }

        return new ResumoGeralOutput
        {
            Vgv = (double)(vgv + manualVgv),
            Vgc = (double)(vgc + manualVgc),
            VendasFechadas = totalVendas + manualVendas,
            VgvLinhas = (double)vgv,
            VgcLinhas = (double)vgc,
            VendasLinhas = totalVendas,
            ManualVgv = (double)manualVgv,
            ManualVgc = (double)manualVgc,
            ManualVendas = manualVendas,
            RegioesResumoManual = manuaisUsados,
            RegioesManualIgnorado = manuaisIgnorados,
        };
    }

    public async Task<ResumoRegiaoOutput> CalcularResumoRegiaoAsync(Regiao regiao)
    {
        var vendas = _db.Vendas.Where(v => v.RegiaoId == regiao.Id);
        var qsNaoExemplo = VendasService.QsNaoExemplo(vendas);
        var fechadas = await VendasService.QsFechadas(vendas).ToListAsync();
        var vendasLancadas = await qsNaoExemplo.CountAsync();

        var rm = regiao.ResumoManual;
        var usarManual = VendasService.UsarResumoManual(regiao, vendasLancadas);

        if (usarManual && rm is not null)
        {
            return new ResumoRegiaoOutput
            {
                Modo = "resumo_manual",
                Vgv = (double)rm.Vgv,
                Vgc = (double)rm.Vgc,
                Vendas = rm.Vendas,
                Obs = rm.Obs,
                VendasLancadas = vendasLancadas,
                ManualIgnorado = false,
            };
        }

        var vgv = VendasService.SumVgv(fechadas);
        var vgc = VendasService.SumVgc(fechadas);
        var n = fechadas.Count;
        double ticket = n > 0 ? (double)(vgv / n) : 0.0;
        return new ResumoRegiaoOutput
        {
            Modo = "linhas",
            Vgv = (double)vgv,
            Vgc = (double)vgc,
            Vendas = n,
            TicketMedio = ticket,
            VendasLancadas = vendasLancadas,
            ManualIgnorado = rm is not null,
            Obs = rm?.Obs ?? "",
        };
    }
}