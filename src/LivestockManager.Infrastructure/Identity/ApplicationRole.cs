using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LivestockManager.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    [MaxLength(200)]
    public string? Description { get; set; }
}
