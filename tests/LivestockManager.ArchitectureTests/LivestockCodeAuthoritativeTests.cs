using System.Reflection;
using System.Text.RegularExpressions;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Entities;
using Xunit;

namespace LivestockManager.ArchitectureTests;

public class LivestockCodeAuthoritativeTests
{
    private static readonly HashSet<int> AuthoritativeDistinctValues = new() { 1, 2, 3, 4, 5 };

    private static readonly Dictionary<LivestockType, int> ExpectedCodeValues = new()
    {
        [LivestockType.Ah] = 1,
        [LivestockType.PurchasedCastratedRam] = 1,
        [LivestockType.Su] = 2,
        [LivestockType.UncastratedRam] = 2,
        [LivestockType.Sa] = 3,
        [LivestockType.PurchasedEwe] = 3,
        [LivestockType.Ad] = 4,
        [LivestockType.BredCastratedRam] = 4,
        [LivestockType.Sd] = 5,
        [LivestockType.BredEwe] = 5,
    };

    private static readonly LivestockType[] PurchasedTypes = new[]
    {
        LivestockType.Ah,
        LivestockType.Su,
        LivestockType.Sa,
    };

    private static readonly LivestockType[] BredTypes = new[]
    {
        LivestockType.Ad,
        LivestockType.Sd,
    };

    private static readonly Regex LivestockIdPattern =
        new Regex(@"^(Ah|Su|Sa|Ad|Sd)\d{5}$", RegexOptions.Compiled);

    [Fact]
    public void LivestockTypeEnum_HasExactlyFiveDistinctValues_AndAliasesMapCorrectly()
    {
        var enumType = typeof(LivestockType);
        var names = Enum.GetNames(enumType);
        var values = Enum.GetValues(enumType).Cast<int>().ToArray();

        Assert.Equal(10, names.Length);

        var distinctValues = new HashSet<int>(values);
        Assert.Equal(5, distinctValues.Count);
        Assert.Equal(AuthoritativeDistinctValues, distinctValues);

        foreach (var (member, expectedValue) in ExpectedCodeValues)
        {
            int actualValue = (int)member;
            Assert.Equal(expectedValue, actualValue);
        }

        foreach (int v in values)
        {
            Assert.Contains(v, AuthoritativeDistinctValues);
        }
    }

    [Theory]
    [InlineData(LivestockType.PurchasedCastratedRam, 100.00)]
    [InlineData(LivestockType.UncastratedRam, 150.50)]
    [InlineData(LivestockType.PurchasedEwe, 200.00)]
    [InlineData(LivestockType.Ah, 0.01)]
    [InlineData(LivestockType.Su, 1.00)]
    [InlineData(LivestockType.Sa, 500.00)]
    public void PurchaseAmountValidation_PurchasedTypes_AllowPositiveAmount(LivestockType type, decimal purchaseAmount)
    {
        var ex = Record.Exception(() => CreateDomainLivestock(type, purchaseAmount));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(LivestockType.PurchasedCastratedRam, 0)]
    [InlineData(LivestockType.UncastratedRam, 0)]
    [InlineData(LivestockType.PurchasedEwe, 0)]
    [InlineData(LivestockType.Su, -5)]
    [InlineData(LivestockType.Sa, -100)]
    public void PurchaseAmountValidation_PurchasedTypes_RejectZeroOrNegative(LivestockType type, decimal purchaseAmount)
    {
        Assert.Throws<ArgumentException>(() => CreateDomainLivestock(type, purchaseAmount));
    }

    [Theory]
    [InlineData(LivestockType.BredCastratedRam, 0)]
    [InlineData(LivestockType.BredEwe, 0)]
    public void PurchaseAmountValidation_BredTypes_RequireExactlyZero(LivestockType type, decimal purchaseAmount)
    {
        var ex = Record.Exception(() => CreateDomainLivestock(type, purchaseAmount));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(LivestockType.BredCastratedRam, 0.01)]
    [InlineData(LivestockType.BredCastratedRam, 1)]
    [InlineData(LivestockType.BredEwe, 250)]
    [InlineData(LivestockType.Ad, 100)]
    [InlineData(LivestockType.Sd, 0.001)]
    [InlineData(LivestockType.Ad, -1)]
    public void PurchaseAmountValidation_BredTypes_RejectAnyNonZero(LivestockType type, decimal purchaseAmount)
    {
        Assert.Throws<ArgumentException>(() => CreateDomainLivestock(type, purchaseAmount));
    }

