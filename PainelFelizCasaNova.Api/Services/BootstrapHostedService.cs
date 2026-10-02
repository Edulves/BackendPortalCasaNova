using Microsoft.AspNetCore.Identity;
using PainelFelizCasaNova.Api.Data;
using PainelFelizCasaNova.Api.Models;
using PainelFelizCasaNova.Api.Services.Importacao;

namespace PainelFelizCasaNova.Api.Services;

/// <summary>
/// Equivalente ao entrypoint.sh do Django: migrate + ensure_bootstrap,
/// e import opcional de dados.json (RUN_IMPORT_ON_START=1).
/// </summary>
public class BootstrapHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<BootstrapHostedService> _log;
    private readonly bool _usaInMemory;

    public BootstrapHostedService(IServiceScopeFactory scopeFactory, IConfiguration config,
        ILogger<BootstrapHostedService> log)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _log = log;
        _usaInMemory = (config["USE_INMEMORY"] ?? "0") == "1";
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var services = scope.ServiceProvider;

            // ── DbSeeder: cria organização, config, polos e admin ──
            await ExecutarSeedAsync(services, cancellationToken);

            // ── Import opcional de dados.json ──
            await ImportarDadosJsonOpicionalAsync(services, cancellationToken);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Falha no bootstrap da aplicação");
            throw;
        }
    }

    private async Task ExecutarSeedAsync(IServiceProvider services, CancellationToken ct)
    {
        try
        {
            var db = services.GetRequiredService<AppDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher<Usuario>>();

            var orgSlug = _config["BOOTSTRAP_ORG_SLUG"] ?? "matriz";
            var orgNome = _config["BOOTSTRAP_ORG_NOME"] ?? "Matriz";
            var adminUser = _config["BOOTSTRAP_ADMIN_USER"] ?? "admin";
            var adminPassword = _config["BOOTSTRAP_ADMIN_PASSWORD"] ?? "admin123";
            var adminEmail = _config["BOOTSTRAP_ADMIN_EMAIL"] ?? "admin@felizcasanova.com.br";

            await DbSeeder.SeedAsync(db, hasher, orgSlug, orgNome,
                adminUser, adminPassword, adminEmail);

            _log.LogInformation("Seed executado — organização/admin garantidos.");
        }
        catch (Exception ex)
        {
            _log.LogWarning("Seed ignorado (banco já populado?): " + ex.Message);
        }
        _ = ct;
    }

    private async Task ImportarDadosJsonOpicionalAsync(IServiceProvider services, CancellationToken ct)
    {
        if ((_config["RUN_IMPORT_ON_START"] ?? "0") != "1") return;

        var baseDir = AppContext.BaseDirectory;
        var file = DadosJsonImportService.Candidatos(_config["DADOS_JSON_FILE"] ?? "", baseDir)
            .FirstOrDefault(File.Exists);
        if (file is null)
        {
            _log.LogWarning("RUN_IMPORT_ON_START=1 mas dados.json não foi encontrado.");
            return;
        }

        var importer = services.GetRequiredService<DadosJsonImportService>();
        await importer.ImportarAsync(
            file,
            _config["ORG_SLUG"] ?? "feliz-casa-nova",
            _config["DJANGO_SUPERUSER_USERNAME"] ?? "admin",
            _config["DJANGO_SUPERUSER_PASSWORD"] ?? "admin123",
            _config["DJANGO_SUPERUSER_EMAIL"] ?? "admin@felizcasanova.local",
            clear: false,
            ifEmpty: true);
        _log.LogInformation("Import de dados.json concluído ({File})", file);
        _ = ct;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}