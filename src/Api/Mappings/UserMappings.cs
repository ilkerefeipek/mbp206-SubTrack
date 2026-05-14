using SubTrack.Api.Contracts;
using SubTrack.Domain.Entities;

namespace SubTrack.Api.Mappings;

public static class UserMappings
{
    public static UserDto ToDto(this User user) => new(
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        user.ThresholdDays,
        user.PreferredCurrency,
        user.IsVerified,
        user.CreatedAt);
}
