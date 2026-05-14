using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SubTrack.Api.Services;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public long? UserId
    {
        get
        {
            var sub = Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email =>
        Principal?.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? Principal?.FindFirstValue(ClaimTypes.Email);

    public string? Jti => Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
}
