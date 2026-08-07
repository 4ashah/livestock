using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Exceptions;

namespace LivestockManager.UnitTests;

public class DischargeLogicTests
{
    private static Livestock CreateActiveAnimal(decimal purchaseAmount = 200)
    {
        return new Livestock(
            Guid.NewGuid(),
            "TEST001",
            LivestockType.PurchasedCastratedRam,
            DateTimeOffset.Now,
            50m,
            WeightUnit.Kg,
            purchaseAmount);
    }

    [Theory]
    [InlineData(DischargeCondition.Sold, LivestockStatus.DischargedSold)]
    [InlineData(DischargeCondition.Deceased, LivestockStatus.DischargedDeceased)]
    [InlineData(DischargeCondition.Lost, LivestockStatus.DischargedLost)]
    [InlineData(DischargeCondition.Stolen, LivestockStatus.DischargedStolen)]
    [InlineData(DischargeCondition.Other, LivestockStatus.DischargedOther)]
    public void CanDischargeActiveLivestockOnce_SetsCorrectStatus(DischargeCondition condition, LivestockStatus expectedStatus)
    {
        var animal = CreateActiveAnimal();
        Assert.Equal(LivestockStatus.Active, animal.Status);

        animal.Discharge(condition, DateTimeOffset.Now, soldAmount: condition == DischargeCondition.Sold ? 250m : null);

        Assert.Equal(expectedStatus, animal.Status);
        Assert.NotNull(animal.DischargeDate);
        Assert.Equal(condition, animal.DischargeCondition);
    }

    [Theory]
    [InlineData(DischargeCondition.Sold, LivestockStatus.DischargedSold)]
    [InlineData(DischargeCondition.Deceased, LivestockStatus.DischargedDeceased)]
    public void CanDischargeActiveLivestockOnce_SecondDischargeThrows(DischargeCondition condition, LivestockStatus _)
    {
        var animal = CreateActiveAnimal();

        animal.Discharge(condition, DateTimeOffset.Now, soldAmount: condition == DischargeCondition.Sold ? 250m : null);

        Assert.Throws<InvalidDischargeException>(() =>
            animal.Discharge(DischargeCondition.Other, DateTimeOffset.Now));
    }

    [Fact]
    public void SoldCondition_RequiresSoldAmount_ZeroThrows()
    {
        var animal = CreateActiveAnimal();

        Assert.Throws<ArgumentException>(() =>
            animal.Discharge(DischargeCondition.Sold, DateTimeOffset.Now, soldAmount: 0m));
    }

    [Fact]
    public void SoldCondition_RequiresSoldAmount_NullThrows()
    {
        var animal = CreateActiveAnimal();

        Assert.Throws<ArgumentException>(() =>
            animal.Discharge(DischargeCondition.Sold, DateTimeOffset.Now, soldAmount: null));
    }

    [Fact]
    public void SoldCondition_RequiresSoldAmount_PositiveAmountOk()
    {
        var animal = CreateActiveAnimal();

        var ex = Record.Exception(() =>
            animal.Discharge(DischargeCondition.Sold, DateTimeOffset.Now, soldAmount: 250m));

        Assert.Null(ex);
        Assert.Equal(250m, animal.SoldAmount);
    }

    [Theory]
    [InlineData(DischargeCondition.Deceased)]
    [InlineData(DischargeCondition.Lost)]
    [InlineData(DischargeCondition.Stolen)]
    [InlineData(DischargeCondition.Other)]
    public void NonSoldCondition_SoldAmountIsNull_WhenProvidedNull(DischargeCondition condition)
    {
        var animal = CreateActiveAnimal();

        animal.Discharge(condition, DateTimeOffset.Now, soldAmount: null);

        Assert.Null(animal.SoldAmount);
    }

    [Theory]
    [InlineData(DischargeCondition.Deceased)]
    [InlineData(DischargeCondition.Lost)]
    [InlineData(DischargeCondition.Stolen)]
    [InlineData(DischargeCondition.Other)]
    public void NonSoldCondition_SoldAmountIsNull_EvenWhenProvidedNonZero(DischargeCondition condition)
    {
        var animal = CreateActiveAnimal();

        animal.Discharge(condition, DateTimeOffset.Now, soldAmount: 500m);

        Assert.Null(animal.SoldAmount);
    }
}
