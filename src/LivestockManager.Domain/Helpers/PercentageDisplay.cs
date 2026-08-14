using System.Globalization;

namespace LivestockManager.Domain.Helpers;

public static class PercentageDisplay
{
    public static string Format(decimal? storedAsFraction)
    {
        if (!storedAsFraction.HasValue)
            return "—";

        return Format(storedAsFraction.Value);
    }

    public static string Format(decimal storedAsFraction)
    {
        if (storedAsFraction == 0m)
            return "0%";

        var hundredX = storedAsFraction * 100m;
        var rounded = Math.Round(hundredX, 2, MidpointRounding.AwayFromZero);
        var formatted = rounded.ToString("0.##", CultureInfo.InvariantCulture);
        return formatted + "%";
    }

    public static string FormatTwoDecimals(decimal? storedAsFraction)
    {
        if (!storedAsFraction.HasValue)
            return "—";

        return FormatTwoDecimals(storedAsFraction.Value);
    }

    public static string FormatTwoDecimals(decimal storedAsFraction)
    {
        var hundredX = storedAsFraction * 100m;
        var rounded = Math.Round(hundredX, 2, MidpointRounding.AwayFromZero);
        return rounded.ToString("N2", CultureInfo.InvariantCulture) + "%";
    }
}
