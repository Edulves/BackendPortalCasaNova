using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Services.Importacao;

public partial class DadosJsonImportService
{
    private async Task ImportarFontesAsync(JsonElement root, Organizacao org,
        Func<string?, Regiao> resolverRegiao)
    {
        if (!root.TryGetProperty("config", out var conf2)
            || !conf2.TryGetProperty("fontesCSV", out var fontes)
            || fontes.ValueKind != JsonValueKind.Array) return;

        foreach (var f in fontes.EnumerateArray())
        {
            var rn = GetString(f, "regiao") ?? "";
            var reg = rn.Length > 0 ? resolverRegiao(rn) : null;
            var legadoId = GetString(f, "id") ?? "";
            var fonte = await _db.ImportacoesCsv.FirstOrDefaultAsync(x =>
                x.OrganizacaoId == org.Id && x.LegadoId == legadoId);
            if (fonte is null)
            {
                fonte = new ImportacaoCsv { Organizacao = org, LegadoId = legadoId };
                _db.ImportacoesCsv.Add(fonte);
            }
            fonte.Label = GetString(f, "label") ?? GetString(f, "url") ?? "fonte";
            fonte.Url = GetString(f, "url") ?? "";
            fonte.Regiao = reg;
            fonte.RegiaoNome = rn;
            fonte.Auto = GetBool(f, "auto");
            fonte.MesRef = GetString(f, "mesRef") ?? GetString(f, "mes_ref");
            fonte.UltimaBusca = null;
        }
        await _db.SaveChangesAsync();
    }

    private async Task GarantirAdminAsync(Organizacao org, string adminUser,
        string adminPassword, string adminEmail)
    {
        var user = await _db.Usuarios.FirstOrDefaultAsync(u => u.Username == adminUser);
        if (user is null)
        {
            user = new Usuario { Username = adminUser, Email = adminEmail };
            _db.Usuarios.Add(user);
            user.PasswordHash = _hasher.HashPassword(user, adminPassword);
        }
        user!.Organizacao = org;
        user.Papel = Models.Papel.Admin;
        user.IsStaff = true;
        user.IsSuperUser = true;
        await _db.SaveChangesAsync();
    }
}