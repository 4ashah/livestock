using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using LivestockManager.Infrastructure.Persistence;

namespace LivestockManager.Infrastructure.Persistence;

public class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=.;Database=LivestockManager;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=15");

        return new AppDbContext(optionsBuilder.Options);
    }
}
