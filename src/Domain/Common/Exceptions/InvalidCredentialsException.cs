namespace SubTrack.Domain.Common.Exceptions;

/// <summary>
/// Thrown when login credentials do not match a registered user.
/// Mapped to HTTP 401 with a deliberately generic message to prevent
/// username enumeration (OWASP A07).
/// </summary>
public sealed class InvalidCredentialsException : AppException
{
    public const string GenericMessage = "E-posta veya parola hatalı.";

    public InvalidCredentialsException() : base(GenericMessage) { }
}
