using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class InvoiceAdditionalChargeConfiguration : IEntityTypeConfiguration<InvoiceAdditionalCharge>
{
    public void Configure(EntityTypeBuilder<InvoiceAdditionalCharge> builder)
    {
        builder.Property(c => c.Description).HasMaxLength(200);
        builder.Property(c => c.Amount).HasPrecision(18, 2);
        builder.Property(c => c.TaxRate).HasPrecision(5, 2);
        builder.Property(c => c.TaxAmount).HasPrecision(18, 2);

        builder.HasOne(c => c.Invoice)
            .WithMany(i => i.AdditionalCharges)
            .HasForeignKey(c => c.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
