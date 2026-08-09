using System.Text.RegularExpressions;

namespace LivestockManager.Web;

public static class ConnectionStringStartupValidator
{
    public const string CanonicalKey = "ConnectionStrings:LivestockManagerDb";
    public const string LegacyKey = "ConnectionStrings:DefaultConnection";

    public const string CanonicalEnvVar = "ConnectionStrings__LivestockManagerDb";
    public const string LegacyEnvVar = "ConnectionStrings__DefaultConnection";

    private static readonly Regex DatabaseNameRegex = new(
        @"(?:^|;)\s*(?:Database|Initial Catalog)\s*=\s*([^;]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] KnownDevDatabaseNames = new[]
    {
        "LivestockManagerDev",
        "LivestockManager_Dev",
        "LivestockManager_Development"
    };

    private static readonly string[] UnsafeSeedFlags = new[]
    {
        "SeedDemoData",
        "EnableDevSeed",
        "EnableE2ESeed",
        "ENV_ENABLE_DEV_SEED"
    };

    public static bool IsTruthy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var v = value.Trim().ToLowerInvariant();
        return v == "true" || v == "1" || v == "yes";
    }

    public static string? ParseDatabaseName(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return null;
        var m = DatabaseNameRegex.Match(connectionString);
        if (!m.Success) return null;
        return m.Groups[1].Value.Trim();
    }

    public static string SanitizeDatabaseNameForLog(string? dbName)
    {
        if (string.IsNullOrWhiteSpace(dbName)) return "<none>";
        if (dbName.Length <= 3) return new string('*', dbName.Length);
        return dbName.Substring(0, 3) + new string('*', Math.Max(1, dbName.Length - 3));
    }

    public static bool ContainsUserIndicator(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return false;
        var cs = connectionString;
        return cs.Contains("User ID=", StringComparison.OrdinalIgnoreCase) ||
               cs.Contains("User=", StringComparison.OrdinalIgnoreCase) ||
               cs.Contains("UID=", StringComparison.OrdinalIgnoreCase);
    }

    public static ConnectionStringValidationResult ValidateAndResolve(
        string? aspNetCoreEnvironment,
        string? canonicalValue,
        string? legacyValue,
        Func<string, string?> getFlagValue)
    {
        var warnings = new List<string>();
        var errors = new List<string>();
        var unsafePatterns = new List<int>();

        bool isProduction = string.Equals(aspNetCoreEnvironment, "Production", StringComparison.OrdinalIgnoreCase);
        bool isDevOrTesting = !isProduction;

        bool hasCanonical = !string.IsNullOrWhiteSpace(canonicalValue);
        bool hasLegacy = !string.IsNullOrWhiteSpace(legacyValue);

        string? resolvedValue = null;
        bool usedLegacyFallback = false;

        if (hasCanonical && hasLegacy)
        {
            errors.Add("Both ConnectionStrings:LivestockManagerDb and ConnectionStrings:DefaultConnection are present. Specify only ConnectionStrings:LivestockManagerDb.");
            return new ConnectionStringValidationResult(
                Success: false,
                ResolvedConnectionString: null,
                UsedLegacyFallback: false,
                Warnings: warnings,
                Errors: errors,
                UnsafePatterns: unsafePatterns,
                SanitizedSummary: string.Empty);
        }

        if (hasCanonical)
        {
            resolvedValue = canonicalValue;
        }
        else if (hasLegacy)
        {
            if (isProduction)
            {
                errors.Add("Missing canonical connection string ConnectionStrings:LivestockManagerDb is required for Production. Legacy fallback ConnectionStrings:DefaultConnection is not allowed in Production.");
                return new ConnectionStringValidationResult(
                    Success: false,
                    ResolvedConnectionString: null,
                    UsedLegacyFallback: false,
                    Warnings: warnings,
                    Errors: errors,
                    UnsafePatterns: unsafePatterns,
                    SanitizedSummary: string.Empty);
            }

            usedLegacyFallback = true;
            resolvedValue = legacyValue;
            warnings.Add("ConnectionStrings:DefaultConnection is deprecated. Switch to ConnectionStrings:LivestockManagerDb.");
        }
        else
        {
            if (isProduction)
            {
                errors.Add("Missing canonical connection string ConnectionStrings:LivestockManagerDb is required for Production.");
            }
            else
            {
                errors.Add("Missing connection string. Configure ConnectionStrings:LivestockManagerDb.");
            }
            return new ConnectionStringValidationResult(
                Success: false,
                ResolvedConnectionString: null,
                UsedLegacyFallback: false,
                Warnings: warnings,
                Errors: errors,
                UnsafePatterns: unsafePatterns,
                SanitizedSummary: string.Empty);
        }

        if (isProduction)
        {
            if (string.IsNullOrWhiteSpace(resolvedValue))
            {
                unsafePatterns.Add(1);
            }

            if (resolvedValue != null && resolvedValue.Contains("__TO_FILL_AT_DEPLOY__", StringComparison.Ordinal))
            {
                unsafePatterns.Add(2);
            }

            if (resolvedValue != null && resolvedValue.IndexOf("(LocalDB)", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                unsafePatterns.Add(3);
            }

            var dbName = ParseDatabaseName(resolvedValue);
            if (dbName != null)
            {
                if (dbName.StartsWith("LivestockManager_E2E", StringComparison.Ordinal))
                {
                    unsafePatterns.Add(4);
                }

                foreach (var devName in KnownDevDatabaseNames)
                {
                    if (string.Equals(dbName, devName, StringComparison.Ordinal))
                    {
                        unsafePatterns.Add(5);
                        break;
                    }
                }
            }

            foreach (var flag in UnsafeSeedFlags)
            {
                var flagVal = getFlagValue(flag);
                if (IsTruthy(flagVal))
                {
                    unsafePatterns.Add(6);
                    break;
                }
            }
        }

        foreach (var p in unsafePatterns.Distinct())
        {
            errors.Add($"Connection string contains unsafe pattern #{p}.");
        }

        bool success = errors.Count == 0;

        int csLength = resolvedValue?.Length ?? 0;
        bool hasUserId = ContainsUserIndicator(resolvedValue);
        var parsedDb = ParseDatabaseName(resolvedValue);
        string sanitizedDb = SanitizeDatabaseNameForLog(parsedDb);
        string summary = $"Length={csLength}; HasUserID={hasUserId}; Database={sanitizedDb};";

        return new ConnectionStringValidationResult(
            Success: success,
            ResolvedConnectionString: resolvedValue,
            UsedLegacyFallback: usedLegacyFallback,
            Warnings: warnings,
            Errors: errors,
            UnsafePatterns: unsafePatterns.Distinct().ToList(),
            SanitizedSummary: summary);
    }
}

public sealed record ConnectionStringValidationResult(
    bool Success,
    string? ResolvedConnectionString,
    bool UsedLegacyFallback,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    IReadOnlyList<int> UnsafePatterns,
    string SanitizedSummary);
