using System.ComponentModel.DataAnnotations.Schema;

namespace LivestockManager.Domain.ValueObjects;

public class TaxSettings
{
    [Column(TypeName = "decimal(5,4)")]
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
