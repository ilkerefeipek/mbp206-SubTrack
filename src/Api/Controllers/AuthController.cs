using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SubTrack.Api.Contracts;
using SubTrack.Api.Services;

namespace SubTrack.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct)
    {
        var response = await authService.RegisterAsync(request, ct);
        return Created($"/api/users/{response.User.Id}", response);
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var response = await authService.LoginAsync(request, ct);
        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var expClaim = User.FindFirstValue(JwtRegisteredClaimNames.Exp);

        if (jti is not null && expClaim is not null && long.TryParse(expClaim, out var expUnix))
        {
            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
            await authService.LogoutAsync(jti, expiresAt, ct);
        }

        return Ok(new { message = "Cikis yapildi." });
    }
}
