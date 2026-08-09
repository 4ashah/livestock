using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using LivestockManager.Infrastructure.Persistence.Seed;
using LivestockManager.Infrastructure.Security;

namespace LivestockManager.UnitTests.Final;

internal class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Production";
    public string ApplicationName { get; set; } = "TestApp";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

public class ProductionSeedHardeningTests
{
    private static IHostEnvironment Env(string name) => new TestHostEnvironment { EnvironmentName = name };

    private static IConfiguration Config(params (string Key, string? Value)[] kvps)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var (k, v) in kvps)
        {
            dict[k] = v;
        }
        return new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }

    [Fact]
    public void T1_Production_SeedDemoData_1_StartupValidationFails()
    {
        var config = Config(("SeedDemoData", "1"));
        var env = Env("Production");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductionSeedGuard.CheckAndThrowIfUnsafe(config, env));

        Assert.StartsWith("UNSAFE_SEED_FLAG_SeedDemoData", ex.Message);
    }

    [Fact]
    public void T2_Production_EnableDevSeed_true_StartupValidationFails()
    {
        var config = Config(("EnableDevSeed", "true"));
        var env = Env("Production");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductionSeedGuard.CheckAndThrowIfUnsafe(config, env));

        Assert.StartsWith("UNSAFE_SEED_FLAG_EnableDevSeed", ex.Message);
    }

    [Fact]
    public void T3_Production_EnableE2ESeed_1_StartupValidationFails()
    {
        var config = Config(("EnableE2ESeed", "1"));
        var env = Env("Production");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductionSeedGuard.CheckAndThrowIfUnsafe(config, env));

        Assert.StartsWith("UNSAFE_SEED_FLAG_EnableE2ESeed", ex.Message);
    }

    [Fact]
    public void T4_Production_ENV_ENABLE_DEV_SEED_yes_StartupValidationFails()
    {
        var config = Config(("ENV_ENABLE_DEV_SEED", "yes"));
        var env = Env("Production");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductionSeedGuard.CheckAndThrowIfUnsafe(config, env));

        Assert.StartsWith("UNSAFE_SEED_FLAG_ENV_ENABLE_DEV_SEED", ex.Message);
    }

    [Fact]
    public void T5_Production_NoFlags_StartupValidationSucceeds()
    {
        var config = Config(
            ("SeedDemoData", "0"),
            ("EnableDevSeed", "false"),
            ("EnableE2ESeed", "false"),
            ("ENV_ENABLE_DEV_SEED", null));
        var env = Env("Production");

        ProductionSeedGuard.CheckAndThrowIfUnsafe(config, env);
    }

    [Fact]
    public void T6_TestingEnv_BothTestingAndEnableE2ESeed_1_ConditionsMet()
    {
        var env = Env("Testing");
        var config = Config(("EnableE2ESeed", "1"), ("EnableDevSeed", null));

        var (testingE2E, development) = DemoDataSeeder.EvaluateSeedConditions(env, config);

        Assert.True(testingE2E);
        Assert.False(development);
    }

    [Fact]
    public void T7_TestingEnv_EnableE2ESeed_0OrMissing_NoOpConditions()
    {
        var envMissing = Env("Testing");
        var configMissing = Config(("EnableDevSeed", "true"));
        var (tm, dm) = DemoDataSeeder.EvaluateSeedConditions(envMissing, configMissing);
        Assert.False(tm);

        var envZero = Env("Testing");
        var configZero = Config(("EnableE2ESeed", "0"));
        var (tz, dz) = DemoDataSeeder.EvaluateSeedConditions(envZero, configZero);
        Assert.False(tz);

        var envFalse = Env("Testing");
        var configFalse = Config(("EnableE2ESeed", "false"));
        var (tf, df) = DemoDataSeeder.EvaluateSeedConditions(envFalse, configFalse);
        Assert.False(tf);
    }

    [Fact]
    public void T8_DevEnv_BothDevAndEnableDevSeed_true_SeederActive()
    {
        var env = Env("Development");
        var config = Config(("EnableDevSeed", "true"));

        var (testingE2E, development) = DemoDataSeeder.EvaluateSeedConditions(env, config);

        Assert.False(testingE2E);
        Assert.True(development);
    }

    [Fact]
    public void T9_DevEnv_EnableDevSeedMissing_SeederNoOp()
    {
        var env = Env("Development");
        var config = Config(("EnableE2ESeed", "1"));

        var (testingE2E, development) = DemoDataSeeder.EvaluateSeedConditions(env, config);

        Assert.False(testingE2E);
        Assert.False(development);
    }

    [Fact]
    public async Task T10_ProductionEnv_SeederShortCircuits_NoUserManagerTouch()
    {
        var env = Env("Production");
        var config = Config(
            ("SeedDemoData", "1"),
            ("EnableDevSeed", "true"),
            ("EnableE2ESeed", "1"));

        await DemoDataSeeder.SeedAsync(
            dbContext: null!,
            userManager: null!,
            roleManager: null!,
            env: env,
            config: config);

        Assert.True(DemoDataSeeder.LastProductionShortCircuited);
        Assert.Equal("Production", DemoDataSeeder.LastEnvironmentCheckedName);
        Assert.False(DemoDataSeeder.LastSeedConditionsMet);
        Assert.False(DemoDataSeeder.LastUsedTestingE2EPath);
        Assert.False(DemoDataSeeder.LastUsedDevelopmentPath);
    }

    [Fact]
    public async Task T11_ProductionEnv_ZeroUsersEnumerated_NoDevEmailsTouched()
    {
        var env = Env("Production");
        var config = Config(
            ("EnableDevSeed", "true"),
            ("EnableE2ESeed", "1"));

        await DemoDataSeeder.SeedAsync(
            dbContext: null!,
            userManager: null!,
            roleManager: null!,
            env: env,
            config: config);

        Assert.Equal(0, DemoDataSeeder.LastUsersEnumeratedCount);
        Assert.Equal(6, DemoDataSeeder.KnownDevEmails.Length);
        Assert.Contains("admin@livestock.dev", DemoDataSeeder.KnownDevEmails);
        Assert.Contains("accounts@livestock.dev", DemoDataSeeder.KnownDevEmails);
        Assert.Contains("farmmanager@livestock.dev", DemoDataSeeder.KnownDevEmails);
        Assert.Contains("dataentry@livestock.dev", DemoDataSeeder.KnownDevEmails);
        Assert.Contains("viewer@livestock.dev", DemoDataSeeder.KnownDevEmails);
        Assert.Contains("sysadmin@livestock.dev", DemoDataSeeder.KnownDevEmails);
    }

    [Theory]
    [InlineData("YES", true)]
    [InlineData("TrUe", true)]
    [InlineData(" 1 ", true)]
    [InlineData("Yes", true)]
    [InlineData("TRUE", true)]
    [InlineData("1", true)]
    [InlineData(" YeS ", true)]
    [InlineData("no", false)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    [InlineData("random", false)]
    public void T12_TruthyEvaluation_AllVariantsCorrect(string? value, bool expected)
    {
        bool guardResult = ProductionSeedGuard.IsTruthy(value);
        bool seederResult = DemoDataSeeder.IsTruthy(value);

        Assert.Equal(expected, guardResult);
        Assert.Equal(expected, seederResult);
    }

    [Fact]
    public void T6Bonus_TestingPrefixEnvName_AlsoMatchesTestingConditions()
    {
        var env = Env("TestingE2E");
        var config = Config(("EnableE2ESeed", "1"));
        Assert.True(DemoDataSeeder.IsTestingEnvironment(env.EnvironmentName));
        var (t, d) = DemoDataSeeder.EvaluateSeedConditions(env, config);
        Assert.True(t);
    }

    [Fact]
    public void T8Bonus_DevelopmentInEnvName_AlsoMatchesDevConditions()
    {
        var env = Env("LocalDevelopment");
        var config = Config(("EnableDevSeed", "true"));
        Assert.True(DemoDataSeeder.IsDevelopmentEnvironment(env.EnvironmentName));
        var (t, d) = DemoDataSeeder.EvaluateSeedConditions(env, config);
        Assert.True(d);
    }

    [Fact]
    public void Staging_WithoutFlags_SeederNoOp()
    {
        var env = Env("Staging");
        var config = Config(("EnableDevSeed", "true"), ("EnableE2ESeed", "1"));
        Assert.False(DemoDataSeeder.IsTestingEnvironment(env.EnvironmentName));
        Assert.False(DemoDataSeeder.IsDevelopmentEnvironment(env.EnvironmentName));
        var (t, d) = DemoDataSeeder.EvaluateSeedConditions(env, config);
        Assert.False(t);
        Assert.False(d);
    }
}
