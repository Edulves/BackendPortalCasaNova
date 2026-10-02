using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelFelizCasaNova.Api.Dtos;
using PainelFelizCasaNova.Api.Services;

namespace PainelFelizCasaNova.Api.Controllers;

/// <summary>
/// Autenticação JWT e reset de senha — espelho de config/urls.py
/// (CustomTokenObtainPairView, TokenRefreshView) e core/auth_views.py.
/// </summary>
[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    [AllowAnonymous]
    [HttpPost("token")]
    public async Task<IActionResult> Token([FromBody] TokenRequest request)
    {
        var result = await _auth.LoginAsync(request);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("token/refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _auth.RefreshAsync(request.Refresh);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("password-reset")]
    public async Task<IActionResult> PasswordReset([FromBody] PasswordResetRequest request)
    {
        var result = await _auth.RequestPasswordResetAsync(request);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("password-reset/confirm")]
    public async Task<IActionResult> PasswordResetConfirm([FromBody] PasswordResetConfirmRequest request)
    {
        var result = await _auth.ConfirmPasswordResetAsync(request);
        return Ok(result);
    }
}