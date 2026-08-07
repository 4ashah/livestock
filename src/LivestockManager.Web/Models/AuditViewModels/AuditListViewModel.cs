namespace LivestockManager.Web.Models.AuditViewModels;

public class AuditListViewModel
{
    public IList<AuditListItemViewModel> Items { get; set; } = new List<AuditListItemViewModel>();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public long TotalCount { get; set; }
    public string? EntityTypeFilter { get; set; }
    public string? EntityIdFilter { get; set; }
    public string? ActionFilter { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
