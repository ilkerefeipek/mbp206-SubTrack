using SubTrack.Domain.Common;

namespace SubTrack.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
    public int SortOrder { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
