using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Services.Importacao;

public partial class DadosJsonImportService
{
    private async Task<bool> ImportarRestanteAsync(JsonDocument data, Organizacao org,
        Dictionary<string, Regiao> empByNome, string adminUser, string adminPassword, string adminEmail)
    {
        var root = data.RootElement;

        Regiao ResolverRegiao(string? nome)
        {
            var key = (nome ?? "").ToUpperInvariant();
            if (empByNome.TryGetValue(key, out var reg)) return reg;
            var primeira = _db.Regioes.FirstOrDefault(x => x.OrganizacaoId == org.Id);
            if (primeira is null) throw new InvalidOperationException($"Sem região para {nome}");
            return primeira;
        }

        if (root.TryGetProperty("vendas", out var vendasEl) && vendasEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var v in vendasEl.EnumerateArray())
            {
                var reg = ResolverRegiao(GetString(v, "regiao"));
                var construtora = GetString(v, "construtora") ?? GetString(v, "empreendimento") ?? "";
                var legadoId = GetString(v, "id") ?? "";
                var venda = await _db.Vendas.FirstOrDefaultAsync(x =>
                    x.OrganizacaoId == org.Id && x.LegadoId == legadoId);
                if (venda is null)
                {
                    venda = new Venda { Organizacao = org, LegadoId = legadoId, CriadoEm = DateTime.UtcNow };
                    _db.Vendas.Add(venda);
                }
                venda.Regiao = reg;
                venda.Numero = GetString(v, "numero") ?? "";
                venda.DataAssinatura = ParseDate(GetString(v, "dataAssinatura"));
                venda.DataEntrada = ParseDate(GetString(v, "dataEntrada"));
                venda.Mes = GetString(v, "mes");
                venda.Cliente = GetString(v, "cliente") ?? "";
                venda.Telefone = GetString(v, "telefone") ?? "";
                venda.Construtora = construtora;
                venda.Produto = GetString(v, "produto") ?? "";
                venda.BlocoQuadra = GetString(v, "blocoQuadra") ?? "";
                venda.Unidade = GetString(v, "unidade") ?? "";
                venda.Valor = GetDecimal(v, "valor");
                venda.Percentual = GetDecimalNullable(v, "percentual");
                venda.Comissao = GetDecimal(v, "comissao");
                venda.Corretor = GetString(v, "corretor") ?? "";
                venda.Origem = GetString(v, "origem") ?? "";
                venda.Financiado = GetString(v, "financiado") ?? "";
                venda.Exemplo = GetBool(v, "exemplo");
                venda.AtualizadoEm = DateTime.UtcNow;
            }
        }
        await _db.SaveChangesAsync();

        if (root.TryGetProperty("vendasPerdidas", out var perdEl) && perdEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in perdEl.EnumerateArray())
            {
                var construtora = GetString(p, "construtora") ?? GetString(p, "empreendimento") ?? "";
                var legadoId = GetString(p, "id") ?? "";
                var perd = await _db.VendasPerdidas.FirstOrDefaultAsync(x =>
                    x.OrganizacaoId == org.Id && x.LegadoId == legadoId);
                if (perd is null)
                {
                    perd = new VendaPerdida { Organizacao = org, LegadoId = legadoId };
                    _db.VendasPerdidas.Add(perd);
                }
                perd.Mes = GetString(p, "mes") ?? "";
                perd.MesNum = GetInt(p, "mesNum") ?? GetInt(p, "mes_num") ?? 0;
                perd.Ano = GetInt(p, "ano") ?? 0;
                perd.Cliente = GetString(p, "cliente") ?? "";
                perd.Telefone = GetString(p, "telefone") ?? "";
                perd.Construtora = construtora;
                perd.Produto = GetString(p, "produto") ?? "";
                perd.Corretor = GetString(p, "corretor") ?? "";
                perd.Motivo = GetString(p, "motivo") ?? "";
                perd.Financiado = GetString(p, "financiado") ?? "";
                perd.Estrategia = GetString(p, "estrategia") ?? "";
            }
        }
        await _db.SaveChangesAsync();

        await ImportarFontesAsync(root, org, ResolverRegiao);
        await GarantirAdminAsync(org, adminUser, adminPassword, adminEmail);
        return true;
    }
}