using System.Security.Claims;
using SubTrack.Domain.Entities;

namespace SubTrack.Api.Services;

public sealed record TokenResult(string Token, DateTime ExpiresAt, string Jti);

public interface ITokenService
{
    TokenResult GenerateToken(User user);
    ClaimsPrincipal? ValidateToken(string token);
}
