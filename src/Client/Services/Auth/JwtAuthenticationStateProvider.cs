using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace SubTrack.Client.Services.Auth;

public sealed class JwtAuthenticationStateProvider(ILocalStorageService localStorage)
    : AuthenticationStateProvider
{
    public const string TokenKey = "subtrack_token";

    private static readonly AuthenticationState _anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await localStorage.GetItemAsync<string>(TokenKey);
        if (string.IsNullOrEmpty(token))
        {
            return _anonymous;
        }

        var claims = ParseClaimsFromJwt(token).ToList();
        if (IsExpired(claims))
        {
            await localStorage.RemoveItemAsync(TokenKey);
            return _anonymous;
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task SetAuthenticatedUserAsync(string token)
    {
        await localStorage.SetItemAsync(TokenKey, token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task ClearAuthenticatedUserAsync()
    {
        await localStorage.RemoveItemAsync(TokenKey);
        NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
    }

    private static bool IsExpired(IEnumerable<Claim> claims)
    {
        var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        if (!long.TryParse(exp, out var unix))
        {
            return false;
        }
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
        return expiresAt < DateTime.UtcNow;
    }

    internal static IEnumerable<Claim> ParseClaimsFromJwt(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return Enumerable.Empty<Claim>();
        }

        try
        {
            var bytes = ParseBase64Url(parts[1]);
            var json = Encoding.UTF8.GetString(bytes);
            var doc = JsonDocument.Parse(json);

            var list = new List<Claim>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var value = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? "",
                    JsonValueKind.Number => prop.Value.ToString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => prop.Value.ToString()
                };
                list.Add(new Claim(prop.Name, value));
            }
            return list;
        }
        catch
        {
            return Enumerable.Empty<Claim>();
        }
    }

    private static byte[] ParseBase64Url(string input)
    {
        var padded = (input.Length % 4) switch
        {
            2 => input + "==",
            3 => input + "=",
            _ => input
        };
        var normalized = padded.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(normalized);
    }
}
