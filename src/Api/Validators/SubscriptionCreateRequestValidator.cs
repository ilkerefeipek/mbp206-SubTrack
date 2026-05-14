using FluentValidation;
using SubTrack.Api.Contracts.Subscriptions;

namespace SubTrack.Api.Validators;

public sealed class SubscriptionCreateRequestValidator : AbstractValidator<SubscriptionCreateRequest>
{
    public SubscriptionCreateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Abonelik adi gereklidir.")
            .MaximumLength(120);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Gecerli bir kategori seciniz.");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Tutar negatif olamaz.")
            .PrecisionScale(10, 2, ignoreTrailingZeros: true);

        RuleFor(x => x.Currency)
            .Length(3).When(x => !string.IsNullOrEmpty(x.Currency))
            .WithMessage("Para birimi 3 karakter olmali (orn. TRY, USD).");

        RuleFor(x => x.BillingCycle)
            .IsInEnum().WithMessage("Gecerli bir fatura donemi seciniz.");

        RuleFor(x => x.NextBilling)
            .NotEqual(default(DateOnly)).WithMessage("Sonraki fatura tarihi gereklidir.")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Sonraki fatura tarihi gecmiste olamaz.");
    }
}
