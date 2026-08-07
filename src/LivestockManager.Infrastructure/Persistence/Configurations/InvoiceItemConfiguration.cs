using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.Property(ii => ii.Description)
            .HasMaxLength(500);

        builder.Property(ii => ii.Quantity)
            .HasPrecision(18, 4);

        builder.Property(ii => ii.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(ii => ii.DiscountPercent)
            .HasPrecision(5, 4);

        builder.Property(ii => ii.DiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(ii => ii.TaxPercent)
            .HasPrecision(5, 4);

        builder.Property(ii => ii.TaxAmount)
            .HasPrecision(18, 2);

        builder.Ignore(ii => ii.LineTotal);

        builder.HasOne(ii => ii.Invoice)
            .WithMany(i => i.Items)
            .HasForeignKey(ii => ii.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ii => ii.SaleItem)
            .WithMany()
            .HasForeignKey(ii => ii.SaleItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
