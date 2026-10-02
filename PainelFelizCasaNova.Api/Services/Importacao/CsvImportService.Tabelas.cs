using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Dtos;
using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Services.Importacao;

public partial class CsvImportService
{
    public async Task<ImportacaoResult> ImportarTabelasAsync(Organizacao org,
        List<(string Nome, List<List<object?>> Linhas)> tabelas,
        Regiao? regiaoPadrao, bool substituir, int? ano = null)
    {
        if (substituir) await LimparVendasAsync(org.Id, regiaoPadrao);

        var totais = new ImportacaoResult { Regioes = new List<string>() };
        var vistas = new HashSet<string>();

        foreach (var (nomeFolha, linhas) in tabelas)
        {
            if (CsvHelpers.FolhaResumo(nomeFolha)) continue;

            if (CsvHelpers.FolhaPerdidas(nomeFolha))
            {
                var (novasP, atuP) = await ImportarPerdidasTabelaAsync(org, linhas, ano);
                totais.PerdidasNovas += novasP;
                totais.PerdidasAtualizadas += atuP;
                continue;
            }

            var mesRef = CsvHelpers.MesDaFolha(nomeFolha, ano);
            var rows = CsvHelpers.TabelaParaVendas(linhas, regiaoPadrao, mesRef);

            var prontas = new List<VendaRow>();
            foreach (var row in rows)
            {
                var regiao = row.Regiao ?? regiaoPadrao;
                if (regiao is null && !string.IsNullOrEmpty(row.RegiaoNome))
                    regiao = await ResolverRegiaoAsync(org, row.RegiaoNome);
                row.Regiao = regiao;
                prontas.Add(row);
            }

            var porRegiao = new Dictionary<int?, List<VendaRow>>();
            foreach (var row in prontas)
            {
                var chave = row.Regiao?.Id;
                if (!porRegiao.TryGetValue(chave, out var grupo))
                {
                    grupo = new List<VendaRow>();
                    porRegiao[chave] = grupo;
                }
                grupo.Add(row);

                if (row.Regiao is not null) vistas.Add(row.Regiao.Nome);
                else totais.SemRegiao++;
            }

            foreach (var grupo in porRegiao.Values)
            {
                var regiao = grupo[0].Regiao;
                var res = await MergeVendasImportadasAsync(org, regiao, grupo, mesRef);
                totais.Novas += res.Novas;
                totais.Atualizadas += res.Atualizadas;
                totais.Conflitos += res.Conflitos;
                totais.Linhas += grupo.Count;
            }
        }

        totais.Regioes = vistas.OrderBy(x => x).ToList();
        if (totais.Linhas == 0 && totais.PerdidasNovas == 0 && totais.PerdidasAtualizadas == 0
            && string.IsNullOrEmpty(totais.Erro))
        {
            totais.Erro = "Nenhuma venda reconhecida. Use cabeçalho com Cliente "
                + "(e, se puder, Nº, Assinatura, Empreendimento, Valor).";
        }
        return totais;
    }

    public async Task<ImportacaoResult> ImportarArquivoAsync(Organizacao org, string nome,
        byte[] conteudo, Regiao? regiaoPadrao, bool substituir)
    {
        var nomeL = (nome ?? "").ToLowerInvariant();
        if (nomeL.EndsWith(".xlsx"))
        {
            List<(string, List<List<object?>>)> folhas;
            try { folhas = XlsxReader.LerXlsxBytes(conteudo); }
            catch (Exception)
            {
                return new ImportacaoResult { Erro = "Não foi possível ler o XLSX." };
            }
            return await ImportarTabelasAsync(org, folhas, regiaoPadrao, substituir,
                CsvHelpers.AnoDoNome(nome));
        }
        if (nomeL.EndsWith(".csv"))
        {
            return await ImportarTabelasAsync(org,
                new List<(string, List<List<object?>>)> { ("csv", CsvHelpers.LerCsvBytes(conteudo)) },
                regiaoPadrao, substituir, CsvHelpers.AnoDoNome(nome));
        }
        return new ImportacaoResult { Erro = "Envie um arquivo .csv ou .xlsx." };
    }
}