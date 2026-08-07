using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LivestockManager.Domain.ValueObjects;

[Owned]
public class TaxSettings
{
    [Precision(5, 4)]
    public decimal? TaxRate { get; set; }

    public bool TaxOnShipping { get; set; }

    public bool CompoundTax { get; set; }

    public TaxSettings()
    {
    }

    public TaxSettings(decimal? taxRate, bool taxOnShipping, bool compoundTax)
    {
        TaxRate = taxRate;
        TaxOnShipping = taxOnShipping;
        CompoundTax = compoundTax;
    }
}
