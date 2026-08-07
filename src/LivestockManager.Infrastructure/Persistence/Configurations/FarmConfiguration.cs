using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class FarmConfiguration : IEntityTypeConfiguration<Farm>
{
    public void Configure(EntityTypeBuilder<Farm> builder)
    {
        builder.Property(f => f.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(f => new { f.CompanyId, f.Code })
            .IsUnique();

        builder.OwnsOne(f => f.Address);

        builder.HasOne(f => f.Company)
            .WithMany(c => c.Farms)
            .HasForeignKey(f => f.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
