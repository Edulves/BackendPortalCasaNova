using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Dtos;
using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Services.Importacao;

public partial class CsvImportService
{
    public async Task<ImportacaoResult> ExecutarFonteAsync(ImportacaoCsv fonte, int timeout = 30)
    {
        var (ok, err) = CsvHelpers.UrlCsvPermitida(fonte.Url);
        if (!ok)
        {
            _log.LogWarning("csv_url_bloqueada fonte_id={FonteId} err={Erro}", fonte.Id, err);
            return new ImportacaoResult { Erro = err };
        }

        var regiao = fonte.Regiao;
        if (regiao is null)
        {
            regiao = await _db.Regioes.FirstOrDefaultAsync(r =>
                r.OrganizacaoId == fonte.OrganizacaoId &&
                r.Nome.ToLower() == (fonte.RegiaoNome ?? "").ToLower());
        }
        if (regiao is null)
        {
            _log.LogWarning("csv_regiao_ausente fonte_id={FonteId} regiao_nome={RegiaoNome}",
                fonte.Id, fonte.RegiaoNome);
            return new ImportacaoResult { Erro = "Região da fonte não encontrada" };
        }

        var mesRef = CsvHelpers.InferMesRef(fonte.Label, fonte.MesRef);

        string text;
        try
        {
            using var client = _http.CreateClient("csv");
            client.Timeout = TimeSpan.FromSeconds(timeout);
            var resp = await client.GetAsync(fonte.Url);
            resp.EnsureSuccessStatusCode();
            text = await resp.Content.ReadAsStringAsync();
        }
        catch (Exception exc)
        {
            _log.LogError("csv_fetch_falhou fonte_id={FonteId} err={Erro}", fonte.Id, exc.Message);
            return new ImportacaoResult { Erro = $"Falha ao baixar CSV: {exc.Message}" };
        }

        if (!CsvHelpers.TextoPareceCsvVendas(text))
        {
            _log.LogWarning("csv_formato_invalido fonte_id={FonteId} bytes={Bytes}", fonte.Id, text.Length);
            return new ImportacaoResult
            {
                Erro = "Conteúdo não parece CSV de vendas. Publique a aba com cabeçalho "
                    + "Assinatura/Cliente (sem linhas de título acima da tabela).",
            };
        }

        var rows = CsvHelpers.ParseCsvParaVendas(text, regiao, mesRef);
        var res = await MergeVendasImportadasAsync(fonte.Organizacao, regiao, rows, mesRef);

        var result = new ImportacaoResult
        {
            Novas = res.Novas,
            Atualizadas = res.Atualizadas,
            Conflitos = res.Conflitos,
            Linhas = rows.Count,
        };

        fonte.UltimaBusca = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(mesRef) && string.IsNullOrEmpty(fonte.MesRef)) fonte.MesRef = mesRef;
        await _db.SaveChangesAsync();

        _log.LogInformation(
            "csv_sync_ok fonte_id={FonteId} regiao={Regiao} linhas={Linhas} novas={Novas} atualizadas={Atualizadas} conflitos={Conflitos}",
            fonte.Id, regiao.Nome, result.Linhas, res.Novas, res.Atualizadas, res.Conflitos);
        return result;
    }

    /// <summary>
    /// Importa CSV a partir de uma URL e associa à importação existente.
    /// </summary>
    public async Task<ImportacaoResult> ImportarDeUrlAsync(string url, int organizacaoId, int importacaoCsvId, int timeout = 30)
    {
        var (ok, err) = CsvHelpers.UrlCsvPermitida(url);
        if (!ok)
        {
            _log.LogWarning("csv_url_bloqueada importacao_id={ImportacaoId} err={Erro}", importacaoCsvId, err);
            return new ImportacaoResult { Erro = err };
        }

        var org = await _db.Organizacoes.FindAsync(organizacaoId);
        if (org is null)
        {
            return new ImportacaoResult { Erro = "Organização não encontrada" };
        }

        string text;
        try
        {
            using var client = _http.CreateClient("csv");
            client.Timeout = TimeSpan.FromSeconds(timeout);
            var resp = await client.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            text = await resp.Content.ReadAsStringAsync();
        }
        catch (Exception exc)
        {
            _log.LogError("csv_fetch_falhou importacao_id={ImportacaoId} err={Erro}", importacaoCsvId, exc.Message);
            return new ImportacaoResult { Erro = $"Falha ao baixar CSV: {exc.Message}" };
        }

        if (!CsvHelpers.TextoPareceCsvVendas(text))
        {
            _log.LogWarning("csv_formato_invalido importacao_id={ImportacaoId} bytes={Bytes}", importacaoCsvId, text.Length);
            return new ImportacaoResult
            {
                Erro = "Conteúdo não parece CSV de vendas. Publique a aba com cabeçalho "
                    + "Assinatura/Cliente (sem linhas de título acima da tabela).",
            };
        }

        // Tenta encontrar ou criar uma região padrão
        var regiao = await _db.Regioes.FirstOrDefaultAsync(r => r.OrganizacaoId == organizacaoId);
        if (regiao is null)
        {
            regiao = new Regiao
            {
                OrganizacaoId = organizacaoId,
                Organizacao = org,
                Nome = "Principal",
                Slug = "principal"
            };
            _db.Regioes.Add(regiao);
            await _db.SaveChangesAsync();
        }

        var mesRef = CsvHelpers.InferMesRef("", null);
        var rows = CsvHelpers.ParseCsvParaVendas(text, regiao, mesRef);
        var res = await MergeVendasImportadasAsync(org, regiao, rows, mesRef);

        // Atualiza contadores na importação
        var imp = await _db.ImportacoesCsv.FindAsync(importacaoCsvId);
        if (imp is not null)
        {
            imp.VendasProcessadas = rows.Count;
            imp.VendasMergidas = res.Novas + res.Atualizadas;
            await _db.SaveChangesAsync();
        }

        var result = new ImportacaoResult
        {
            Novas = res.Novas,
            Atualizadas = res.Atualizadas,
            Conflitos = res.Conflitos,
            Linhas = rows.Count,
        };

        _log.LogInformation(
            "csv_importar_url_ok importacao_id={ImportacaoId} linhas={Linhas} novas={Novas} atualizadas={Atualizadas} conflitos={Conflitos}",
            importacaoCsvId, result.Linhas, res.Novas, res.Atualizadas, res.Conflitos);
        return result;
    }
}