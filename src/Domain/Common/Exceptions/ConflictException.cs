namespace SubTrack.Domain.Common.Exceptions;

/// <summary>
/// Thrown when an operation cannot complete due to a business rule conflict
/// (e.g. duplicate email on registration, deleting a category that still has
/// active subscriptions).
/// </summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
}
