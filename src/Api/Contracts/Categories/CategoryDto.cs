namespace SubTrack.Api.Contracts.Categories;

public sealed record CategoryDto(
    long Id,
    string Name,
    string Icon,
    string Color,
    bool IsDefault,
    int SortOrder);
