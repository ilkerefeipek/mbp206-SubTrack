using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SubTrack.Api.Configuration;
using SubTrack.Api.Services;
using SubTrack.Domain.Entities;

namespace SubTrack.Tests.Services;

public class JwtTokenServiceTests
{
    private static JwtOptions ValidOptions(int expirationMinutes = 1440) => new()
    {
        Issuer = "SubTrack",
        Audience = "SubTrack",
        ExpirationMinutes = expirationMinutes,
        Key = "test-jwt-key-not-for-production-aaaaaaaaaaaaaaaaaaaaaaaaaa"
    };

    private static User TestUser() => new()
    {
        Id = 42,
        Email = "alice@example.com",
        PasswordHash = "$2a$10$irrelevantForTokenService",
        FirstName = "Alice",
        LastName = "Smith",
        ThresholdDays = 30,
        PreferredCurrency = "TRY",
        IsVerified = true
    };

    private static JwtTokenService Sut(JwtOptions? opts = null) =>
        new(Options.Create(opts ?? ValidOptions()));

    [Fact]
    public void GenerateToken_ContainsExpectedClaims()
    {
        var token = Sut().GenerateToken(TestUser()).Token;
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().ContainSingle(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "42");
        jwt.Claims.Should().ContainSingle(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "alice@example.com");
        jwt.Claims.Should().ContainSingle(c => c.Type == JwtRegisteredClaimNames.Jti);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
    }

    [Fact]
    public void GenerateToken_RoundTripsThroughValidateToken()
    {
        var sut = Sut();
        var result = sut.GenerateToken(TestUser());

        var principal = sut.ValidateToken(result.Token);

        principal.Should().NotBeNull();
        principal!.FindFirst(JwtRegisteredClaimNames.Jti)!.Value.Should().Be(result.Jti);
    }

    [Fact]
    public void GenerateToken_ExpiresAt_MatchesConfiguredMinutes()
    {
        var minutes = 120;
        var sut = Sut(ValidOptions(minutes));

        var result = sut.GenerateToken(TestUser());

        result.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(minutes), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ValidateToken_TamperedKey_ReturnsNull()
    {
        var signer = Sut(ValidOptions());
        var verifier = Sut(new JwtOptions
        {
            Issuer = "SubTrack",
            Audience = "SubTrack",
            ExpirationMinutes = 1440,
            Key = "different-key-totally-different-bbbbbbbbbbbbbbbbbbbbbbbbbb"
        });

        var token = signer.GenerateToken(TestUser()).Token;
        verifier.ValidateToken(token).Should().BeNull();
    }

    [Fact]
    public void ValidateToken_MalformedToken_ReturnsNull()
    {
        Sut().ValidateToken("not.a.real.token").Should().BeNull();
    }

    [Fact]
    public void GenerateToken_NoKey_Throws()
    {
        var sut = Sut(new JwtOptions { Issuer = "SubTrack", Audience = "SubTrack", ExpirationMinutes = 60, Key = "" });

        var act = () => sut.GenerateToken(TestUser());
        act.Should().Throw<InvalidOperationException>();
    }
}
