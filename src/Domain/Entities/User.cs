using SubTrack.Domain.Common;

namespace SubTrack.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int ThresholdDays { get; set; } = 30;
    public string PreferredCurrency { get; set; } = "TRY";
    public bool IsVerified { get; set; } = false;

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
