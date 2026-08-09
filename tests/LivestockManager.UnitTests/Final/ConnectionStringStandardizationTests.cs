using LivestockManager.Web;

namespace LivestockManager.UnitTests.Final;

public class ConnectionStringStandardizationTests
{
    private const string ValidConnString = "Server=.;Database=LivestockManagerProd;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
    private const string ValidConnStringWithUserId = "Server=.;Database=LivestockManagerProd;User ID=appuser;Password=secret123;MultipleActiveResultSets=True;";

    private static Func<string, string?> NoFlags => _ => null;

    [Fact]
    public void C1_CanonicalOnly_ResolvesCorrectly_NoWarnings_NoErrors()
    {
        var result = ConnectionStringStartupValidator.ValidateAndResolve(
            aspNetCoreEnvironment: "Development",
            canonicalValue: ValidConnString,
            legacyValue: null,
            getFlagValue: NoFlags);

        Assert.True(result.Success);
        Assert.Equal(ValidConnString, result.ResolvedConnectionString);
        Assert.False(result.UsedLegacyFallback);
        Assert.Empty(result.Warnings);
        Assert.Empty(result.Errors);
        Assert.Empty(result.UnsafePatterns);
    }

    [Fact]
    public void C2_LegacyOnly_InDevOrTesting_WarnsAndUsesFallback()
    {
        var legacyConn = "Server=.;Database=LivestockManager;Integrated Security=True;";

        var devResult = ConnectionStringStartupValidator.ValidateAndResolve(
            aspNetCoreEnvironment: "Development",
            canonicalValue: null,
            legacyValue: legacyConn,
            getFlagValue: NoFlags);

        Assert.True(devResult.Success);
        Assert.Equal(legacyConn, devResult.ResolvedConnectionString);
        Assert.True(devResult.UsedLegacyFallback);
        Assert.Single(devResult.Warnings);
        Assert.Contains("deprecated", devResult.Warnings[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DefaultConnection", devResult.Warnings[0]);
        Assert.Contains("LivestockManagerDb", devResult.Warnings[0]);
        Assert.Empty(devResult.Errors);

        var testResult = ConnectionStringStartupValidator.ValidateAndResolve(
            aspNetCoreEnvironment: "Testing",
            canonicalValue: null,
            legacyValue: legacyConn,
            getFlagValue: NoFlags);

        Assert.True(testResult.Success);
        Assert.True(testResult.UsedLegacyFallback);
        Assert.Single(testResult.Warnings);
    }

    [Fact]
    public void C3_BothPresent_FailsStartup_WithClearError()
    {
        var result = ConnectionStringStartupValidator.ValidateAndResolve(
            aspNetCoreEnvironment: "Development",
            canonicalValue: ValidConnString,
            legacyValue: "Server=.;Database=Other;Integrated Security=True;",
            getFlagValue: NoFlags);

        Assert.False(result.Success);
        Assert.Null(result.ResolvedConnectionString);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Both", result.Errors[0]);
        Assert.Contains("LivestockManagerDb", result.Errors[0]);
        Assert.Contains("DefaultConnection", result.Errors[0]);
    }

    [Fact]
    public void C4_BothPresent_InTestingEnvironment_StillFails()
    {
        var result = ConnectionStringStartupValidator.ValidateAndResolve(
            aspNetCoreEnvironment: "Testing",
            canonicalValue: ValidConnString,
            legacyValue: "Server=.;Database=Other;Integrated Security=True;",
            getFlagValue: NoFlags);

        Assert.False(result.Success);
        Assert.Contains("Both", result.Errors[0]);
    }

    [Fact]
    public void C5_Production_NeitherPresent_FailsWithRequiredMessage()
    {
        var result = ConnectionStringStartupValidator.ValidateAndResolve(
            aspNetCoreEnvironment: "Production",
            canonicalValue: null,
            legacyValue: null,
            getFlagValue: NoFlags);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("ConnectionStrings:LivestockManagerDb", result.Errors[0]);
        Assert.Contains("required", result.Errors[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Production", result.Errors[0]);
    }

    [Fact]
    public void C6_Production_PlaceholderLiteral_AnywhereInValue_FailsPattern2()
    {
        string placeholderConn = "Server=.;Database=__TO_FILL_AT_DEPLOY__;Trusted_Connection=True;";

        var result = ConnectionStringStartupValidator.ValidateAndResolve(
            aspNetCoreEnvironment: "Production",
            canonicalValue: placeholderConn,
            legacyValue: null,
            getFlagValue: NoFlags);

        Assert.False(result.Success);
        Assert.Contains(2, result.UnsafePatterns);
        Assert.Contains("unsafe pattern #2", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void C7_Production_LocalDB_Substring_CaseInsensitive_FailsPattern3()
    {
        string localDbConn1 = "Server=(LocalDB)\\MSSQLLocalDB;Database=LivestockManager;Integrated Security=True;";
        string localDbConn2 = "Server=(localdb)\\v11.0;Database=LivestockManager;Integrated Security=True;";

        var result1 = ConnectionStringStartupValidator.ValidateAndResolve(
            aspNetCoreEnvironment: "Production",
            canonicalValue: localDbConn1,
            legacyValue: null,
            getFlagValue: NoFlags);

        Assert.False(result1.Success);
        Assert.Contains(3, result1.UnsafePatterns);

        var result2 = ConnectionStringStartupValidator.ValidateAndResolve(
            aspNetCoreEnvironment: "Production",
            canonicalValue: localDbConn2,
            legacyValue: null,
            getFlagValue: NoFlags);

        Assert.False(result2.Success);
        Assert.Contains(3, result2.UnsafePatterns);
    }

    [Fact]
    public void C8_Production_E2EDBPrefix_FailsPattern4()
    {
        string e2eExact = "Server=.;Database=LivestockManager_E2E;Trusted_Connection=True;";
        string e2eSuffix = "Server=.;Database=LivestockManager_E2E_20260809;Trusted_Connection=True;";

        var exactResult = ConnectionStringStartupValidator.ValidateAndResolve(
            aspNetCoreEnvironment: "Production",
            canonicalValue: e2eExact,
            legacyValue: null,
            getFlagValue: NoFlags);

        Assert.False(exactResult.Success);
        Assert.Contains(4, exactResult.UnsafePatterns);

        var suffixResult = ConnectionStringStartupValidator.ValidateAndResolve(
            aspNetCoreEnvironment: "Production",
            canonicalValue: e2eSuffix,
            legacyValue: null,
            getFlagValue: NoFlags);

        Assert.False(suffixResult.Success);
        Assert.Contains(4, suffixResult.UnsafePatterns);
    }

    [Fact]
    public void C9_Production_KnownDevDatabaseNames_FailsPattern5()
    {
        string[] devNames = { "LivestockManagerDev", "LivestockManager_Dev", "LivestockManager_Development" };

        foreach (var devName in devNames)
        {
            string devConn = $"Server=.;Database={devName};Trusted_Connection=True;";
            var result = ConnectionStringStartupValidator.ValidateAndResolve(
                aspNetCoreEnvironment: "Production",
                canonicalValue: devConn,
                legacyValue: null,
                getFlagValue: NoFlags);

            Assert.False(result.Success, $"Expected failure for dev DB name: {devName}");
            Assert.Contains(5, result.UnsafePatterns);
        }
    }

    [Theory]
    [InlineData("SeedDemoData", "true")]
    [InlineData("SeedDemoData", "1")]
    [InlineData("SeedDemoData", "yes")]
    [InlineData("EnableDevSeed", "true")]
    [InlineData("EnableDevSeed", "1")]
    [InlineData("EnableDevSeed", "YES")]
    [InlineData("EnableE2ESeed", "True")]
    [InlineData("EnableE2ESeed", "1")]
    [InlineData("ENV_ENABLE_DEV_SEED", "true")]
    [InlineData("ENV_ENABLE_DEV_SEED", "1")]
    [InlineData("ENV_ENABLE_DEV_SEED", "yes")]
    public void C10_Production_SeedFlagsTruthyValues_FailsPattern6(string flagName, string flagValue)
    {
        var result = ConnectionStringStartupValidator.ValidateAndResolve(
            aspNetCoreEnvironment: "Production",
            canonicalValue: ValidConnString,
            legacyValue: null,
            getFlagValue: flag => flag == flagName ? flagValue : null);

        Assert.False(result.Success);
        Assert.Contains(6, result.UnsafePatterns);
    }

    [Fact]
    public void C11_SanitizedSummary_NeverIncludesSecrets_OnlySafeMetadata()
    {
        var result = ConnectionStringStartupValidator.ValidateAndResolve(
            aspNetCoreEnvironment: "Development",
            canonicalValue: ValidConnStringWithUserId,
            legacyValue: null,
            getFlagValue: NoFlags);

        Assert.True(result.Success);
        string summary = result.SanitizedSummary;

        Assert.DoesNotContain("secret123", summary);
        Assert.DoesNotContain("appuser", summary);
        Assert.DoesNotContain("Password=", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LivestockManagerProd", summary);

        Assert.Contains("Length=", summary);
        Assert.Contains("HasUserID=True", summary);

        var dbName = ConnectionStringStartupValidator.ParseDatabaseName(ValidConnStringWithUserId);
        Assert.NotNull(dbName);
        var sanitizedDb = ConnectionStringStartupValidator.SanitizeDatabaseNameForLog(dbName);
        Assert.Contains($"Database={sanitizedDb}", summary);

        Assert.StartsWith("Liv", sanitizedDb);
        Assert.EndsWith("*", sanitizedDb);
        Assert.NotEqual(dbName, sanitizedDb);
        Assert.Contains("*", sanitizedDb);
    }

    [Fact]
    public void IsTruthy_AcceptsTrue1Yes_RejectsOtherValues()
    {
        Assert.True(ConnectionStringStartupValidator.IsTruthy("true"));
        Assert.True(ConnectionStringStartupValidator.IsTruthy("TRUE"));
        Assert.True(ConnectionStringStartupValidator.IsTruthy("True"));
        Assert.True(ConnectionStringStartupValidator.IsTruthy("1"));
        Assert.True(ConnectionStringStartupValidator.IsTruthy("yes"));
        Assert.True(ConnectionStringStartupValidator.IsTruthy("YES"));
        Assert.True(ConnectionStringStartupValidator.IsTruthy("  true  "));

        Assert.False(ConnectionStringStartupValidator.IsTruthy("false"));
        Assert.False(ConnectionStringStartupValidator.IsTruthy("0"));
        Assert.False(ConnectionStringStartupValidator.IsTruthy("no"));
        Assert.False(ConnectionStringStartupValidator.IsTruthy(""));
        Assert.False(ConnectionStringStartupValidator.IsTruthy(null));
        Assert.False(ConnectionStringStartupValidator.IsTruthy("  "));
        Assert.False(ConnectionStringStartupValidator.IsTruthy("random"));
    }

    [Fact]
    public void ParseDatabaseName_ExtractsDatabaseOrInitialCatalog_CaseInsensitive()
    {
        Assert.Equal("MyDb", ConnectionStringStartupValidator.ParseDatabaseName("Server=.;Database=MyDb;Trusted_Connection=True;"));
        Assert.Equal("MyDb", ConnectionStringStartupValidator.ParseDatabaseName("Server=.;Initial Catalog=MyDb;Trusted_Connection=True;"));
        Assert.Equal("MyDb", ConnectionStringStartupValidator.ParseDatabaseName("Server=.;database=MyDb;Trusted_Connection=True;"));
        Assert.Equal("MyDb", ConnectionStringStartupValidator.ParseDatabaseName("Database=MyDb"));
        Assert.Equal("MyDb", ConnectionStringStartupValidator.ParseDatabaseName("Server=.;Database = MyDb ;Integrated Security=True;"));

        Assert.Null(ConnectionStringStartupValidator.ParseDatabaseName(null));
        Assert.Null(ConnectionStringStartupValidator.ParseDatabaseName(""));
        Assert.Null(ConnectionStringStartupValidator.ParseDatabaseName("Server=.;Trusted_Connection=True;"));
    }

    [Fact]
    public void Production_UsingLegacyFallback_NotAllowed()
    {
        var legacyConn = "Server=.;Database=LivestockManagerProd;Trusted_Connection=True;";
        var result = ConnectionStringStartupValidator.ValidateAndResolve(
            aspNetCoreEnvironment: "Production",
            canonicalValue: null,
            legacyValue: legacyConn,
            getFlagValue: NoFlags);

        Assert.False(result.Success);
        Assert.Contains("Production", result.Errors[0]);
        Assert.Contains("DefaultConnection", result.Errors[0]);
        Assert.Contains("not allowed", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NonProduction_KnownDevNamesAndE2E_Allowed()
    {
        var devConn = "Server=.;Database=LivestockManager_Dev;Trusted_Connection=True;";
        var devResult = ConnectionStringStartupValidator.ValidateAndResolve(
            aspNetCoreEnvironment: "Development",
            canonicalValue: devConn,
            legacyValue: null,
            getFlagValue: flag => flag == "SeedDemoData" ? "1" : null);

        Assert.True(devResult.Success);
        Assert.Empty(devResult.UnsafePatterns);

        var e2eConn = "Server=.;Database=LivestockManager_E2E_1234;Trusted_Connection=True;";
        var testResult = ConnectionStringStartupValidator.ValidateAndResolve(
            aspNetCoreEnvironment: "Testing",
            canonicalValue: e2eConn,
            legacyValue: null,
            getFlagValue: flag =>
            {
                if (flag == "EnableE2ESeed") return "1";
                if (flag == "EnableDevSeed") return "true";
                if (flag == "SeedDemoData") return "0";
                return null;
            });

        Assert.True(testResult.Success);
        Assert.Empty(testResult.UnsafePatterns);
    }

    [Fact]
    public void ErrorMessages_NeverPrintActualConnectionString()
    {
        string uniqueUser = "LM_AppUser_X7_Secret";
        string uniquePass = "P@ss_LM_K9_AlphaZulu!";
        string uniqueServer = "LM-PROD-SQLNODE-42";
        string uniqueDb = "CorpLivestock_Private_07";
        string secretConn = $"Server={uniqueServer};Database={uniqueDb};User ID={uniqueUser};Password={uniquePass};MultipleActiveResultSets=True;";

        var placeholderConn = secretConn.Replace(uniqueDb, "__TO_FILL_AT_DEPLOY__");

        var result = ConnectionStringStartupValidator.ValidateAndResolve(
            aspNetCoreEnvironment: "Production",
            canonicalValue: placeholderConn,
            legacyValue: null,
            getFlagValue: NoFlags);

        Assert.False(result.Success);
        string allErrors = string.Join(" ", result.Errors);

        Assert.DoesNotContain(uniquePass, allErrors, StringComparison.Ordinal);
        Assert.DoesNotContain(uniqueUser, allErrors, StringComparison.Ordinal);
        Assert.DoesNotContain("__TO_FILL_AT_DEPLOY__", allErrors, StringComparison.Ordinal);
        Assert.DoesNotContain(uniqueServer, allErrors, StringComparison.Ordinal);
    }
}
