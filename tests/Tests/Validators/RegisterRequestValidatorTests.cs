using FluentAssertions;
using FluentValidation.TestHelper;
using SubTrack.Api.Contracts;
using SubTrack.Api.Validators;

namespace SubTrack.Tests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void ValidInput_Passes()
    {
        var request = new RegisterRequest("user@example.com", "ValidPass123", "Ada", "Lovelace");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@no-local-part.com")]
    public void InvalidEmail_Fails(string email)
    {
        var request = new RegisterRequest(email, "ValidPass123", "Ada", "Lovelace");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("nodigitshere")]
    [InlineData("12345678")]
    public void WeakPassword_Fails(string password)
    {
        var request = new RegisterRequest("user@example.com", password, "Ada", "Lovelace");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.Password);
    }

    [Fact]
    public void EmptyFirstName_Fails()
    {
        var request = new RegisterRequest("user@example.com", "ValidPass123", "", "Lovelace");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.FirstName);
    }

    [Fact]
    public void FirstNameWithDigits_Fails()
    {
        var request = new RegisterRequest("user@example.com", "ValidPass123", "Ada123", "Lovelace");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.FirstName);
    }

    [Fact]
    public void TurkishCharactersInName_Pass()
    {
        var request = new RegisterRequest("user@example.com", "ValidPass123", "Şule", "Çelik");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(r => r.FirstName);
        result.ShouldNotHaveValidationErrorFor(r => r.LastName);
    }
}
