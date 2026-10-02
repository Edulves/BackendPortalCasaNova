using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Data;
using PainelFelizCasaNova.Api.Dtos;
using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Services;

/// <summary>Espelho do <c>core/services/alertas.py</c>.</summary>
public class AlertasService
{
    private static readonly Dictionary<string, string> CampoLabel = new()
    {
        ["cliente"] = "Cliente",
        ["telefone"] = "Telefone",
        ["empreendimento"] = "Construtora",
        ["construtora"] = "Construtora",
        ["regiao"] = "Região",
        ["produto"] = "Produto",
        ["valor"] = "Valor",
        ["corretor"] = "Corretor",
        ["bloco_quadra"] = "Bloco/Quadra",
        ["blocoQuadra"] = "Bloco/Quadra",
        ["unidade"] = "Unidade",
        ["origem"] = "Origem",
    };

    private readonly AppDbContext _db;

    public AlertasService(AppDbContext db) => _db = db;

    private static object? CampoValor(Venda venda, string campo) =>
        campo switch
        {
            "empreendimento" or "construtora" => venda.Construtora,
            "regiao" => venda.RegiaoId is not null ? venda.Regiao?.Nome : "",
            "blocoQuadra" or "bloco_quadra" => venda.BlocoQuadra,
            _ => venda.GetType().GetProperty(campo)?.GetValue(venda),
        };

    public static List<string> CamposFaltando(Venda venda, List<string> obrigatorios)
    {
        var faltando = new List<string>();
        foreach (var campo in obrigatorios)
        {
            var val = CampoValor(venda, campo);
            if (val is null || (val is string s && string.IsNullOrWhiteSpace(s)))
            {
                faltando.Add(campo);
                continue;
            }
            if (campo == "valor" && (decimal)val <= 0)
            {
                faltando.Add(campo);
            }
        }
        return faltando;
    }

    public async Task<List<AlertaOutput>> CalcularAlertasAsync(int organizacaoId, DateOnly? hoje = null)
    {
        hoje ??= DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var organizacao = await _db.Organizacoes
            .Include(o => o.Config)
            .FirstOrDefaultAsync(o => o.Id == organizacaoId);

        var limite = organizacao?.Config?.AlertaDiasSemAssinatura ?? 15;
        var obrigatorios = organizacao?.Config?.CamposObrigatorios is { Count: > 0 } c
            ? c
            : new List<string> { "cliente", "construtora", "produto", "valor", "corretor" };

        var vendas = await _db.Vendas
            .Where(v => v.OrganizacaoId == organizacaoId)
            .Include(v => v.Regiao)
            .OrderByDescending(v => v.DataAssinatura)
            .ThenByDescending(v => v.Id)
            .ToListAsync();

        var alertas = new List<AlertaOutput>();

        foreach (var v in vendas)
        {
            if (v.DataAssinatura is null && v.DataEntrada is { } entrada)
            {
                var dias = hoje.Value.DayNumber - entrada.DayNumber;
                if (dias >= limite)
                {
                    alertas.Add(new AlertaOutput
                    {
                        Tipo = "assinatura",
                        Sev = dias >= limite * 2 ? "high" : "mid",
                        Titulo = $"{v.Cliente ?? "Cliente sem nome"} — {dias} dias sem assinatura",
                        Detalhe = $"{v.Construtora ?? "—"} · {v.Produto ?? "—"} · corretor {v.Corretor ?? "—"}"
                            + (v.Exemplo ? "  (exemplo — edite ou exclua)" : ""),
                        VendaId = v.Id,
                    });
                }
            }

            var faltando = CamposFaltando(v, obrigatorios);
            if (faltando.Count > 0)
            {
                var labels = string.Join(", ", faltando.Select(f => CampoLabel.GetValueOrDefault(f, f)));
                var nome = !string.IsNullOrWhiteSpace(v.Cliente)
                    ? v.Cliente
                    : $"Venda #{v.Numero ?? "?"}";
                alertas.Add(new AlertaOutput
                {
                    Tipo = "dados",
                    Sev = "mid",
                    Titulo = $"{nome} — falta preencher {labels}",
                    Detalhe = $"{v.Construtora ?? "—"} · {(v.Regiao is not null ? v.Regiao.Nome : "sem região")}"
                        + (v.DataAssinatura is { } ass ? $" · assinado em {ass:dd/MM/yyyy}" : ""),
                    VendaId = v.Id,
                });
            }
        }

        return alertas.OrderBy(a => a.Sev == "high" ? 0 : 1).ToList();
    }
}