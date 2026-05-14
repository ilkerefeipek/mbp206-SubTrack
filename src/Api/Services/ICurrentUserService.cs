namespace SubTrack.Api.Services;

/// <summary>
/// Resolves the currently authenticated user from the request's JWT claims.
/// Returns null fields when no token / unauthenticated context.
/// </summary>
public interface ICurrentUserService
{
    long? UserId { get; }
    string? Email { get; }
    string? Jti { get; }
    bool IsAuthenticated { get; }
}
