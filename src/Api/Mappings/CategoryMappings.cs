using SubTrack.Api.Contracts.Categories;
using SubTrack.Domain.Entities;

namespace SubTrack.Api.Mappings;

public static class CategoryMappings
{
    public static CategoryDto ToDto(this Category c) =>
        new(c.Id, c.Name, c.Icon, c.Color, c.IsDefault, c.SortOrder);
}
