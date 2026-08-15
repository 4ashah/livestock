using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LivestockManager.Web.Models.UserViewModels;

public class UserEditViewModel
{
    public Guid Id { get; set; }

    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [EmailAddress]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Current Role")]
    public string CurrentRole { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Role")]
    public string SelectedRole { get; set; } = "DataEntry";

    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; }

    [Display(Name = "System Administrator")]
    public bool IsSystemAdmin { get; set; }

    [Display(Name = "Grant System Admin Access")]
    public bool IncludeSystemAdmin { get; set; }

    public IList<SelectListItem> RoleOptions { get; set; } = [];

    public Guid? CompanyId { get; set; }
}
