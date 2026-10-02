using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Data;
using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Services.Importacao;

/// <summary>
/// Porta do management command <c>importar_dados_json</c> — importa snapshot
/// JSON (backup/legado) no mesmo formato exportado por /api/estado/.
/// </summary>
public partial class DadosJsonImportService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<Usuario> _hasher;

    public DadosJsonImportService(AppDbContext db, IPasswordHasher<Usuario> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public static List<string> Candidatos(string? file, string baseDir) => new()
    {
        file ?? "",
        Path.Combine("/", "data", "dados.json"),
        Path.Combine(baseDir, "dados.json"),
        Path.Combine(baseDir, "frontend", "dados.json"),
    };

    public async Task<bool> ImportarAsync(string filePath, string orgSlug, string adminUser,
        string adminPassword, string adminEmail, bool clear, bool ifEmpty)
    {
        var data = JsonDocument.Parse(await File.ReadAllTextAsync(filePath));
        var root = data.RootElement;
        var empresa = GetString(root, "empresa") ?? "Feliz Casa Nova";

        var org = await _db.Organizacoes.FirstOrDefaultAsync(o => o.Slug == orgSlug);
        if (org is null)
        {
            org = new Organizacao { Nome = empresa, Slug = orgSlug, CriadoEm = DateTime.UtcNow };
            _db.Organizacoes.Add(org);
        }
        else if (empresa is not null && empresa.Length > 0)
        {
            org.Nome = empresa;
        }
        await _db.SaveChangesAsync();

        if (ifEmpty && await _db.Vendas.AnyAsync(v => v.OrganizacaoId == org.Id)) return false;

        if (clear)
        {
            var vendas = await _db.Vendas.Where(v => v.OrganizacaoId == org.Id).ToListAsync();
            var perdidas = await _db.VendasPerdidas.Where(v => v.OrganizacaoId == org.Id).ToListAsync();
            var fontes = await _db.ImportacoesCsv.Where(f => f.OrganizacaoId == org.Id).ToListAsync();
            var regioes = await _db.Regioes.Where(r => r.OrganizacaoId == org.Id).ToListAsync();
            _db.Vendas.RemoveRange(vendas);
            _db.VendasPerdidas.RemoveRange(perdidas);
            _db.ImportacoesCsv.RemoveRange(fontes);
            _db.Regioes.RemoveRange(regioes);
        }

        var cfg = await _db.OrganizacaoConfigs.FirstOrDefaultAsync(c => c.OrganizacaoId == org.Id);
        if (cfg is null)
        {
            cfg = new OrganizacaoConfig { Organizacao = org };
            _db.OrganizacaoConfigs.Add(cfg);
        }
        if (root.TryGetProperty("config", out var conf))
        {
            cfg.AlertaDiasSemAssinatura = GetInt(conf, "alertaDiasSemAssinatura")
                ?? GetInt(conf, "alerta_dias_sem_assinatura") ?? 15;
            cfg.CamposObrigatorios = GetStringList(conf, "camposObrigatorios")
                ?? GetStringList(conf, "campos_obrigatorios")
                ?? new List<string>(DbSeeder.CamposObrigatoriosPadrao);
        }
        else
        {
            cfg.CamposObrigatorios = new List<string>(DbSeeder.CamposObrigatoriosPadrao);
        }
        cfg.LogoDataUrl = GetString(root, "logoDataUrl") ?? GetString(root, "logo_data_url");
        await _db.SaveChangesAsync();

        var empByNome = new Dictionary<string, Regiao>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("regioes", out var regioesEl) && regioesEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in regioesEl.EnumerateArray())
            {
                var nome = GetString(r, "nome") ?? "";
                var eslug = GetString(r, "id");
                if (string.IsNullOrEmpty(eslug) && !string.IsNullOrEmpty(nome))
                    eslug = CsvHelpers.Slugificar(nome);
                if (string.IsNullOrEmpty(eslug)) continue;

                var reg = await _db.Regioes.FirstOrDefaultAsync(x =>
                    x.OrganizacaoId == org.Id && x.Slug == eslug);
                if (reg is null)
                {
                    reg = new Regiao { Organizacao = org, Slug = eslug, Nome = nome };
                    _db.Regioes.Add(reg);
                }
                else if (!string.IsNullOrEmpty(nome))
                {
                    reg.Nome = nome;
                }
                await _db.SaveChangesAsync();
                empByNome[nome.ToUpperInvariant()] = reg;

                if (r.TryGetProperty("resumoManual", out var rm) && rm.ValueKind == JsonValueKind.Object)
                {
                    var resumo = await _db.ResumoManuais.FirstOrDefaultAsync(x => x.RegiaoId == reg.Id);
                    if (resumo is null)
                    {
                        resumo = new ResumoManual { Regiao = reg };
                        _db.ResumoManuais.Add(resumo);
                    }
                    resumo.Vgv = GetDecimal(rm, "vgv");
                    resumo.Vgc = GetDecimal(rm, "vgc");
                    resumo.Vendas = GetInt(rm, "vendas") ?? 0;
                    resumo.Obs = GetString(rm, "obs") ?? "";
                }
                else
                {
                    var resumo = await _db.ResumoManuais.FirstOrDefaultAsync(x => x.RegiaoId == reg.Id);
                    if (resumo is not null) _db.ResumoManuais.Remove(resumo);
                }
            }
        }
        await _db.SaveChangesAsync();

        return await ImportarRestanteAsync(data, org, empByNome, adminUser, adminPassword, adminEmail);
    }
}