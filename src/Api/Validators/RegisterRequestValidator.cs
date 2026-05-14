using FluentValidation;
using SubTrack.Api.Contracts;

namespace SubTrack.Api.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta gereklidir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre gereklidir.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.")
            .MaximumLength(128).WithMessage("Şifre en fazla 128 karakter olabilir.")
            .Matches(@"(?=.*[A-Za-z])(?=.*\d)")
            .WithMessage("Şifre en az 1 harf ve 1 rakam içermelidir.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad gereklidir.")
            .MaximumLength(100)
            .Matches(@"^[\p{L}\s'\-]+$")
            .WithMessage("Ad sadece harf, boşluk, tire ve kesme içerebilir.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad gereklidir.")
            .MaximumLength(100)
            .Matches(@"^[\p{L}\s'\-]+$")
            .WithMessage("Soyad sadece harf, boşluk, tire ve kesme içerebilir.");
    }
}
