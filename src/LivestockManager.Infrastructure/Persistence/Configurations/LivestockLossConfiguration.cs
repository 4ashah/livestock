using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class LivestockLossConfiguration : IEntityTypeConfiguration<LivestockLoss>
{
    public void Configure(EntityTypeBuilder<LivestockLoss> builder)
    {
        builder.Property(l => l.LossNumber).HasMaxLength(32);
        builder.Property(l => l.BookValue).HasPrecision(18, 2);
        builder.Property(l => l.SalvageValue).HasPrecision(18, 2);
        builder.Property(l => l.LossAmount).HasPrecision(18, 2);
        builder.Property(l => l.Reason).HasMaxLength(2000);
        builder.Property(l => l.Notes).HasMaxLength(2000);
        builder.Property(l => l.ReversalReason).HasMaxLength(2000);

        builder.HasIndex(l => new { l.CompanyId, l.LossNumber }).IsUnique();
        builder.HasIndex(l => new { l.CompanyId, l.LossDate });
        builder.HasIndex(l => new { l.LivestockId, l.IsReversed });

        builder.HasOne(l => l.Company)
            .WithMany()
            .HasForeignKey(l => l.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Farm)
            .WithMany()
            .HasForeignKey(l => l.FarmId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Livestock)
            .WithMany()
            .HasForeignKey(l => l.LivestockId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
