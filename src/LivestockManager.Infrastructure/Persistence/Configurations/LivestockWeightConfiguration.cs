using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class LivestockWeightConfiguration : IEntityTypeConfiguration<LivestockWeight>
{
    public void Configure(EntityTypeBuilder<LivestockWeight> builder)
    {
        builder.Property(lw => lw.Weight)
            .HasPrecision(18, 4);

        builder.Property(lw => lw.Notes)
            .HasMaxLength(500);

        builder.HasIndex(lw => new { lw.LivestockId, lw.WeighedAt })
            .IsDescending(false, true);

        builder.HasOne(lw => lw.Livestock)
            .WithMany(l => l.Weights)
            .HasForeignKey(lw => lw.LivestockId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
