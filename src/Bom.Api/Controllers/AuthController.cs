using Bom.Application.Dtos;
using Bom.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bom.Api.Controllers;

/// <summary>Login single-admin (lihat <see cref="IAuthService"/>) dan ganti password.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service) => _service = service;

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request)
    {
        var result = await _service.LoginAsync(request);
        if (result is null)
            return Unauthorized(new { message = "Username atau password salah." });

        return Ok(result);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequestDto request)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        try
        {
            await _service.ChangePasswordAsync(username, request);
            return NoContent();
        }
        catch (BomServiceException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }
}
