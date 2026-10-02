using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Models;

namespace PainelFelizCasaNova.Api.Data;

/// <summary>DbContext principal — espelho do settings DATABASES + models.</summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Organizacao> Organizacoes => Set<Organizacao>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<OrganizacaoConfig> OrganizacaoConfigs => Set<OrganizacaoConfig>();
    public DbSet<Regiao> Regioes => Set<Regiao>();
    public DbSet<ResumoManual> ResumoManuais => Set<ResumoManual>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<VendaPerdida> VendasPerdidas => Set<VendaPerdida>();
    public DbSet<ImportacaoCsv> ImportacoesCsv => Set<ImportacaoCsv>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Organizacao>(e =>
        {
            e.ToTable("organizacoes");
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.Property(x => x.Username).HasMaxLength(150).IsRequired();
            e.Property(x => x.Email).HasMaxLength(254);
            e.Property(x => x.Papel).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Username).IsUnique();
            e.HasOne(x => x.Organizacao).WithMany(x => x.Usuarios)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrganizacaoConfig>(e =>
        {
            e.ToTable("organizacao_configs");
            e.Property(x => x.AlertaDiasSemAssinatura).IsRequired();
            e.Property(x => x.CamposObrigatorios).HasColumnName("campos_obrigatorios")
                .HasColumnType("jsonb").IsRequired()
                .HasConversion(
                    v => OrganizacaoConfig.SerializarCampos(v),
                    v => OrganizacaoConfig.DeserializarCampos(v));
            e.Property(x => x.LogoDataUrl).HasColumnType("text");
            e.HasIndex(x => x.OrganizacaoId).IsUnique();
            e.HasOne(x => x.Organizacao).WithOne(x => x.Config)
                .HasForeignKey<OrganizacaoConfig>(x => x.OrganizacaoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Regiao>(e =>
        {
            e.ToTable("regioes");
            e.Property(x => x.Slug).HasMaxLength(120).IsRequired();
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.OrganizacaoId, x.Slug }).IsUnique();
            e.HasOne(x => x.Organizacao).WithMany(x => x.Regioes)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ResumoManual).WithOne(x => x.Regiao)
                .HasForeignKey<ResumoManual>(x => x.RegiaoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ResumoManual>(e =>
        {
            e.ToTable("resumos_manuais");
            e.Property(x => x.Vgv).HasPrecision(16, 2).IsRequired();
            e.Property(x => x.Vgc).HasPrecision(16, 2).IsRequired();
            e.Property(x => x.Vendas).IsRequired();
            e.Property(x => x.Obs).HasColumnType("text");
        });

        modelBuilder.Entity<Venda>(e =>
        {
            e.ToTable("vendas");
            e.Property(x => x.LegadoId).HasMaxLength(32);
            e.HasIndex(x => x.LegadoId);
            e.Property(x => x.Numero).HasMaxLength(40);
            e.Property(x => x.DataAssinatura).HasColumnType("date");
            e.Property(x => x.DataEntrada).HasColumnType("date");
            e.Property(x => x.Mes).HasMaxLength(7);
            e.Property(x => x.Cliente).HasMaxLength(255).IsRequired();
            e.Property(x => x.Telefone).HasMaxLength(60);
            e.Property(x => x.Construtora).HasMaxLength(120);
            e.Property(x => x.Produto).HasMaxLength(120);
            e.Property(x => x.BlocoQuadra).HasMaxLength(60);
            e.Property(x => x.Unidade).HasMaxLength(60);
            e.Property(x => x.Valor).HasPrecision(14, 2).IsRequired();
            e.Property(x => x.Percentual).HasPrecision(8, 4);
            e.Property(x => x.Comissao).HasPrecision(14, 2).IsRequired();
            e.Property(x => x.Corretor).HasMaxLength(120);
            e.Property(x => x.Origem).HasMaxLength(120);
            e.Property(x => x.Financiado).HasMaxLength(120);
            e.HasIndex(x => new { x.OrganizacaoId, x.Numero, x.Mes });
            e.HasOne(x => x.Organizacao).WithMany(x => x.Vendas)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Regiao).WithMany(x => x.Vendas)
                .HasForeignKey(x => x.RegiaoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ImportacaoCsv).WithMany()
                .HasForeignKey(x => x.ImportacaoCsvId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<VendaPerdida>(e =>
        {
            e.ToTable("vendas_perdidas");
            e.Property(x => x.LegadoId).HasMaxLength(32);
            e.Property(x => x.Mes).HasMaxLength(40);
            e.Property(x => x.Cliente).HasMaxLength(255).IsRequired();
            e.Property(x => x.Telefone).HasMaxLength(60);
            e.Property(x => x.Construtora).HasMaxLength(120);
            e.Property(x => x.Produto).HasMaxLength(120);
            e.Property(x => x.Corretor).HasMaxLength(120);
            e.Property(x => x.Motivo).HasColumnType("text");
            e.Property(x => x.Financiado).HasMaxLength(120);
            e.Property(x => x.Estrategia).HasColumnType("text");
            e.HasOne(x => x.Organizacao).WithMany(x => x.VendasPerdidas)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Regiao).WithMany(x => x.VendasPerdidas)
                .HasForeignKey(x => x.RegiaoId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ImportacaoCsv>(e =>
        {
            e.ToTable("importacoes_csv");
            e.Property(x => x.LegadoId).HasMaxLength(32);
            e.Property(x => x.Label).HasMaxLength(255).IsRequired();
            e.Property(x => x.Url).HasMaxLength(1000).IsRequired();
            e.Property(x => x.RegiaoNome).HasMaxLength(200);
            e.Property(x => x.MesRef).HasMaxLength(7);
            e.HasOne(x => x.Organizacao).WithMany(x => x.FontesCsv)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Regiao).WithMany(x => x.FontesCsv)
                .HasForeignKey(x => x.RegiaoId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}