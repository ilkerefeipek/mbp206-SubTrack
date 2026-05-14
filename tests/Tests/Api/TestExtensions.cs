using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SubTrack.Api.Contracts;
using SubTrack.Infrastructure.Persistence;
using SubTrack.Tests.Infrastructure;

namespace SubTrack.Tests.Api;

internal static class TestExtensions
{
    public static HttpClient CreateClientWithPartition(this SubTrackWebAppFactory factory, string? clientId = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            "X-Test-Client",
            clientId ?? $"test-{Guid.NewGuid():N}");
        return client;
    }

    public static async Task<AuthResponse> RegisterFreshAsync(this HttpClient client)
    {
        var email = $"u-{Guid.NewGuid():N}@example.com";
        var resp = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = "ValidPass123",
            FirstName = "Test",
            LastName = "User"
        });
        resp.EnsureSuccessStatusCode();
        var data = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        return data!;
    }

    public static HttpClient WithBearer(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task EnsureCategoriesSeededAsync(this SubTrackWebAppFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Categories))
        {
            await DataSeeder.SeedAsync(db);
        }
    }
}
