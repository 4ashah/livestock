using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.Property(s => s.SaleNumber)
            .HasMaxLength(50);

        builder.Property(s => s.Subtotal)
            .HasPrecision(18, 2);

        builder.Property(s => s.DiscountTotal)
            .HasPrecision(18, 2);

        builder.Property(s => s.TaxTotal)
            .HasPrecision(18, 2);

        builder.Property(s => s.GrandTotal)
            .HasPrecision(18, 2);

        builder.Property(s => s.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(s => new { s.CompanyId, s.SaleNumber })
            .IsUnique()
            .HasFilter("[SaleNumber] IS NOT NULL");

        builder.HasOne(s => s.Farm)
            .WithMany(f => f.Sales)
            .HasForeignKey(s => s.FarmId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Company)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
