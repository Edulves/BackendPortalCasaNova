using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PainelFelizCasaNova.Api.Auth;
using PainelFelizCasaNova.Api.Dtos;
using PainelFelizCasaNova.Api.Services;

namespace PainelFelizCasaNova.Api.Controllers;

/// <summary>
/// GET /api/config/
/// PATCH /api/config/
/// — Configuração da organização (logo, etc).
/// </summary>
[Route("api/config")]
[Authorize(Policy = Politicas.PodeAdministrar)]
public class ConfigController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var config = await Db.OrganizacaoConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.OrganizacaoId == OrganizacaoId.Value);

        var resp = new OrganizacaoConfigResponse
        {
            LogoBase64 = config?.LogoBase64,
            LogoMimeType = config?.LogoMimeType,
        };
        return Ok(resp);
    }

    [HttpPatch]
    public async Task<IActionResult> Update()
    {
        if (OrganizacaoId is null)
            throw new ApiException(403, "Sem organização.");

        var config = await Db.OrganizacaoConfigs
            .FirstOrDefaultAsync(c => c.OrganizacaoId == OrganizacaoId.Value);

        if (config is null)
        {
            config = new() { OrganizacaoId = OrganizacaoId.Value };
            Db.OrganizacaoConfigs.Add(config);
        }

        var (request, presentes) = await LerCorpoAsync<OrganizacaoConfigUpdateRequest>();
        if (request is null)
            throw new ApiException(400, "Corpo da requisição inválido.");
        if (presentes.Contains("logo_base64"))
        {
            if (request.LogoBase64 is not null)
            {
                try
                {
                    LogoValidator.Validar(request.LogoBase64);
                    config.LogoBase64 = request.LogoBase64;
                    config.LogoMimeType = request.LogoMimeType ?? "image/png";
                }
                catch (ValidationException ex)
                {
                    throw new ApiException(400, ex.Message, ApiCodes.ValidationError,
                        ErroCampo("logo_base64", ex.Message));
                }
            }
            else
                config.LogoBase64 = null;
        }

        await Db.SaveChangesAsync();
        return Ok(new OrganizacaoConfigResponse
        {
            LogoBase64 = config.LogoBase64,
            LogoMimeType = config.LogoMimeType,
        });
    }
}