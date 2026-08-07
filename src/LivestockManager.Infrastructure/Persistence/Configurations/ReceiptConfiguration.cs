using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.Property(r => r.ReceiptNumber).HasMaxLength(50);
        builder.Property(r => r.CustomerName).HasMaxLength(200);
        builder.Property(r => r.CustomerAddress).HasMaxLength(500);
        builder.Property(r => r.CustomerTaxNumber).HasMaxLength(50);
        builder.Property(r => r.AmountReceived).HasPrecision(18, 2);
        builder.Property(r => r.RunningInvoiceBalance).HasPrecision(18, 2);
        builder.Property(r => r.Notes).HasMaxLength(2000);
        builder.Property(r => r.ReversalReason).HasMaxLength(1000);

        builder.HasIndex(r => new { r.CompanyId, r.ReceiptNumber })
            .IsUnique()
            .HasFilter("[ReceiptNumber] IS NOT NULL");

        builder.HasOne(r => r.Company)
            .WithMany()
            .HasForeignKey(r => r.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Payment)
            .WithMany()
            .HasForeignKey(r => r.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
