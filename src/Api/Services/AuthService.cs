using SubTrack.Api.Contracts;
using SubTrack.Api.Mappings;
using SubTrack.Domain.Common;
using SubTrack.Domain.Common.Exceptions;
using SubTrack.Domain.Entities;

namespace SubTrack.Api.Services;

public sealed class AuthService(
    IUnitOfWork uow,
    ITokenService tokenService,
    ITokenBlacklist blacklist,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuthService> logger) : IAuthService
{
    // Pre-computed hash used for constant-time login response when the user
    // does not exist — defends against email enumeration via timing (OWASP A07).
    private static readonly string _dummyHash =
        BCrypt.Net.BCrypt.HashPassword("dummy-for-uniform-timing", workFactor: 10);

    private string ClientIp =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.ToLowerInvariant();

        if (await uow.Users.EmailExistsAsync(normalizedEmail, ct))
        {
            logger.LogWarning(
                "Register rejected (duplicate email): {Email} from {Ip}",
                normalizedEmail,
                ClientIp);
            throw new ConflictException("Bu e-posta zaten kayitli.");
        }

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 10),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            ThresholdDays = 30,
            PreferredCurrency = "TRY",
            IsVerified = false
        };

        await uow.Users.AddAsync(user, ct);
        await uow.SaveChangesAsync(ct);

        var token = tokenService.GenerateToken(user);
        logger.LogInformation("User registered: {Email} from {Ip}", user.Email, ClientIp);

        return new AuthResponse(token.Token, token.ExpiresAt, user.ToDto());
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.ToLowerInvariant();
        var user = await uow.Users.GetByEmailAsync(normalizedEmail, ct);

        // Always perform BCrypt verify (real or dummy hash) so non-existent
        // emails take the same time as wrong-password attempts.
        var hash = user?.PasswordHash ?? _dummyHash;
        var passwordMatches = BCrypt.Net.BCrypt.Verify(request.Password, hash);

        if (user is null || !passwordMatches)
        {
            logger.LogWarning(
                "Failed login attempt: {Email} from {Ip}",
                normalizedEmail,
                ClientIp);
            throw new InvalidCredentialsException();
        }

        var token = tokenService.GenerateToken(user);
        logger.LogInformation("User logged in: {Email} from {Ip}", user.Email, ClientIp);

        return new AuthResponse(token.Token, token.ExpiresAt, user.ToDto());
    }

    public async Task LogoutAsync(string jti, DateTime expiresAt, CancellationToken ct = default)
    {
        await blacklist.AddAsync(jti, expiresAt);
        logger.LogInformation("User logged out: jti={Jti}", jti);
    }
}
