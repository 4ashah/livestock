namespace LivestockManager.Web.Models.UserViewModels;

public class UserRowViewModel
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
}
