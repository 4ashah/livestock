namespace LivestockManager.Web.Models.DocumentViewModels;

public class DocumentListViewModel
{
    public IList<DocumentListItemViewModel> Items { get; set; } = new List<DocumentListItemViewModel>();

    public string? EntityType { get; set; }

    public Guid? EntityId { get; set; }

    public bool CanUpload { get; set; }

    public bool CanDelete { get; set; }
}
