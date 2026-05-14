using SubTrack.Api.Contracts;

namespace SubTrack.Api.Services;

/// <summary>
/// UML Bolum 17 AuthService kontrati. ValidateAsync metodu JwtBearer middleware
/// tarafindan otomatik calistirildigi icin service'te tekrar gerekmez.
/// </summary>
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task LogoutAsync(string jti, DateTime expiresAt, CancellationToken ct = default);
}
