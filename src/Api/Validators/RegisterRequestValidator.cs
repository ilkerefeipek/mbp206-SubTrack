using FluentValidation;
using SubTrack.Api.Contracts;

namespace SubTrack.Api.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta gereklidir.")
            .EmailAddress().WithMessage("Gecerli bir e-posta adresi giriniz.")
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Sifre gereklidir.")
            .MinimumLength(8).WithMessage("Sifre en az 8 karakter olmalidir.")
            .MaximumLength(128).WithMessage("Sifre en fazla 128 karakter olabilir.")
            .Matches(@"(?=.*[A-Za-z])(?=.*\d)")
            .WithMessage("Sifre en az 1 harf ve 1 rakam icermelidir.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad gereklidir.")
            .MaximumLength(100)
            .Matches(@"^[\p{L}\s'\-]+$")
            .WithMessage("Ad sadece harf, bosluk, tire ve kesme icerebilir.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad gereklidir.")
            .MaximumLength(100)
            .Matches(@"^[\p{L}\s'\-]+$")
            .WithMessage("Soyad sadece harf, bosluk, tire ve kesme icerebilir.");
    }
}
