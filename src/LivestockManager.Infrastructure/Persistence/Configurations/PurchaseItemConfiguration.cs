using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.Property(i => i.Description).HasMaxLength(500);
        builder.Property(i => i.UnitWeight).HasPrecision(18, 4);
        builder.Property(i => i.UnitCost).HasPrecision(18, 2);
        builder.Property(i => i.DiscountPct).HasPrecision(5, 2);
        builder.Property(i => i.TaxRate).HasPrecision(5, 2);
        builder.Property(i => i.LineTotal).HasPrecision(18, 2);

        builder.HasOne(i => i.Purchase)
            .WithMany(p => p.Items)
            .HasForeignKey(i => i.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Livestock)
            .WithMany()
            .HasForeignKey(i => i.LivestockId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
