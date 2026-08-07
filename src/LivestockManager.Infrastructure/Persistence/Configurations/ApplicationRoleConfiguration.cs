using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Infrastructure.Identity;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.Property(r => r.Description)
            .HasMaxLength(200);

        builder.Property(r => r.Name)
            .HasMaxLength(256);

        builder.Property(r => r.NormalizedName)
            .HasMaxLength(256);

        builder.Property(r => r.ConcurrencyStamp)
            .HasMaxLength(1024);

        builder.HasIndex(r => r.NormalizedName)
            .IsUnique()
            .HasFilter("[NormalizedName] IS NOT NULL");
    }
}
