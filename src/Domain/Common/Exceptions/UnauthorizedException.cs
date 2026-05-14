namespace SubTrack.Domain.Common.Exceptions;

/// <summary>
/// Thrown when a request lacks valid authentication (no token, expired token,
/// signature mismatch). Mapped to HTTP 401 by the global exception handler.
/// </summary>
public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException()
        : base("Bu kaynaga erisim icin kimlik dogrulamasi gereklidir.") { }

    public UnauthorizedException(string message) : base(message) { }
}
