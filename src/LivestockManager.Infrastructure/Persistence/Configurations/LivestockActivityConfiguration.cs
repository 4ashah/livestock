using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Persistence.Configurations;

public class LivestockActivityConfiguration : IEntityTypeConfiguration<LivestockActivity>
{
    public void Configure(EntityTypeBuilder<LivestockActivity> builder)
    {
        builder.Property(la => la.Description)
            .HasMaxLength(2000);

        builder.HasOne(la => la.Livestock)
            .WithMany(l => l.Activities)
            .HasForeignKey(la => la.LivestockId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
