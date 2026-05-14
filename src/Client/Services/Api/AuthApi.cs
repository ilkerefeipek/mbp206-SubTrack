using SubTrack.Client.Models;

namespace SubTrack.Client.Services.Api;

public interface IAuthApi
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
}

public sealed class AuthApi(HttpClient http) : ApiClientBase(http), IAuthApi
{
    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default) =>
        PostAsync<AuthResponse>("/api/auth/login", request, ct);

    public Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default) =>
        PostAsync<AuthResponse>("/api/auth/register", request, ct);

    public Task LogoutAsync(CancellationToken ct = default) =>
        PostAsync("/api/auth/logout", body: null, ct);
}
