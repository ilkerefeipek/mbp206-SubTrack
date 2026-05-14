using Microsoft.EntityFrameworkCore;
using SubTrack.Domain.Entities;
using SubTrack.Domain.Enums;

namespace SubTrack.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync(ct);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(ct);
        }

        await SeedCategoriesAsync(db, ct);
        await SeedDemoUserWithSubscriptionsAsync(db, ct);
    }

    private static async Task SeedCategoriesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Categories.AnyAsync(ct))
        {
            return;
        }

        var categories = new[]
        {
            new Category { Name = "Streaming",  Icon = "tv",         Color = "#EF4444", IsDefault = true, SortOrder = 1 },
            new Category { Name = "Muzik",      Icon = "music",      Color = "#10B981", IsDefault = true, SortOrder = 2 },
            new Category { Name = "Verimlilik", Icon = "briefcase",  Color = "#0F766E", IsDefault = true, SortOrder = 3 },
            new Category { Name = "Oyun",       Icon = "gamepad",    Color = "#F59E0B", IsDefault = true, SortOrder = 4 },
            new Category { Name = "Spor",       Icon = "activity",   Color = "#14B8A6", IsDefault = true, SortOrder = 5 }
        };

        db.Categories.AddRange(categories);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedDemoUserWithSubscriptionsAsync(AppDbContext db, CancellationToken ct)
    {
        const string demoEmail = "demo@subtrack.app";

        if (await db.Users.AnyAsync(u => u.Email == demoEmail, ct))
        {
            return;
        }

        var user = new User
        {
            Email = demoEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test1234!", 10),
            FirstName = "Demo",
            LastName = "User",
            ThresholdDays = 30,
            PreferredCurrency = "TRY",
            IsVerified = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var streaming = await db.Categories.SingleAsync(c => c.Name == "Streaming", ct);
        var music = await db.Categories.SingleAsync(c => c.Name == "Muzik", ct);
        var productivity = await db.Categories.SingleAsync(c => c.Name == "Verimlilik", ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var subs = new[]
        {
            new Subscription
            {
                UserId = user.Id, CategoryId = streaming.Id, Name = "Netflix Premium",
                Amount = 229.99m, Currency = "TRY", BillingCycle = BillingCycle.Monthly,
                NextBilling = today.AddDays(7), LastUsedDate = today.AddDays(-2),
                Status = SubscriptionStatus.Active
            },
            new Subscription
            {
                UserId = user.Id, CategoryId = music.Id, Name = "Spotify Premium",
                Amount = 59.99m, Currency = "TRY", BillingCycle = BillingCycle.Monthly,
                NextBilling = today.AddDays(14), LastUsedDate = today,
                Status = SubscriptionStatus.Active
            },
            new Subscription
            {
                UserId = user.Id, CategoryId = streaming.Id, Name = "Disney+",
                Amount = 149.99m, Currency = "TRY", BillingCycle = BillingCycle.Monthly,
                NextBilling = today.AddDays(21), LastUsedDate = today.AddDays(-45),
                Status = SubscriptionStatus.Active
            },
            new Subscription
            {
                UserId = user.Id, CategoryId = streaming.Id, Name = "YouTube Premium",
                Amount = 79.99m, Currency = "TRY", BillingCycle = BillingCycle.Monthly,
                NextBilling = today.AddDays(3), LastUsedDate = today.AddDays(-1),
                Status = SubscriptionStatus.Active
            },
            new Subscription
            {
                UserId = user.Id, CategoryId = productivity.Id, Name = "Adobe Creative Cloud",
                Amount = 1199m, Currency = "TRY", BillingCycle = BillingCycle.Yearly,
                NextBilling = today.AddMonths(11), LastUsedDate = today.AddDays(-60),
                Status = SubscriptionStatus.Active
            },
            new Subscription
            {
                UserId = user.Id, CategoryId = productivity.Id, Name = "Notion Pro",
                Amount = 12m, Currency = "USD", BillingCycle = BillingCycle.Monthly,
                NextBilling = today.AddDays(28), LastUsedDate = today,
                Status = SubscriptionStatus.Active
            },
            new Subscription
            {
                UserId = user.Id, CategoryId = productivity.Id, Name = "GitHub Pro",
                Amount = 4m, Currency = "USD", BillingCycle = BillingCycle.Monthly,
                NextBilling = today.AddDays(11), LastUsedDate = today,
                Status = SubscriptionStatus.Active
            }
        };
        db.Subscriptions.AddRange(subs);
        await db.SaveChangesAsync(ct);

        var netflix = subs[0];
        var spotify = subs[1];
        var adobe = subs[4];

        db.Payments.AddRange(
            new Payment
            {
                SubscriptionId = netflix.Id,
                Amount = 229.99m,
                Currency = "TRY",
                Method = "credit_card",
                PaymentDate = today.AddMonths(-1),
                Status = PaymentStatus.Success,
                TransactionId = "DEMO-NFX-001"
            },
            new Payment
            {
                SubscriptionId = netflix.Id,
                Amount = 229.99m,
                Currency = "TRY",
                Method = "credit_card",
                PaymentDate = today.AddMonths(-2),
                Status = PaymentStatus.Success,
                TransactionId = "DEMO-NFX-002"
            },
            new Payment
            {
                SubscriptionId = spotify.Id,
                Amount = 59.99m,
                Currency = "TRY",
                Method = "bank_transfer",
                PaymentDate = today.AddDays(-16),
                Status = PaymentStatus.Success,
                TransactionId = "DEMO-SPT-001"
            }
        );

        db.Notifications.AddRange(
            new Notification
            {
                UserId = user.Id,
                SubscriptionId = netflix.Id,
                Type = NotificationType.RenewalReminder,
                Message = "Netflix Premium aboneliginiz 7 gun icinde yenilenecek.",
                Channel = "in-app",
                Priority = "normal",
                SentAt = DateTime.UtcNow.AddDays(-1)
            },
            new Notification
            {
                UserId = user.Id,
                SubscriptionId = adobe.Id,
                Type = NotificationType.UnusedAlert,
                Message = "Adobe Creative Cloud son 60 gundur kullanilmadi. Iptal etmeyi dusunur musunuz?",
                Channel = "email",
                Priority = "high",
                SentAt = DateTime.UtcNow.AddDays(-2)
            }
        );

        db.UsageLogs.AddRange(
            new UsageLog
            {
                SubscriptionId = netflix.Id,
                AccessDate = DateTime.UtcNow.AddDays(-2),
                DurationMin = 95,
                Source = "web",
                DeviceType = "desktop",
                IpAddress = "192.0.2.10"
            },
            new UsageLog
            {
                SubscriptionId = spotify.Id,
                AccessDate = DateTime.UtcNow,
                DurationMin = 47,
                Source = "mobile",
                DeviceType = "phone",
                IpAddress = "192.0.2.11"
            },
            new UsageLog
            {
                SubscriptionId = subs[3].Id,
                AccessDate = DateTime.UtcNow.AddDays(-1),
                DurationMin = 22,
                Source = "web",
                DeviceType = "desktop",
                IpAddress = "192.0.2.12"
            }
        );

        await db.SaveChangesAsync(ct);
    }
}
