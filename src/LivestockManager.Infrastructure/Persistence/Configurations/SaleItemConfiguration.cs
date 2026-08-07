using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.Property(si => si.Description)
            .HasMaxLength(500);

        builder.Property(si => si.Quantity)
            .HasPrecision(18, 4);

        builder.Property(si => si.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(si => si.DiscountPercent)
            .HasPrecision(5, 4);

        builder.Property(si => si.DiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(si => si.TaxPercent)
            .HasPrecision(5, 4);

        builder.Property(si => si.TaxAmount)
            .HasPrecision(18, 2);

        builder.HasIndex(si => si.LivestockId)
            .IsUnique()
            .HasFilter("[LivestockId] IS NOT NULL");

        builder.Ignore(si => si.LineTotal);

        builder.HasOne(si => si.Sale)
            .WithMany(s => s.Items)
            .HasForeignKey(si => si.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(si => si.Livestock)
            .WithOne(l => l.SaleItem)
            .HasForeignKey<SaleItem>(si => si.LivestockId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
