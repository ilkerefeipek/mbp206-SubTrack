namespace SubTrack.Domain.Common.Exceptions;

/// <summary>
/// Root of the SubTrack domain exception hierarchy. The global exception handler
/// (S2) maps these to HTTP responses; concrete subclasses indicate the failure
/// category.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
    protected AppException(string message, Exception inner) : base(message, inner) { }
}
