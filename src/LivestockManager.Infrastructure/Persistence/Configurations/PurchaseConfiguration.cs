using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.Property(p => p.PurchaseNumber).HasMaxLength(50);
        builder.Property(p => p.SupplierReference).HasMaxLength(200);
        builder.Property(p => p.DiscountPct).HasPrecision(5, 2);
        builder.Property(p => p.TaxRate).HasPrecision(5, 2);
        builder.Property(p => p.Subtotal).HasPrecision(18, 2);
        builder.Property(p => p.TaxTotal).HasPrecision(18, 2);
        builder.Property(p => p.GrandTotal).HasPrecision(18, 2);
        builder.Property(p => p.AmountPaid).HasPrecision(18, 2);
        builder.Ignore(p => p.OutstandingAmount);
        builder.Property(p => p.PaymentStatus).HasMaxLength(50);
        builder.Property(p => p.Notes).HasMaxLength(2000);

        builder.HasIndex(p => new { p.CompanyId, p.PurchaseNumber })
            .IsUnique()
            .HasFilter("[PurchaseNumber] IS NOT NULL");

        builder.HasOne(p => p.Company)
            .WithMany()
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Farm)
            .WithMany()
            .HasForeignKey(p => p.FarmId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Supplier)
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Document)
            .WithMany()
            .HasForeignKey(p => p.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Items)
            .WithOne(i => i.Purchase)
            .HasForeignKey(i => i.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
