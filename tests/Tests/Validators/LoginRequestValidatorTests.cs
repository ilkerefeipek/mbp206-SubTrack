using FluentValidation.TestHelper;
using SubTrack.Api.Contracts;
using SubTrack.Api.Validators;

namespace SubTrack.Tests.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void ValidInput_Passes()
    {
        var request = new LoginRequest("user@example.com", "anything");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyEmail_Fails()
    {
        var request = new LoginRequest("", "password");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.Email);
    }

    [Fact]
    public void MalformedEmail_Fails()
    {
        var request = new LoginRequest("notanemail", "password");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.Email);
    }

    [Fact]
    public void EmptyPassword_Fails()
    {
        var request = new LoginRequest("user@example.com", "");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(r => r.Password);
    }

    [Fact]
    public void ShortPasswordAccepted_LegacyUsersStillLogIn()
    {
        // No minimum length for login (S1 users with old passwords should still work).
        var request = new LoginRequest("user@example.com", "short");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(r => r.Password);
    }
}
