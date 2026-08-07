using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.Property(i => i.InvoiceNumber)
            .HasMaxLength(50);

        builder.Property(i => i.Subtotal)
            .HasPrecision(18, 2);

        builder.Property(i => i.DiscountTotal)
            .HasPrecision(18, 2);

        builder.Property(i => i.TaxTotal)
            .HasPrecision(18, 2);

        builder.Property(i => i.GrandTotal)
            .HasPrecision(18, 2);

        builder.Property(i => i.PaidAmount)
            .HasPrecision(18, 2);

        builder.Ignore(i => i.OutstandingAmount);

        builder.Property(i => i.CustomerSnapshot)
            .HasMaxLength(4000);

        builder.Property(i => i.CompanySnapshot)
            .HasMaxLength(4000);

        builder.Property(i => i.Notes)
            .HasMaxLength(2000);

        builder.Property(i => i.Terms)
            .HasMaxLength(2000);

        builder.Property(i => i.PdfBlobName)
            .HasMaxLength(500);

        builder.HasIndex(i => new { i.CompanyId, i.InvoiceNumber })
            .IsUnique()
            .HasFilter("[InvoiceNumber] IS NOT NULL");

        builder.HasOne(i => i.Sale)
            .WithMany(s => s.Invoices)
            .HasForeignKey(i => i.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Customer)
            .WithMany(c => c.Invoices)
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Company)
            .WithMany(c => c.Invoices)
            .HasForeignKey(i => i.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
