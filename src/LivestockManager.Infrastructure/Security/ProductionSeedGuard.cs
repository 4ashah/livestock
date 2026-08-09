using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace LivestockManager.Infrastructure.Security;

public static class ProductionSeedGuard
{
    internal static readonly string[] UnsafeSeedFlags = new[]
    {
        "SeedDemoData",
        "EnableDevSeed",
        "EnableE2ESeed",
        "ENV_ENABLE_DEV_SEED"
    };

    internal static bool IsTruthy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var v = value.Trim().ToLowerInvariant();
        return v == "true" || v == "1" || v == "yes";
    }

    public static void CheckAndThrowIfUnsafe(IConfiguration config, IHostEnvironment env)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (env == null) throw new ArgumentNullException(nameof(env));

        if (!env.IsProduction())
        {
            return;
        }

        foreach (var flag in UnsafeSeedFlags)
        {
            var flagVal = config[flag];
            if (IsTruthy(flagVal))
            {
                throw new InvalidOperationException($"UNSAFE_SEED_FLAG_{flag}");
            }
        }
    }
}
