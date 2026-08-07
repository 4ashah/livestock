using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LivestockManager.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? CompanyId { get; set; }

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset? LastLoginAt { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset? ModifiedAt { get; set; }
}
