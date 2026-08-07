using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Infrastructure.Identity;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.UserName)
            .HasMaxLength(256);

        builder.Property(u => u.NormalizedUserName)
            .HasMaxLength(256);

        builder.Property(u => u.Email)
            .HasMaxLength(256);

        builder.Property(u => u.NormalizedEmail)
            .HasMaxLength(256);

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(1024);

        builder.Property(u => u.SecurityStamp)
            .HasMaxLength(1024);

        builder.Property(u => u.ConcurrencyStamp)
            .HasMaxLength(1024);

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(50);

        builder.HasIndex(u => u.CompanyId);

        builder.HasIndex(u => u.Email);

        builder.HasIndex(u => u.NormalizedUserName)
            .IsUnique()
            .HasFilter("[NormalizedUserName] IS NOT NULL");
    }
}
