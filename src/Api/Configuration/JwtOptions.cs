namespace SubTrack.Api.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "SubTrack";
    public string Audience { get; set; } = "SubTrack";
    public int ExpirationMinutes { get; set; } = 1440;
    public string Key { get; set; } = string.Empty;
}
