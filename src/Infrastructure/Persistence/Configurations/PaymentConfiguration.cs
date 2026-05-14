using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubTrack.Domain.Entities;

namespace SubTrack.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasPrecision(10, 2);
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.Method).HasMaxLength(50).IsRequired();
        builder.Property(p => p.TransactionId).HasMaxLength(200);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(p => p.Subscription)
            .WithMany(s => s.Payments)
            .HasForeignKey(p => p.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        // S1 — covering index: queries "payments for subscription, latest first, sum amount"
        builder.HasIndex(p => new { p.SubscriptionId, p.PaymentDate })
            .IsDescending(false, true)
            .IncludeProperties(p => p.Amount)
            .HasDatabaseName("IX_Payments_Sub_Date_Amount");
    }
}
