using FluentValidation;
using SubTrack.Api.Contracts.Subscriptions;

namespace SubTrack.Api.Validators;

public sealed class SubscriptionUpdateRequestValidator : AbstractValidator<SubscriptionUpdateRequest>
{
    public SubscriptionUpdateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(120)
            .When(x => x.Name != null);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).When(x => x.CategoryId.HasValue);

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).PrecisionScale(10, 2, ignoreTrailingZeros: true)
            .When(x => x.Amount.HasValue);

        RuleFor(x => x.BillingCycle)
            .IsInEnum().When(x => x.BillingCycle.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum().When(x => x.Status.HasValue);
    }
}
