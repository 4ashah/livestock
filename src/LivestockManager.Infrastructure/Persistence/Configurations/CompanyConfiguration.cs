using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.RegistrationNumber)
            .HasMaxLength(100);

        builder.Property(c => c.TaxNumber)
            .HasMaxLength(100);

        builder.Property(c => c.Phone)
            .HasMaxLength(50);

        builder.Property(c => c.Email)
            .HasMaxLength(256);

        builder.Property(c => c.LogoBlobName)
            .HasMaxLength(500);

        builder.Property(c => c.InvoicePrefix)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(c => c.ReceiptPrefix)
            .HasMaxLength(10)
            .IsRequired();

        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.OwnsOne(c => c.Address);
    }
}
