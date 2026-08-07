using LivestockManager.Application.DTOs.Invoices;

namespace LivestockManager.Application.Services.Invoices;

public interface IInvoiceService
{
    Task<InvoiceDetailDto> GetByIdAsync(Guid id, CancellationToken ct);
    Task<InvoiceDetailDto> ConfirmAsync(InvoiceConfirmDto dto, CancellationToken ct);
    Task CancelOrVoidAsync(Guid invoiceId, string reason, bool voidIfPaidExists, CancellationToken ct);
    Task RecalculateTotalsAsync(Guid invoiceId, CancellationToken ct);
    Task<byte[]> GetPdfAsync(Guid invoiceId, CancellationToken ct);
    Task<IList<InvoiceSummaryDto>> ListOutstandingAsync(Guid companyId, CancellationToken ct);
    Task<IList<InvoiceSummaryDto>> ListAsync(Guid companyId, CancellationToken ct);
}
