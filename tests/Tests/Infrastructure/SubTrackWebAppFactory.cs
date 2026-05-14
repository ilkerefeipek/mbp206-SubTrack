using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Tests.Infrastructure;

/// <summary>
/// In-process test host that injects test-only configuration (Jwt:Key, in-memory DB)
/// so tests do not require LocalDB or user-secrets to run.
/// </summary>
public class SubTrackWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"SubTrackTests_{Guid.NewGuid():N}";

    static SubTrackWebAppFactory()
    {
        // Set env vars BEFORE Program.cs reads builder.Configuration during WebApplication.CreateBuilder.
        // ConfigureWebHost callbacks run too late in .NET 9 minimal hosting for this purpose.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable(
            "Jwt__Key",
            "test-jwt-key-must-be-at-least-32-chars-long-for-hs256-signature");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "SubTrack");
        Environment.SetEnvironmentVariable("Jwt__Audience", "SubTrack");
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Server=unused-replaced-by-inmemory-in-ConfigureServices");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(_dbName));
        });
    }
}
