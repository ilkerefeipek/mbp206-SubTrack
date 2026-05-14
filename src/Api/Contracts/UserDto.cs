namespace SubTrack.Api.Contracts;

/// <summary>
/// Public user shape returned to clients. PasswordHash and internal flags are
/// intentionally excluded — see SubTrack.Api.Mappings.UserMappings.ToDto.
/// </summary>
public sealed record UserDto(
    long Id,
    string Email,
    string FirstName,
    string LastName,
    int ThresholdDays,
    string PreferredCurrency,
    bool IsVerified,
    DateTime CreatedAt);
