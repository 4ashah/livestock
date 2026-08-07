using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LivestockManager.Infrastructure.Persistence;
using System.Net;
using Xunit;

namespace LivestockManager.IntegrationTests;

public class IntegrationBootTests : IClassFixture<LivestockManagerWebFactory>
{
    private readonly LivestockManagerWebFactory _factory;

    public IntegrationBootTests(LivestockManagerWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CanBootApplication_ReturnsHealthy()
    {
        var client = _factory.CreateClientNoRedirect();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task CanBoot_AppliesMigrations_TablesExist()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            bool canConnect = await db.Database.CanConnectAsync();
            Assert.True(canConnect, "Should be able to connect to integration database.");
            var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
                    var result = await command.ExecuteScalarAsync();
                    int tableCount = Convert.ToInt32(result);
                    Assert.True(tableCount >= 27,
                        $"Expected at least 27 tables from Domain + Identity, got {tableCount}");
                }
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
    }

    [Fact]
    public async Task UnauthenticatedHome_GoesToLogin()
    {
        var client = _factory.CreateClientNoRedirect();
        var response = await client.GetAsync("/");
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found ||
            (int)response.StatusCode == 302,
            $"Expected 302 redirect, got {(int)response.StatusCode} {response.StatusCode}");
        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("/Account/Login", location, StringComparison.OrdinalIgnoreCase);
    }
}
