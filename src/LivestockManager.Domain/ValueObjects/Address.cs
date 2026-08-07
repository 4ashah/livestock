using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LivestockManager.Domain.ValueObjects;

[Owned]
public class Address : IEquatable<Address>
{
    [MaxLength(256)]
    public string? Street1 { get; set; }

    [MaxLength(256)]
    public string? Street2 { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(50)]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    public Address()
    {
    }

    public Address(string? street1, string? street2, string? city, string? state, string? postalCode, string? country)
    {
        Street1 = street1;
        Street2 = street2;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    public bool Equals(Address? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Street1 == other.Street1 &&
               Street2 == other.Street2 &&
               City == other.City &&
               State == other.State &&
               PostalCode == other.PostalCode &&
               Country == other.Country;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Address);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Street1, Street2, City, State, PostalCode, Country);
    }

    public static bool operator ==(Address? left, Address? right)
    {
        return EqualityComparer<Address>.Default.Equals(left, right);
    }

    public static bool operator !=(Address? left, Address? right)
    {
        return !(left == right);
    }
}
