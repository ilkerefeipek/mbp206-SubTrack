namespace SubTrack.Domain.Common.Exceptions;

/// <summary>
/// Thrown when an authenticated user lacks authorization for an operation.
/// Mapped to HTTP 403. Note: for cross-user resource access we deliberately
/// throw EntityNotFoundException (404) instead — info disclosure prevention,
/// see CLAUDE.md Bolum 15 owner-check decision.
/// </summary>
public sealed class ForbiddenException : AppException
{
    public ForbiddenException() : base("Bu islem icin yetkiniz yok.") { }
    public ForbiddenException(string message) : base(message) { }
}
