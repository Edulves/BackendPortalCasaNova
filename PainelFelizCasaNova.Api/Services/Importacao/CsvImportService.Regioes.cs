using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Services.Importacao;

public partial class CsvImportService
{
    public async Task<Dictionary<string, object>> LimparVendasAsync(int orgId, Regiao? regiao)
    {
        var vendas = _db.Vendas.Where(v => v.OrganizacaoId == orgId);
        var perdidas = _db.VendasPerdidas.Where(v => v.OrganizacaoId == orgId);
        if (regiao is not null)
        {
            var alvo = await vendas.Where(v => v.RegiaoId == regiao.Id).ToListAsync();
            _db.Vendas.RemoveRange(alvo);
            await _db.SaveChangesAsync();
            return new Dictionary<string, object>
            {
                ["vendas"] = alvo.Count, ["perdidas"] = 0, ["regiao"] = regiao.Nome,
            };
        }
        var tudoVendas = await vendas.ToListAsync();
        var tudoPerdidas = await perdidas.ToListAsync();
        _db.Vendas.RemoveRange(tudoVendas);
        _db.VendasPerdidas.RemoveRange(tudoPerdidas);
        await _db.SaveChangesAsync();
        return new Dictionary<string, object>
        {
            ["vendas"] = tudoVendas.Count, ["perdidas"] = tudoPerdidas.Count, ["regiao"] = null,
        };
    }

    private static VendaRow AplicarRegiao(VendaRow row, Regiao? regiaoPadrao, Func<string, Regiao> resolver)
    {
        var regiao = row.Regiao ?? regiaoPadrao;
        if (regiao is null && !string.IsNullOrEmpty(row.RegiaoNome))
            regiao = resolver(row.RegiaoNome);
        row.Regiao = regiao;
        return row;
    }

    public async Task<(int Novas, int Atualizadas)> ImportarPerdidasTabelaAsync(Organizacao org,
        List<List<object?>> linhas, int? ano)
    {
        Dictionary<string, int>? mapa = null;
        var inicio = 0;
        for (var i = 0; i < linhas.Count; i++)
        {
            var encontrado = CsvHelpers.MapaColunas(linhas[i].Select(CsvHelpers.CelulaParaTexto).ToList());
            if (encontrado is not null)
            {
                mapa = encontrado;
                inicio = i + 1;
                break;
            }
        }
        if (mapa is null) return (0, 0);

        var novas = 0;
        var atualizadas = 0;
        for (var i = inicio; i < linhas.Count; i++)
        {
            var cols = linhas[i];
            if (CsvHelpers.MapaColunas(cols.Select(CsvHelpers.CelulaParaTexto).ToList()) is not null)
                continue;

            var cliente = CsvHelpers.TitleCase(CsvHelpers.Pegar(cols, mapa, "cliente", -1));
            if (string.IsNullOrEmpty(cliente) || CsvHelpers.NormalizarCabecalho(cliente) == "cliente")
                continue;

            var mesTxt = CsvHelpers.Pegar(cols, mapa, "mes", -1);
            var mesRef = CsvHelpers.MesDaFolha(mesTxt, ano);
            var mesNum = mesRef is not null && mesRef.Length >= 5 ? int.Parse(mesRef[5..7]) : 0;
            var anoRow = mesRef is not null ? int.Parse(mesRef[..4]) : (ano ?? 0);
            var construtora = CsvHelpers.Pegar(cols, mapa, "construtora", -1).ToUpperInvariant();
            var produto = CsvHelpers.TitleCase(CsvHelpers.Pegar(cols, mapa, "produto", -1));

            var telefone = CsvHelpers.Pegar(cols, mapa, "telefone", -1);
            var corretor = CsvHelpers.TitleCase(CsvHelpers.Pegar(cols, mapa, "corretor", -1));
            var motivo = CsvHelpers.Pegar(cols, mapa, "motivo", -1);
            var financiado = CsvHelpers.TitleCase(CsvHelpers.Pegar(cols, mapa, "financiado", -1));
            var mesCampo = (string.IsNullOrEmpty(mesTxt) ? (mesRef ?? "") : mesTxt).Trim();

            var hit = await _db.VendasPerdidas.FirstOrDefaultAsync(p =>
                p.OrganizacaoId == org.Id &&
                p.Cliente.ToLower() == cliente.ToLower() &&
                p.Ano == anoRow && p.MesNum == mesNum &&
                p.Construtora == construtora && p.Produto == produto);

            if (hit is not null)
            {
                if (!string.IsNullOrEmpty(telefone)) hit.Telefone = telefone;
                if (!string.IsNullOrEmpty(corretor)) hit.Corretor = corretor;
                if (!string.IsNullOrEmpty(motivo)) hit.Motivo = motivo;
                if (!string.IsNullOrEmpty(financiado)) hit.Financiado = financiado;
                if (!string.IsNullOrEmpty(mesCampo)) hit.Mes = mesCampo;
                if (string.IsNullOrEmpty(hit.Estrategia)) hit.Estrategia = "";
                atualizadas++;
            }
            else
            {
                _db.VendasPerdidas.Add(new VendaPerdida
                {
                    Organizacao = org,
                    Mes = mesCampo.Length > 0 ? mesCampo : mesNum.ToString(),
                    MesNum = mesNum,
                    Ano = anoRow,
                    Cliente = cliente,
                    Telefone = telefone,
                    Construtora = construtora,
                    Produto = produto,
                    Corretor = corretor,
                    Motivo = motivo,
                    Financiado = financiado,
                });
                novas++;
            }
        }
        await _db.SaveChangesAsync();
        return (novas, atualizadas);
    }
}