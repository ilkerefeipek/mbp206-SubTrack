using FluentValidation;
using SubTrack.Api.Contracts.Payments;

namespace SubTrack.Api.Validators;

public sealed class PaymentCreateRequestValidator : AbstractValidator<PaymentCreateRequest>
{
    public PaymentCreateRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Tutar 0'dan büyük olmalı.")
            .PrecisionScale(10, 2, ignoreTrailingZeros: true);

        RuleFor(x => x.Method)
            .NotEmpty().WithMessage("Ödeme yöntemi gereklidir.")
            .MaximumLength(50);

        RuleFor(x => x.PaymentDate)
            .NotEqual(default(DateOnly)).WithMessage("Ödeme tarihi gereklidir.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Ödeme tarihi gelecekte olamaz.");

        RuleFor(x => x.Currency)
            .Length(3).When(x => !string.IsNullOrEmpty(x.Currency));

        RuleFor(x => x.TransactionId)
            .MaximumLength(200).When(x => x.TransactionId is not null);
    }
}
