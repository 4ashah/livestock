using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LivestockManager.Web.Models.UserViewModels;

public class UserAddViewModel
{
    [Required]
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Role")]
    public string SelectedRole { get; set; } = "Viewer";

    public IList<SelectListItem> RoleOptions { get; set; } = new List<SelectListItem>();

    public string TemporaryPassword { get; } = "Dev@123456";
}
