using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(al => al.Action)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(al => al.EntityType)
            .HasMaxLength(200);

        builder.Property(al => al.EntityId)
            .HasMaxLength(100);

        builder.Property(al => al.OldValuesJson)
            .HasMaxLength(8000);

        builder.Property(al => al.NewValuesJson)
            .HasMaxLength(8000);

        builder.Property(al => al.IpAddress)
            .HasMaxLength(100);

        builder.Property(al => al.UserAgent)
            .HasMaxLength(500);
    }
}
