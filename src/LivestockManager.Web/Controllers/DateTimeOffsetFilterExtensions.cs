using System;

namespace LivestockManager.Web.Controllers;

public static class DateTimeOffsetFilterExtensions
{
    public static DateTimeOffset AsUtcDayStart(this DateTime date)
    {
        return new DateTimeOffset(date.Date.Ticks, TimeSpan.Zero);
    }

    public static DateTimeOffset AsUtcDayEnd(this DateTime date)
    {
        return new DateTimeOffset(date.Date.AddDays(1).AddTicks(-1).Ticks, TimeSpan.Zero);
    }

    public static DateTimeOffset? AsUtcDayStartOrDefault(this DateTime? date, DateTimeOffset? fallback = null)
    {
        if (date.HasValue) return date.Value.AsUtcDayStart();
        return fallback;
    }

    public static DateTimeOffset? AsUtcDayEndOrDefault(this DateTime? date, DateTimeOffset? fallback = null)
    {
        if (date.HasValue) return date.Value.AsUtcDayEnd();
        return fallback;
    }
}
