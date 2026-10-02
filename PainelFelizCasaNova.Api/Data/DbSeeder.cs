using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Data;

/// <summary>
/// Equivalente ao management command <c>ensure_bootstrap</c> do Django:
/// garante organização padrão, config, polos iniciais e usuário admin.
/// </summary>
public static class DbSeeder
{
    public static readonly string[] CamposObrigatoriosPadrao =
    {
        "cliente", "construtora", "produto", "valor", "corretor",
    };

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher<Usuario> hasher,
        string orgSlug, string orgNome, string adminUser, string adminPassword, string adminEmail)
    {
        var org = await db.Organizacoes.FirstOrDefaultAsync(x => x.Slug == orgSlug);
        if (org is null)
        {
            org = new Organizacao { Nome = orgNome, Slug = orgSlug, CriadoEm = DateTime.UtcNow };
            db.Organizacoes.Add(org);
        }
        else if (org.Nome != orgNome)
        {
            org.Nome = orgNome;
        }

        var cfg = await db.OrganizacaoConfigs.FirstOrDefaultAsync(x => x.OrganizacaoId == org.Id);
        if (cfg is null)
        {
            cfg = new OrganizacaoConfig
            {
                Organizacao = org,
                AlertaDiasSemAssinatura = 15,
                CamposObrigatorios = new List<string>(CamposObrigatoriosPadrao),
            };
            db.OrganizacaoConfigs.Add(cfg);
        }

        // Polos iniciais mínimos (podem ser criados/editados no painel)
        var polos = new[] { ("lauro-de-freitas", "Lauro de Freitas"), ("camacari", "Camaçari") };
        foreach (var (slug, nome) in polos)
        {
            var existe = await db.Regioes.AnyAsync(x => x.Organizacao == org && x.Slug == slug);
            if (!existe)
            {
                db.Regioes.Add(new Regiao { Organizacao = org, Slug = slug, Nome = nome });
            }
        }

        var usuario = await db.Usuarios.FirstOrDefaultAsync(x => x.Username == adminUser);
        var criado = usuario is null;
        if (criado)
        {
            usuario = new Usuario { Username = adminUser, Email = adminEmail };
            db.Usuarios.Add(usuario);
            usuario.PasswordHash = hasher.HashPassword(usuario, adminPassword);
        }

        usuario!.Organizacao = org;
        usuario.OrganizacaoId = org.Id; // Garante que a FK seja persistida
        usuario.Papel = Models.Papel.Admin;
        usuario.IsStaff = true;
        usuario.IsSuperUser = true;
        usuario.IsActive = true;

        await db.SaveChangesAsync();
    }

    /// <summary>Sinaliza em log quando a senha do admin é fraca/padrão (espelho do Django).</summary>
    public static bool SenhaFraca(string senha) =>
        senha is "admin123" or "admin" or "password" or "123456";
}