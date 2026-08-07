using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.Property(d => d.DisplayName).HasMaxLength(255);
        builder.Property(d => d.StoragePath).HasMaxLength(500);
        builder.Property(d => d.ContentType).HasMaxLength(100);
        builder.Property(d => d.Extension).HasMaxLength(20);
        builder.Property(d => d.EntityType).HasMaxLength(50);

        builder.HasIndex(d => new { d.CompanyId, d.EntityType, d.EntityId })
            .HasFilter("[EntityId] IS NOT NULL");

        builder.HasOne(d => d.Company)
            .WithMany()
            .HasForeignKey(d => d.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
