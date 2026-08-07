using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;

namespace LivestockManager.UnitTests;

public class LivestockDomainTests
{
    private static Livestock CreateLivestock(
        LivestockType type,
        decimal initialWeight = 50,
        decimal purchaseAmount = 0,
        string livestockId = "TEST001")
    {
        return new Livestock(
            Guid.NewGuid(),
            livestockId,
            type,
            DateTimeOffset.Now,
            initialWeight,
            WeightUnit.Kg,
            purchaseAmount);
    }

    [Theory]
    [InlineData(LivestockType.BredCastratedRam, 0)]
    [InlineData(LivestockType.BredEwe, 0)]
    public void Constructor_WhenBredType_PurchaseAmountMustBeZero_Success(LivestockType type, decimal purchaseAmount)
    {
        var ex = Record.Exception(() => CreateLivestock(type, purchaseAmount: purchaseAmount));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(LivestockType.BredCastratedRam, 100)]
    [InlineData(LivestockType.BredCastratedRam, 1)]
    [InlineData(LivestockType.BredEwe, 250)]
    [InlineData(LivestockType.BredEwe, 0.01)]
    public void Constructor_WhenBredType_PurchaseAmountMustBeZero_FailsIfPositive(LivestockType type, decimal purchaseAmount)
    {
        Assert.Throws<ArgumentException>(() => CreateLivestock(type, purchaseAmount: purchaseAmount));
    }

    [Theory]
    [InlineData(LivestockType.PurchasedCastratedRam, 100)]
    [InlineData(LivestockType.UncastratedRam, 150)]
    [InlineData(LivestockType.PurchasedEwe, 200)]
    [InlineData(LivestockType.PurchasedCastratedRam, 0.01)]
    public void Constructor_WhenPurchasedType_PurchaseAmountMustBePositive_Success(LivestockType type, decimal purchaseAmount)
    {
        var ex = Record.Exception(() => CreateLivestock(type, purchaseAmount: purchaseAmount));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(LivestockType.PurchasedCastratedRam, 0)]
    [InlineData(LivestockType.UncastratedRam, 0)]
    [InlineData(LivestockType.PurchasedEwe, 0)]
    public void Constructor_WhenPurchasedType_PurchaseAmountMustBePositive_FailsIfZero(LivestockType type, decimal purchaseAmount)
    {
        Assert.Throws<ArgumentException>(() => CreateLivestock(type, purchaseAmount: purchaseAmount));
    }

    [Theory]
    [InlineData(LivestockType.PurchasedCastratedRam, -5)]
    [InlineData(LivestockType.UncastratedRam, -1)]
    [InlineData(LivestockType.PurchasedEwe, -100)]
    public void Constructor_WhenPurchasedType_PurchaseAmountMustBePositive_FailsIfNegative(LivestockType type, decimal purchaseAmount)
    {
        Assert.Throws<ArgumentException>(() => CreateLivestock(type, purchaseAmount: purchaseAmount));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50.5)]
    public void Constructor_InitialWeightMustBePositive_FailsIfNonPositive(decimal weight)
    {
        Assert.Throws<ArgumentException>(() => CreateLivestock(LivestockType.BredEwe, initialWeight: weight));
    }

    [Fact]
    public void Constructor_InitialWeightMustBePositive_SucceedsIfPositive()
    {
        var ex = Record.Exception(() => CreateLivestock(LivestockType.BredEwe, initialWeight: 12.5m));
        Assert.Null(ex);
    }

    [Fact]
    public void BasicProfitLoss_WhenNotSold_ReturnsNull()
    {
        var animal = CreateLivestock(LivestockType.PurchasedCastratedRam, purchaseAmount: 200);
        animal.Status = LivestockStatus.Active;
        animal.SoldAmount = null;

        Assert.Null(animal.BasicProfitLoss);
    }

    [Fact]
    public void BasicProfitLoss_WhenDischargedNotSold_ReturnsNull()
    {
        var animal = CreateLivestock(LivestockType.PurchasedCastratedRam, purchaseAmount: 200);
        animal.Status = LivestockStatus.DischargedDeceased;
        animal.SoldAmount = null;

        Assert.Null(animal.BasicProfitLoss);
    }

    [Fact]
    public void BasicProfitLoss_WhenSold_ReturnsSoldMinusPurchase_Positive()
    {
        var animal = CreateLivestock(LivestockType.PurchasedCastratedRam, purchaseAmount: 200);
        animal.Status = LivestockStatus.DischargedSold;
        animal.SoldAmount = 300;

        Assert.Equal(100m, animal.BasicProfitLoss);
    }

    [Fact]
    public void BasicProfitLoss_WhenSold_ReturnsSoldMinusPurchase_Negative()
    {
        var animal = CreateLivestock(LivestockType.PurchasedCastratedRam, purchaseAmount: 200);
        animal.Status = LivestockStatus.DischargedSold;
        animal.SoldAmount = 150;

        Assert.Equal(-50m, animal.BasicProfitLoss);
    }

    [Fact]
    public void LivestockIdIsLocked_SetOnceByConstructor()
    {
        var id1 = "AH00001";
        var id2 = "SD00099";

        var animal1 = CreateLivestock(LivestockType.PurchasedCastratedRam, livestockId: id1, purchaseAmount: 150m);
        var animal2 = CreateLivestock(LivestockType.BredEwe, livestockId: id2, purchaseAmount: 0m);

        Assert.Equal(id1, animal1.LivestockId);
        Assert.Equal(id2, animal2.LivestockId);
        Assert.NotEqual(animal1.LivestockId, animal2.LivestockId);
    }
}
