namespace LivestockManager.Domain.Common;

public class PageCapabilities
{
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanDetails { get; set; }
    public bool CanEdit { get; set; }
    public bool CanArchive { get; set; }
    public bool CanExport { get; set; }
    public bool CanExportCsv { get; set; }
    public bool CanUpload { get; set; }
    public bool CanDelete { get; set; }
    public bool CanDownload { get; set; }
    public bool CanDownloadPdf { get; set; }
    public bool CanConfirm { get; set; }
    public bool CanReverse { get; set; }
    public bool CanVoid { get; set; }
    public bool CanRecord { get; set; }
    public bool CanAddWeight { get; set; }
    public bool CanDischarge { get; set; }
    public bool CanGenerateFromPayment { get; set; }
    public bool CanCreateFromInvoice { get; set; }
    public bool CanAssignRole { get; set; }
    public bool CanViewAuditLogs { get; set; }
    public bool CanManageUsers { get; set; }
    public bool CanManageCompanies { get; set; }
    public bool CanManageSettings { get; set; }
}