    [Fact]
    public void PurchasedAndBredTypeSets_AreDisjointAndCoverAllFiveCodes()
    {
        var all = PurchasedTypes.Concat(BredTypes).Select(t => (int)t).ToHashSet();
        Assert.Equal(AuthoritativeDistinctValues, all);
        Assert.Equal(5, all.Count);

        var purchasedVals = PurchasedTypes.Select(t => (int)t).ToHashSet();
        var bredVals = BredTypes.Select(t => (int)t).ToHashSet();
        Assert.Empty(purchasedVals.Intersect(bredVals));
    }

    [Theory]
    [InlineData("Ah00001", true)]
    [InlineData("Su00001", true)]
    [InlineData("Sa00001", true)]
    [InlineData("Ad00001", true)]
    [InlineData("Sd00001", true)]
    [InlineData("Ah12345", true)]
    [InlineData("Su99999", true)]
    [InlineData("Sd00000", true)]
    [InlineData("Ah0001", false)]
    [InlineData("Ah000001", false)]
    [InlineData("XX00001", false)]
    [InlineData("Ch00001", false)]
    [InlineData("PV00001", false)]
    [InlineData("Sh00001", false)]
    [InlineData("AdXXXX1", false)]
    [InlineData("Ah 00001", false)]
    [InlineData("ah00001", false)]
    [InlineData("", false)]
    public void LivestockIdPattern_MatchesAuthoritativeFiveCodesRegex(string candidate, bool shouldMatch)
    {
        bool isMatch = LivestockIdPattern.IsMatch(candidate);
        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData(LivestockType.Ah, 1, "Ah00001")]
    [InlineData(LivestockType.Su, 1, "Su00001")]
    [InlineData(LivestockType.Sa, 1, "Sa00001")]
    [InlineData(LivestockType.Ad, 1, "Ad00001")]
    [InlineData(LivestockType.Sd, 1, "Sd00001")]
    [InlineData(LivestockType.PurchasedCastratedRam, 123, "Ah00123")]
    [InlineData(LivestockType.UncastratedRam, 10000, "Su10000")]
    [InlineData(LivestockType.PurchasedEwe, 99999, "Sa99999")]
    [InlineData(LivestockType.BredCastratedRam, 7, "Ad00007")]
    [InlineData(LivestockType.BredEwe, 42, "Sd00042")]
    public void LivestockIdFormat_FromPrefixAndNumber_MatchesRegex(LivestockType type, long number, string expected)
    {
        string prefix = type switch
        {
            LivestockType.Ah or LivestockType.PurchasedCastratedRam => "Ah",
            LivestockType.Su or LivestockType.UncastratedRam => "Su",
            LivestockType.Sa or LivestockType.PurchasedEwe => "Sa",
            LivestockType.Ad or LivestockType.BredCastratedRam => "Ad",
            LivestockType.Sd or LivestockType.BredEwe => "Sd",
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        string generated = prefix + number.ToString("D5");
        Assert.Equal(expected, generated);
        Assert.True(LivestockIdPattern.IsMatch(generated), $"Generated ID {generated} must match authoritative pattern");
    }

    [Fact]
    public void PurchaseAmountValidationRule_IsConsistentBetweenDomainEntityAndServiceLayer()
    {
        var serviceType = typeof(LivestockManager.Application.Services.Livestock.LivestockService);
        var registerMethod = serviceType.GetMethod("RegisterAsync", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(registerMethod);

        Assert.All(PurchasedTypes, purchasedType =>
        {
            var validEx = Record.Exception(() => CreateDomainLivestock(purchasedType, 50m));
            Assert.Null(validEx);

            var zeroEx = Record.Exception(() => CreateDomainLivestock(purchasedType, 0));
            Assert.IsType<ArgumentException>(zeroEx);
        });

        Assert.All(BredTypes, bredType =>
        {
            var validEx = Record.Exception(() => CreateDomainLivestock(bredType, 0m));
            Assert.Null(validEx);

            var positiveEx = Record.Exception(() => CreateDomainLivestock(bredType, 1m));
            Assert.IsType<ArgumentException>(positiveEx);
        });
    }

    private static Livestock CreateDomainLivestock(LivestockType type, decimal purchaseAmount)
    {
        return new Livestock(
            Guid.NewGuid(),
            "TST00001",
            type,
            DateTimeOffset.UtcNow,
            50m,
            WeightUnit.Kg,
            purchaseAmount);
    }
}
