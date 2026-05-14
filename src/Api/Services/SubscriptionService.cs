using SubTrack.Api.Contracts.Subscriptions;
using SubTrack.Api.Mappings;
using SubTrack.Domain.Common;
using SubTrack.Domain.Common.Exceptions;
using SubTrack.Domain.Entities;
using SubTrack.Domain.Enums;

namespace SubTrack.Api.Services;

public sealed class SubscriptionService(
    IUnitOfWork uow,
    ICurrentUserService currentUser) : ISubscriptionService
{
    public async Task<IReadOnlyList<SubscriptionListItemDto>> ListAsync(
        SubscriptionFilters filters,
        CancellationToken ct = default)
    {
        var userId = RequireUserId();

        var subscriptions = await uow.Subscriptions.GetByUserAsync(userId, ct);
        IEnumerable<Subscription> filtered = subscriptions;

        if (filters.CategoryId.HasValue)
        {
            filtered = filtered.Where(s => s.CategoryId == filters.CategoryId.Value);
        }

        if (filters.Status.HasValue)
        {
            filtered = filtered.Where(s => s.Status == filters.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            filtered = filtered.Where(s =>
                s.Name.Contains(filters.Search, StringComparison.OrdinalIgnoreCase));
        }

        var categories = (await uow.Categories.ListAsync(ct)).ToDictionary(c => c.Id);

        return filtered
            .OrderBy(s => s.Status == SubscriptionStatus.Active ? 0 : 1)
            .ThenBy(s => s.NextBilling)
            .Select(s => s.ToListItemDto(
                categories.TryGetValue(s.CategoryId, out var c) ? c.Name : string.Empty))
            .ToList();
    }

    public async Task<SubscriptionDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var subscription = await LoadOwnedAsync(id, userId, ct);
        var category = await uow.Categories.GetByIdAsync(subscription.CategoryId, ct)
            ?? throw new EntityNotFoundException("Category", subscription.CategoryId);
        return subscription.ToDto(category);
    }

    public async Task<SubscriptionDto> CreateAsync(
        SubscriptionCreateRequest request,
        CancellationToken ct = default)
    {
        var userId = RequireUserId();

        var category = await uow.Categories.GetByIdAsync(request.CategoryId, ct)
            ?? throw new EntityNotFoundException("Category", request.CategoryId);

        var subscription = new Subscription
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Amount = request.Amount,
            Currency = string.IsNullOrWhiteSpace(request.Currency)
                ? "TRY"
                : request.Currency.ToUpperInvariant(),
            BillingCycle = request.BillingCycle,
            NextBilling = request.NextBilling,
            Status = SubscriptionStatus.Active
        };

        await uow.Subscriptions.AddAsync(subscription, ct);
        await uow.SaveChangesAsync(ct);

        return subscription.ToDto(category);
    }

    public async Task<SubscriptionDto> UpdateAsync(
        long id,
        SubscriptionUpdateRequest request,
        CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var subscription = await LoadOwnedAsync(id, userId, ct);

        if (request.CategoryId.HasValue && request.CategoryId.Value != subscription.CategoryId)
        {
            _ = await uow.Categories.GetByIdAsync(request.CategoryId.Value, ct)
                ?? throw new EntityNotFoundException("Category", request.CategoryId.Value);
        }

        subscription.ApplyUpdate(request);
        uow.Subscriptions.Update(subscription);
        await uow.SaveChangesAsync(ct);

        var category = await uow.Categories.GetByIdAsync(subscription.CategoryId, ct);
        return subscription.ToDto(category!);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var subscription = await LoadOwnedAsync(id, userId, ct);

        uow.Subscriptions.Remove(subscription);
        await uow.SaveChangesAsync(ct);
    }

    public async Task MarkAsUsedAsync(long id, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var subscription = await LoadOwnedAsync(id, userId, ct);

        subscription.LastUsedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        uow.Subscriptions.Update(subscription);
        await uow.SaveChangesAsync(ct);
    }

    private long RequireUserId() =>
        currentUser.UserId ?? throw new UnauthorizedException();

    /// <summary>
    /// Load a subscription that the user owns. Returns 404 for "not found" OR
    /// "owned by someone else" — info disclosure prevention (see CLAUDE.md Bolum 15).
    /// </summary>
    private async Task<Subscription> LoadOwnedAsync(long id, long userId, CancellationToken ct)
    {
        var subscription = await uow.Subscriptions.GetByIdAsync(id, ct);
        if (subscription is null || subscription.UserId != userId)
        {
            throw new EntityNotFoundException("Subscription", id);
        }
        return subscription;
    }
}
