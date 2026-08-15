namespace LivestockManager.Web.Models.UserViewModels;

public class UserManagementListViewModel
{
    public IList<UserRowViewModel> Items { get; set; } = new List<UserRowViewModel>();
    public bool CurrentUserCanResetPasswords { get; set; }
    public bool CurrentUserIsSystemAdmin { get; set; }
    public Guid? CurrentUserCompanyId { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
