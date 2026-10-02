using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelFelizCasaNova.Api.Auth;
using PainelFelizCasaNova.Api.Services;

namespace PainelFelizCasaNova.Api.Controllers;

/// <summary>
/// GET /api/alertas/
/// — Lista de alertas de completude de dados + vendas não assinadas por dias.
/// Espelho de AlertasViewSet (list).
/// </summary>
[Route("api/alertas")]
[Authorize(Policy = Politicas.PodeLer)]
public class AlertasController : BaseApiController
{
    private readonly AlertasService _alertas;

    public AlertasController(AlertasService alertas) => _alertas = alertas;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var resultado = await _alertas.CalcularAlertasAsync(OrganizacaoId.Value);
        return Ok(resultado);
    }
}