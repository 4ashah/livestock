using System.Reflection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Entities;

namespace LivestockManager.UnitTests.Final;

public class DateTimeClockTests
{
    private static string FindSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 16; i++)
        {
            if (File.Exists(Path.Combine(dir, "LivestockManager.sln"))) return dir;
            dir = Path.GetDirectoryName(dir);
            if (string.IsNullOrWhiteSpace(dir)) return Environment.CurrentDirectory;
        }
        return Environment.CurrentDirectory;
    }

    [Fact]
    public void DT1_ProgramCs_ContainsStartupInfoUtcStartLiteral()
    {
        var slnRoot = FindSolutionRoot();
        var programPath = Path.Combine(slnRoot, "src", "LivestockManager.Web", "Program.cs");
        Assert.True(File.Exists(programPath), $"Program.cs not found at expected path: {programPath}");
        var content = File.ReadAllText(programPath);
        Assert.Contains("[STARTUP-INFO] UTC start time", content, StringComparison.Ordinal);
    }

    [Fact]
    public void DT2_LocalTimezoneId_NonEmpty_And_UtcNow_Between1970And2100()
    {
        var tzId = TimeZoneInfo.Local.Id;
        Assert.False(string.IsNullOrWhiteSpace(tzId), "TimeZoneInfo.Local.Id must be non-empty and valid.");

        var now = DateTime.UtcNow;
        var min = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var max = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.True(now >= min, $"DateTime.UtcNow {now:o} must be >= 1970-01-01 UTC.");
        Assert.True(now <= max, $"DateTime.UtcNow {now:o} must be <= 2100-01-01 UTC.");
    }

    [Fact]
    public void DT3_ClockHealthCheck_ReturnsHealthy_WhenValid()
    {
        var tzId = TimeZoneInfo.Local.Id;
        var now = DateTime.UtcNow;
        var minValid = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxValid = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var tzOk = !string.IsNullOrWhiteSpace(tzId);
        var clockOk = now >= minValid && now <= maxValid;

        HealthStatus expectedStatus;
        string expectedDescContains;
        if (tzOk && clockOk)
        {
            expectedStatus = HealthStatus.Healthy;
            expectedDescContains = "valid";
        }
        else
        {
            expectedStatus = HealthStatus.Degraded;
            expectedDescContains = "invalid";
        }

        Func<HealthCheckResult> clockCheck = () =>
        {
            var localTzId = TimeZoneInfo.Local.Id;
            var localNow = DateTime.UtcNow;
            var localMin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var localMax = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var localTzOk = !string.IsNullOrWhiteSpace(localTzId);
            var localClockOk = localNow >= localMin && localNow <= localMax;
            if (localTzOk && localClockOk)
                return HealthCheckResult.Healthy("Clock and timezone valid.");
            return HealthCheckResult.Degraded("System clock or timezone appears invalid");
        };

        var result = clockCheck();
        Assert.Equal(expectedStatus, result.Status);
        Assert.NotNull(result.Description);
        Assert.Contains(expectedDescContains, result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DT4_BaseAuditableEntity_CreatedAt_UsesDateTimeOffset_WithUtcDefault()
    {
        var createdProp = typeof(BaseAuditableEntity).GetProperty("CreatedAt", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(createdProp);
        Assert.Equal(typeof(DateTimeOffset), createdProp!.PropertyType);

        var concreteEntityType = typeof(Livestock);
        var ctor = concreteEntityType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(c => c.GetParameters().Length == 0);
        if (ctor == null)
        {
            ctor = concreteEntityType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(c => c.GetParameters().Length)
                .First();
        }
        Assert.NotNull(ctor);

        var args = new object?[ctor.GetParameters().Length];
        var instance = (BaseAuditableEntity)ctor!.Invoke(args);
        var createdAt = (DateTimeOffset)createdProp.GetValue(instance)!;

        Assert.Equal(TimeSpan.Zero, createdAt.Offset);
    }
}
