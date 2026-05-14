using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubTrack.Domain.Entities;

namespace SubTrack.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Message).HasMaxLength(2000).IsRequired();
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(n => n.Channel).HasMaxLength(30);
        builder.Property(n => n.Priority).HasMaxLength(20);
        builder.Property(n => n.IsRead).HasDefaultValue(false);

        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Subscription)
            .WithMany(s => s.Notifications)
            .HasForeignKey(n => n.SubscriptionId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        // S1 — filtered index: only unread notifications (smaller, faster for the common query)
        builder.HasIndex(n => new { n.UserId, n.IsRead })
            .HasFilter("[IsRead] = 0")
            .HasDatabaseName("IX_Notifications_Unread");
    }
}
