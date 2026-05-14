using SubTrack.Api.Contracts.Subscriptions;
using SubTrack.Domain.Entities;

namespace SubTrack.Api.Mappings;

public static class SubscriptionMappings
{
    public static SubscriptionDto ToDto(this Subscription s, Category category) => new(
        s.Id,
        s.Name,
        s.CategoryId,
        category.Name,
        s.Amount,
        s.Currency,
        s.BillingCycle,
        s.NextBilling,
        s.LastUsedDate,
        s.Status,
        s.CreatedAt);

    public static SubscriptionListItemDto ToListItemDto(this Subscription s, string categoryName) => new(
        s.Id,
        s.Name,
        s.CategoryId,
        categoryName,
        s.Amount,
        s.Currency,
        s.BillingCycle,
        s.NextBilling,
        s.LastUsedDate,
        s.Status);

    public static void ApplyUpdate(this Subscription target, SubscriptionUpdateRequest src)
    {
        if (src.Name is not null)
        {
            target.Name = src.Name;
        }

        if (src.CategoryId.HasValue)
        {
            target.CategoryId = src.CategoryId.Value;
        }

        if (src.Amount.HasValue)
        {
            target.Amount = src.Amount.Value;
        }

        if (src.BillingCycle.HasValue)
        {
            target.BillingCycle = src.BillingCycle.Value;
        }

        if (src.NextBilling.HasValue)
        {
            target.NextBilling = src.NextBilling.Value;
        }

        if (src.Status.HasValue)
        {
            target.Status = src.Status.Value;
        }
    }
}
