using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(c => c.CustomerCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(c => c.TaxNumber)
            .HasMaxLength(100);

        builder.Property(c => c.Phone)
            .HasMaxLength(50);

        builder.Property(c => c.Email)
            .HasMaxLength(256);

        builder.Property(c => c.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(c => new { c.CompanyId, c.CustomerCode })
            .IsUnique();

        builder.OwnsOne(c => c.BillingAddress);
        builder.OwnsOne(c => c.DeliveryAddress);

        builder.HasOne(c => c.Company)
            .WithMany(c => c.Customers)
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
