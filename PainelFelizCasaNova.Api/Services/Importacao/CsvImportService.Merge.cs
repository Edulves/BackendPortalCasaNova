using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Services.Importacao;

public partial class CsvImportService
{
    public async Task<(Venda? Venda, bool Conflito)> EncontrarVendaExistenteAsync(Organizacao org,
        Regiao? regiao, VendaRow row, string? mesRefFonte)
    {
        var mesRef = row.Mes ?? mesRefFonte;
        if (string.IsNullOrEmpty(mesRef) && row.DataAssinatura is { } ass)
            mesRef = ass.ToString("yyyy-MM");

        var qs = _db.Vendas.Where(v => v.OrganizacaoId == org.Id);
        qs = regiao is not null
            ? qs.Where(v => v.RegiaoId == regiao.Id)
            : qs.Where(v => v.RegiaoId == null);

        if (!string.IsNullOrEmpty(row.Numero) && !string.IsNullOrEmpty(mesRef))
        {
            var candidatos = (await qs.Where(v => v.Numero == row.Numero).ToListAsync())
                .Where(x => VendasService.MesRefDaVenda(x, null) == mesRef).ToList();
            if (candidatos.Count == 1) return (candidatos[0], false);
            if (candidatos.Count > 1) return (null, true);
        }

        if (!string.IsNullOrEmpty(row.Cliente))
        {
            var unidade = row.Unidade ?? "";
            var construtora = row.Construtora ?? "";
            var candidatos = await qs
                .Where(v => v.Cliente.ToLower() == row.Cliente.ToLower()
                    && v.Unidade == unidade && v.Construtora == construtora)
                .ToListAsync();
            if (candidatos.Count == 1) return (candidatos[0], false);
            if (candidatos.Count > 1) return (null, true);
        }

        return (null, false);
    }

    public async Task<(int Novas, int Atualizadas, int Conflitos)> MergeVendasImportadasAsync(
        Organizacao org, Regiao? regiao, List<VendaRow> rows, string? mesRefFonte)
    {
        var novas = 0;
        var atualizadas = 0;
        var conflitos = 0;

        foreach (var row in rows)
        {
            var (hit, conflito) = await EncontrarVendaExistenteAsync(org, regiao, row, mesRefFonte);
            if (conflito)
            {
                conflitos++;
                continue;
            }
            if (hit is not null)
            {
                MergeCamposPreenchidos(hit, row);
                atualizadas++;
            }
            else
            {
                var agora = DateTime.UtcNow;
                _db.Vendas.Add(new Venda
                {
                    Organizacao = org,
                    Regiao = regiao,
                    Numero = row.Numero ?? "",
                    DataAssinatura = row.DataAssinatura,
                    DataEntrada = row.DataEntrada,
                    Mes = row.Mes,
                    Cliente = row.Cliente,
                    Telefone = row.Telefone ?? "",
                    Construtora = row.Construtora ?? "",
                    Produto = row.Produto ?? "",
                    BlocoQuadra = row.BlocoQuadra ?? "",
                    Unidade = row.Unidade ?? "",
                    Valor = row.Valor ?? 0m,
                    Percentual = row.Percentual,
                    Comissao = row.Comissao ?? 0m,
                    Corretor = row.Corretor ?? "",
                    Origem = row.Origem ?? "",
                    Financiado = row.Financiado ?? "",
                    Exemplo = false,
                    CriadoEm = agora,
                    AtualizadoEm = agora,
                });
                novas++;
            }
        }
        await _db.SaveChangesAsync();
        return (novas, atualizadas, conflitos);
    }

    private static void MergeCamposPreenchidos(Venda existente, VendaRow data)
    {
        if (!string.IsNullOrEmpty(data.Numero)) existente.Numero = data.Numero;
        if (data.DataAssinatura is not null) existente.DataAssinatura = data.DataAssinatura;
        if (data.DataEntrada is not null) existente.DataEntrada = data.DataEntrada;
        if (!string.IsNullOrEmpty(data.Mes)) existente.Mes = data.Mes;
        if (!string.IsNullOrEmpty(data.Cliente)) existente.Cliente = data.Cliente;
        if (!string.IsNullOrEmpty(data.Telefone)) existente.Telefone = data.Telefone;
        if (!string.IsNullOrEmpty(data.Construtora)) existente.Construtora = data.Construtora;
        if (!string.IsNullOrEmpty(data.Produto)) existente.Produto = data.Produto;
        if (!string.IsNullOrEmpty(data.BlocoQuadra)) existente.BlocoQuadra = data.BlocoQuadra;
        if (!string.IsNullOrEmpty(data.Unidade)) existente.Unidade = data.Unidade;
        if (data.Valor is not null) existente.Valor = data.Valor.Value;
        if (data.Percentual is not null) existente.Percentual = data.Percentual;
        if (data.Comissao is not null) existente.Comissao = data.Comissao.Value;
        if (!string.IsNullOrEmpty(data.Corretor)) existente.Corretor = data.Corretor;
        if (!string.IsNullOrEmpty(data.Origem)) existente.Origem = data.Origem;
        if (!string.IsNullOrEmpty(data.Financiado)) existente.Financiado = data.Financiado;
        existente.Exemplo = false;
        existente.AtualizadoEm = DateTime.UtcNow;
    }
}