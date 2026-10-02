using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PainelFelizCasaNova.Api;
using PainelFelizCasaNova.Api.Auth;
using PainelFelizCasaNova.Api.Data;
using PainelFelizCasaNova.Api.Middleware;
using PainelFelizCasaNova.Api.Models;
using PainelFelizCasaNova.Api.Services;
using PainelFelizCasaNova.Api.Services.Importacao;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===== LOGGING =====
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (!builder.Environment.IsProduction())
    builder.Logging.AddDebug();

// ===== DATABASE =====
var useInMemory = (builder.Configuration["USE_INMEMORY"] ?? "0") == "1";
if (useInMemory)
{
    builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("PainelFcn"));
}
else
{
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string not found");
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(connStr, pgOpt => pgOpt.CommandTimeout(30)));
}

// ===== IDENTITY + PASSWORD HASHER =====
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

// ===== JWT AUTHENTICATION =====
var jwtKey = builder.Configuration["JWT_SECRET"]
    ?? throw new InvalidOperationException("JWT_SECRET not configured");
var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? "painel-feliz-casa-nova";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "painel-feliz-casa-nova";
var jwtExpiryMinutes = int.TryParse(builder.Configuration["JWT_EXPIRY_MINUTES"], out var exp) ? exp : 60;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(5),
        };
    });

// ===== AUTHORIZATION POLICIES =====
builder.Services.AddAuthorization(opt => Politicas.Registrar(opt));
builder.Services.AddScoped<IAuthorizationHandler, PermissaoHandler>();
builder.Services.AddHttpContextAccessor();

// ===== CONTROLLERS + SWAGGER =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new() { Title = "Painel Feliz Casa Nova", Version = "v1" });
    opt.AddSecurityDefinition("Bearer", new()
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme",
    });
    opt.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// ===== CORS =====
var corsOrigins = (builder.Configuration["CORS_ORIGINS"] ?? "http://localhost:8080").Split(',');
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowFrontend", pb =>
    {
        pb.WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ===== APPLICATION SERVICES =====
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PasswordResetTokenService>();
builder.Services.AddScoped<VendasService>();
builder.Services.AddScoped<ResumoService>();
builder.Services.AddScoped<AlertasService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<MailService>();
// SenhaValidator, LogoValidator e XlsxReader são classes estáticas (helpers)
// e não precisam de injeção de dependência.

builder.Services.AddHttpClient(); // Para CsvImportService poder baixar arquivos
builder.Services.AddScoped<CsvImportService>();
builder.Services.AddScoped<DadosJsonImportService>();

// ===== HOSTED SERVICES (bootstrap on startup) =====
builder.Services.AddHostedService<BootstrapHostedService>();

var app = builder.Build();

// ===== MIDDLEWARE PIPELINE =====
app.UseErrorHandlingMiddleware();
app.UseRateLimitMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opt => opt.SwaggerEndpoint("/swagger/v1/swagger.json", "Painel v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
