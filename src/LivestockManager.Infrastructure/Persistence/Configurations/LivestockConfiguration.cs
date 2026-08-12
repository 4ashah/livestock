using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class LivestockConfiguration : IEntityTypeConfiguration<Livestock>
{
    public void Configure(EntityTypeBuilder<Livestock> builder)
    {
        builder.Property(l => l.LivestockId)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.InitialWeight)
            .HasPrecision(18, 4);

        builder.Property(l => l.CurrentWeight)
            .HasPrecision(18, 4);

        builder.Property(l => l.PurchaseAmount)
            .HasPrecision(18, 2);

        builder.Property(l => l.SoldAmount)
            .HasPrecision(18, 2);

        builder.Ignore(l => l.BasicProfitLoss);

        builder.Property(l => l.Comments)
            .HasMaxLength(2000);

        builder.Property(l => l.DischargeDetails)
            .HasMaxLength(2000);

        builder.HasIndex(l => new { l.CompanyId, l.LivestockId })
            .IsUnique();

        builder.HasIndex(l => new { l.CompanyId, l.StockSource });

        builder.HasIndex(l => l.MotherLivestockId);

        builder.HasIndex(l => l.FatherLivestockId);

        builder.HasIndex(l => l.DateOfBirth);

        builder.HasIndex(l => l.AcquisitionDate);

        builder.HasOne(l => l.Company)
            .WithMany(c => c.Livestock)
            .HasForeignKey(l => l.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Farm)
            .WithMany(f => f.Livestock)
            .HasForeignKey(l => l.FarmId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Mother)
            .WithMany()
            .HasForeignKey(l => l.MotherLivestockId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Father)
            .WithMany()
            .HasForeignKey(l => l.FatherLivestockId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(l => l.Weights)
            .WithOne(w => w.Livestock)
            .HasForeignKey(w => w.LivestockId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
