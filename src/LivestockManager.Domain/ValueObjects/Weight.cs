using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Domain.ValueObjects;

public class Weight : IEquatable<Weight>
{
    [Column(TypeName = "decimal(18,4)")]
    public decimal Value { get; set; }

    public WeightUnit Unit { get; set; }

    public Weight()
    {
    }

    public Weight(decimal value, WeightUnit unit)
    {
        if (value <= 0)
            throw new ArgumentException("Weight value must be greater than zero.", nameof(value));
        Value = value;
        Unit = unit;
    }

    public decimal ToKg()
    {
        return Unit == WeightUnit.Kg ? Value : Value * 0.45359237m;
    }

    public decimal ToLb()
    {
        return Unit == WeightUnit.Lb ? Value : Value * 2.2046226218488m;
    }

    public Weight ConvertTo(WeightUnit targetUnit)
    {
        if (Unit == targetUnit)
            return new Weight(Value, Unit);
        return targetUnit == WeightUnit.Kg
            ? new Weight(ToKg(), WeightUnit.Kg)
            : new Weight(ToLb(), WeightUnit.Lb);
    }

    public bool Equals(Weight? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value && Unit == other.Unit;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Weight);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value, (int)Unit);
    }

    public static bool operator ==(Weight? left, Weight? right)
    {
        return EqualityComparer<Weight>.Default.Equals(left, right);
    }

    public static bool operator !=(Weight? left, Weight? right)
    {
        return !(left == right);
    }
}
