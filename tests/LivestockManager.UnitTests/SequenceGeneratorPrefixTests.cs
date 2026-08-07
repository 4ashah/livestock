using System.Reflection;
using LivestockManager.Domain.Enums;
using LivestockManager.Infrastructure.Services;

namespace LivestockManager.UnitTests;

public class SequenceGeneratorPrefixTests
{
    private static readonly MethodInfo? GetPrefixMethod = typeof(EfSequenceGenerator)
        .GetMethod("GetPrefix", BindingFlags.Static | BindingFlags.NonPublic);

    private static string InvokeGetPrefix(LivestockType type)
    {
        Assert.NotNull(GetPrefixMethod);
        var result = GetPrefixMethod!.Invoke(null, new object[] { type });
        Assert.NotNull(result);
        return (string)result!;
    }

    [Theory]
    [InlineData(LivestockType.PurchasedCastratedRam, "Ah")]
    [InlineData(LivestockType.UncastratedRam, "Su")]
    [InlineData(LivestockType.PurchasedEwe, "Sa")]
    [InlineData(LivestockType.BredCastratedRam, "Ad")]
    [InlineData(LivestockType.BredEwe, "Sd")]
    public void LivestockTypePrefixMappings_ReturnsCorrectCode(LivestockType type, string expectedPrefix)
    {
        var actual = InvokeGetPrefix(type);
        Assert.Equal(expectedPrefix, actual);
    }

    [Theory]
    [InlineData(1, "Ah", "Ah00001")]
    [InlineData(123, "Ah", "Ah00123")]
    [InlineData(10000, "Ah", "Ah10000")]
    [InlineData(99999, "Sd", "Sd99999")]
    [InlineData(42, "Su", "Su00042")]
    public void Formatting_UsesFiveDigitPadding(long lastValue, string prefix, string expected)
    {
        var formatted = prefix + lastValue.ToString("D5");
        Assert.Equal(expected, formatted);
    }

    [Theory]
    [InlineData(LivestockType.PurchasedEwe, 1, "Sa00001")]
    [InlineData(LivestockType.BredCastratedRam, 1234, "Ad01234")]
    [InlineData(LivestockType.UncastratedRam, 50000, "Su50000")]
    public void Combined_PrefixPlusFiveDigitPadding_ProducesExpected(LivestockType type, long lastValue, string expected)
    {
        var prefix = InvokeGetPrefix(type);
        var actual = prefix + lastValue.ToString("D5");
        Assert.Equal(expected, actual);
    }
}
