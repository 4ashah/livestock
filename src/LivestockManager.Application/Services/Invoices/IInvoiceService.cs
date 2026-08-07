using LivestockManager.Application.DTOs.Invoices;

namespace LivestockManager.Application.Services.Invoices;

public interface IInvoiceService
{
    Task<InvoiceDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct);
    Task<InvoiceDetailDto> ConfirmAsync(InvoiceConfirmDto dto, Guid companyId, CancellationToken ct);
    Task CancelOrVoidAsync(Guid invoiceId, string reason, bool voidIfPaidExists, Guid companyId, CancellationToken ct);
    Task RecalculateTotalsAsync(Guid invoiceId, Guid companyId, CancellationToken ct);
    Task<byte[]> GetPdfAsync(Guid invoiceId, Guid companyId, CancellationToken ct);
    Task<IList<InvoiceSummaryDto>> ListOutstandingAsync(Guid companyId, CancellationToken ct);
    Task<IList<InvoiceSummaryDto>> ListAsync(Guid companyId, CancellationToken ct);
}
