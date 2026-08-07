using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class SequenceCounterConfiguration : IEntityTypeConfiguration<SequenceCounter>
{
    public void Configure(EntityTypeBuilder<SequenceCounter> builder)
    {
        builder.Property(sc => sc.Prefix)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(sc => new { sc.CompanyId, sc.Prefix })
            .IsUnique();

        builder.HasOne(sc => sc.Company)
            .WithMany(c => c.SequenceCounters)
            .HasForeignKey(sc => sc.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
