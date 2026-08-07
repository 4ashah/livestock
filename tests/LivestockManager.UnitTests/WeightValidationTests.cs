using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;

namespace LivestockManager.UnitTests;

public class WeightValidationTests
{
    private static Livestock CreateActiveAnimal()
    {
        return new Livestock(
            Guid.NewGuid(),
            "TEST001",
            LivestockType.BredEwe,
            DateTimeOffset.Now.AddDays(-10),
            45m,
            WeightUnit.Kg,
            0m)
        {
            CurrentWeight = 45m,
            CurrentWeightDate = DateTimeOffset.Now.AddDays(-10)
        };
    }

    [Fact]
    public void AddWeight_WeightMustBePositive_ZeroThrows()
    {
        Assert.Throws<ArgumentException>(() =>
            new LivestockWeight(Guid.NewGuid(), 0m, WeightUnit.Kg, DateTimeOffset.Now));
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(-0.01)]
    [InlineData(-1000.5)]
    public void AddWeight_WeightMustBePositive_NegativeThrows(decimal weight)
    {
        Assert.Throws<ArgumentException>(() =>
            new LivestockWeight(Guid.NewGuid(), weight, WeightUnit.Kg, DateTimeOffset.Now));
    }

    [Fact]
    public void AddWeight_WeightMustBePositive_PositiveSucceeds()
    {
        var ex = Record.Exception(() =>
            new LivestockWeight(Guid.NewGuid(), 12.5m, WeightUnit.Kg, DateTimeOffset.Now));
        Assert.Null(ex);
    }

    [Fact]
    public void WeighedAtCannotBeFuture_FutureDateThrows()
    {
        var futureDate = DateTimeOffset.Now.AddDays(5);
        Assert.Throws<ArgumentException>(() =>
            LivestockWeight.CreateValidated(Guid.NewGuid(), 25m, WeightUnit.Kg, futureDate));
    }

    [Fact]
    public void WeighedAtCannotBeFuture_TodaySucceeds()
    {
        var today = DateTimeOffset.Now;
        var ex = Record.Exception(() =>
            LivestockWeight.CreateValidated(Guid.NewGuid(), 30m, WeightUnit.Kg, today));
        Assert.Null(ex);
    }

    [Fact]
    public void WeighedAtCannotBeFuture_PastDateSucceeds()
    {
        var past = DateTimeOffset.Now.AddDays(-10);
        var ex = Record.Exception(() =>
            LivestockWeight.CreateValidated(Guid.NewGuid(), 30m, WeightUnit.Kg, past));
        Assert.Null(ex);
    }

    [Fact]
    public void NewWeightBecomesCurrent_SetsLatestWeight()
    {
        var animal = CreateActiveAnimal();
        var weighDate = DateTimeOffset.Now;
        var newWeight = 55.5m;

        var weightRecord = new LivestockWeight(animal.Id, newWeight, WeightUnit.Kg, weighDate);
        animal.Weights.Add(weightRecord);

        animal.CurrentWeight = newWeight;
        animal.CurrentWeightDate = weighDate;

        Assert.Equal(newWeight, animal.CurrentWeight);
        Assert.Equal(weighDate, animal.CurrentWeightDate);
    }

    [Fact]
    public void NewWeightBecomesCurrent_SecondWeightUpdatesCurrent()
    {
        var animal = CreateActiveAnimal();
        var firstDate = DateTimeOffset.Now.AddDays(-3);
        var secondDate = DateTimeOffset.Now;

        var w1 = new LivestockWeight(animal.Id, 48m, WeightUnit.Kg, firstDate);
        animal.Weights.Add(w1);
        animal.CurrentWeight = w1.Weight;
        animal.CurrentWeightDate = w1.WeighedAt;

        var w2 = new LivestockWeight(animal.Id, 52m, WeightUnit.Kg, secondDate);
        animal.Weights.Add(w2);
        animal.CurrentWeight = w2.Weight;
        animal.CurrentWeightDate = w2.WeighedAt;

        Assert.Equal(52m, animal.CurrentWeight);
        Assert.Equal(secondDate, animal.CurrentWeightDate);
        Assert.Equal(2, animal.Weights.Count);
    }
}
