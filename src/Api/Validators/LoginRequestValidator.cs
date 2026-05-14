using FluentValidation;
using SubTrack.Api.Contracts;

namespace SubTrack.Api.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta gereklidir.")
            .EmailAddress().WithMessage("Gecerli bir e-posta adresi giriniz.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Sifre gereklidir.")
            .MaximumLength(128);
    }
}
