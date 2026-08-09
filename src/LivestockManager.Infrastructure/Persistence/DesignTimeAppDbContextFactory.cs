using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using LivestockManager.Infrastructure.Persistence;

namespace LivestockManager.Infrastructure.Persistence;

public class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();

        var webProbe = Path.Combine(basePath, "..", "LivestockManager.Web");
        var resolvedBase = Directory.Exists(webProbe) ? Path.GetFullPath(webProbe) : basePath;

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(resolvedBase)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables();

        var config = configBuilder.Build();

        var canonical = config["ConnectionStrings:LivestockManagerDb"];
        var legacy = config["ConnectionStrings:DefaultConnection"];

        string? connectionString = null;
        if (!string.IsNullOrWhiteSpace(canonical))
        {
            connectionString = canonical;
        }
        else if (!string.IsNullOrWhiteSpace(legacy))
        {
            connectionString = legacy;
            Console.WriteLine("[DesignTime] Warning: ConnectionStrings:DefaultConnection is deprecated. Switch to ConnectionStrings:LivestockManagerDb.");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Server=.;Database=LivestockManager;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=15";
            Console.WriteLine("[DesignTime] Warning: Using hardcoded fallback connection string. Configure ConnectionStrings:LivestockManagerDb.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            sql =>
            {
                sql.CommandTimeout(90);
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            });

        return new AppDbContext(optionsBuilder.Options);
    }
}
