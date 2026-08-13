using LivestockManager.Domain.Enums;

namespace LivestockManager.Domain.Helpers;

public static class LivestockTypeDisplay
{
    public const string AhLabel = "Ah - Purchased Castrated Ram";
    public const string SuLabel = "Su - Uncastrated Ram";
    public const string SaLabel = "Sa - Purchased Ewe";
    public const string AdLabel = "Ad - Bred Castrated Ram";
    public const string SdLabel = "Sd - Bred Ewe";

    public static string GetDisplayName(this LivestockType type) => type switch
    {
        LivestockType.Ah => AhLabel,
        LivestockType.Su => SuLabel,
        LivestockType.Sa => SaLabel,
        LivestockType.Ad => AdLabel,
        LivestockType.Sd => SdLabel,
        _ => type.ToString()
    };

    public static string GetCode(this LivestockType type) => type switch
    {
        LivestockType.Ah => "Ah",
        LivestockType.Su => "Su",
        LivestockType.Sa => "Sa",
        LivestockType.Ad => "Ad",
        LivestockType.Sd => "Sd",
        _ => type.ToString()
    };

    public static IReadOnlyDictionary<LivestockType, string> AllDisplayNames { get; } = new Dictionary<LivestockType, string>
    {
        [LivestockType.Ah] = AhLabel,
        [LivestockType.Su] = SuLabel,
        [LivestockType.Sa] = SaLabel,
        [LivestockType.Ad] = AdLabel,
        [LivestockType.Sd] = SdLabel
    };
}
