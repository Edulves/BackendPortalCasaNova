using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Data;
using PainelFelizCasaNova.Api.Dtos;

namespace PainelFelizCasaNova.Api.Controllers;

/// <summary>GET /api/health/ — público, espelho do HealthView do Django.</summary>
[Route("api/health")]
public class HealthController : BaseApiController
{
    private readonly AppDbContext _db;
    private readonly ILogger<HealthController> _log;

    public HealthController(AppDbContext db, ILogger<HealthController> log)
    {
        _db = db;
        _log = log;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery(Name = "deep")] string? deep = null)
    {
        var checarBanco = deep is "1" or "true" or "yes";
        var payload = new HealthResponse { Status = "ok", Service = "painel-feliz-casa-nova" };

        if (checarBanco)
        {
            try
            {
                var conn = _db.Database.GetDbConnection();
                await conn.OpenAsync();
                conn.Close();
                payload.Database = "ok";
            }
            catch (Exception exc)
            {
                _log.LogError(exc, "health_db_falhou");
                payload.Status = "degraded";
                payload.Database = "error";
                payload.DatabaseDetail = (exc.Message ?? "").Length > 200
                    ? exc.Message[..200]
                    : exc.Message;
                return StatusCode(503, payload);
            }
        }
        return Ok(payload);
    }
}