using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.ValueObjects;

namespace LivestockManager.Domain.Entities;

public class Farm : BaseAuditableEntity
{
    public Guid CompanyId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    public Address? Address { get; set; }

    public Guid? ManagerUserId { get; set; }

    public Currency Currency { get; set; }

    public WeightUnit WeightUnit { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(CompanyId))]
    public virtual Company? Company { get; set; }

    public virtual List<Livestock> Livestock { get; set; } = new();

    public virtual List<Sale> Sales { get; set; } = new();

    protected Farm()
    {
    }

    public Farm(Guid companyId, string name, string code)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Farm name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Farm code cannot be empty.", nameof(code));
        CompanyId = companyId;
        Name = name;
        Code = code;
    }
}
