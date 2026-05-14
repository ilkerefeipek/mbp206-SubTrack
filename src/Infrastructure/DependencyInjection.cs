using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubTrack.Domain.Common;
using SubTrack.Domain.Repositories;
using SubTrack.Infrastructure.Persistence;
using SubTrack.Infrastructure.Repositories;

namespace SubTrack.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers SQL Server-backed AppDbContext + all repositories + UoW.
    /// Use in normal application startup (non-Testing environment).
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        return services.AddInfrastructureRepositories();
    }

    /// <summary>
    /// Registers only repositories + UoW (no DbContext). Use from integration test
    /// fixtures that supply a separate in-memory or test-scoped DbContext.
    /// </summary>
    public static IServiceCollection AddInfrastructureRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUsageLogRepository, UsageLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
