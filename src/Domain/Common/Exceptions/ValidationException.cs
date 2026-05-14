namespace SubTrack.Domain.Common.Exceptions;

/// <summary>
/// Thrown when input fails validation (S2 will populate this from FluentValidation
/// results in the AuthService / controllers).
/// </summary>
public sealed class ValidationException : AppException
{
    public ValidationException(IReadOnlyDictionary<string, IReadOnlyList<string>> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string field, string message)
        : this(new Dictionary<string, IReadOnlyList<string>> { [field] = new[] { message } })
    {
    }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; }
}
