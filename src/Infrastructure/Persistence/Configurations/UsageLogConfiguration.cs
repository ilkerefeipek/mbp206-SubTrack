using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubTrack.Domain.Entities;

namespace SubTrack.Infrastructure.Persistence.Configurations;

public class UsageLogConfiguration : IEntityTypeConfiguration<UsageLog>
{
    public void Configure(EntityTypeBuilder<UsageLog> builder)
    {
        builder.ToTable("UsageLogs");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Source).HasMaxLength(50);
        builder.Property(u => u.DeviceType).HasMaxLength(50);
        builder.Property(u => u.IpAddress).HasMaxLength(45);

        builder.HasOne(u => u.Subscription)
            .WithMany(s => s.UsageLogs)
            .HasForeignKey(u => u.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(u => new { u.SubscriptionId, u.AccessDate });
    }
}
