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
            "Server=(localdb)\\MSSQLLocalDB;Database=LivestockManager_Design;Trusted_Connection=True;MultipleActiveResultSets=true");

        return new AppDbContext(optionsBuilder.Options);
    }
}
