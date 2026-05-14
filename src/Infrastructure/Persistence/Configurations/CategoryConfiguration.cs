using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubTrack.Domain.Entities;

namespace SubTrack.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(c => c.Name).IsUnique();

        builder.Property(c => c.Icon).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Color).HasMaxLength(9).IsRequired();
        builder.Property(c => c.IsDefault).HasDefaultValue(false);
        builder.Property(c => c.SortOrder).HasDefaultValue(0);
    }
}
