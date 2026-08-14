using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Invoices;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LivestockManager.Application.Services.Invoices;

public class InvoiceService : IInvoiceService
{
    private readonly IAppDbContext _db;
    private readonly IDateTime _dateTime;
    private readonly ISequenceGenerator _sequenceGenerator;
    private readonly IPdfGenerator _pdfGenerator;

    public InvoiceService(
        IAppDbContext db,
        IDateTime dateTime,
        ISequenceGenerator sequenceGenerator,
        IPdfGenerator pdfGenerator)
    {
        _db = db;
        _dateTime = dateTime;
        _sequenceGenerator = sequenceGenerator;
        _pdfGenerator = pdfGenerator;
    }

    public async Task<InvoiceDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId, ct)
            ?? throw new DomainException("Invoice not found.");

        var items = await _db.InvoiceItems.Where(it => it.InvoiceId == id).ToListAsync(ct);
        var payments = await _db.Payments.Where(p => p.InvoiceId == id).ToListAsync(ct);
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == invoice.CustomerId, ct);
        if (customer != null && customer.CompanyId != companyId)
            throw new DomainException("Customer does not belong to this company.");

        invoice.Items = items;
        invoice.Payments = payments;
        invoice.RecalculatePaidAmountFromPayments();

        return MapToDetail(invoice, items, customer?.Name);
    }

    public async Task<InvoiceDetailDto> ConfirmAsync(InvoiceConfirmDto dto, Guid companyId, CancellationToken ct)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == dto.InvoiceId && i.CompanyId == companyId, ct)
            ?? throw new DomainException("Invoice not found.");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new DomainException("Only draft invoices can be confirmed.");

        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
        {
            invoice.InvoiceNumber = await _sequenceGenerator.GenerateInvoiceNumberAsync(invoice.CompanyId);
        }

        var items = await _db.InvoiceItems.Where(it => it.InvoiceId == dto.InvoiceId).ToListAsync(ct);
        invoice.Items = items;
        invoice.DiscountTotal = items.Sum(i => i.DiscountAmount);
        invoice.UpdateTotalsFromItemsAndCharges();

        invoice.Status = InvoiceStatus.Confirmed;
        invoice.UpdateStatusFromBalances(_dateTime.Now);
        invoice.ModifiedAt = _dateTime.Now;

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(invoice.Id, companyId, ct);
    }

    public async Task CancelOrVoidAsync(Guid invoiceId, string reason, bool voidIfPaidExists, Guid companyId, CancellationToken ct)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId && i.CompanyId == companyId, ct)
            ?? throw new DomainException("Invoice not found.");

        var paidPayments = await _db.Payments
            .Where(p => p.InvoiceId == invoiceId && !p.IsReversed)
            .AnyAsync(ct);

        if (paidPayments && !voidIfPaidExists)
            throw new DomainException("Cannot cancel invoice with existing payments. Void instead.");

        invoice.Status = paidPayments ? InvoiceStatus.Voided : InvoiceStatus.Cancelled;
        invoice.Notes = string.IsNullOrWhiteSpace(invoice.Notes)
            ? $"{(paidPayments ? "Voided" : "Cancelled")}: {reason}"
            : $"{invoice.Notes}; {(paidPayments ? "Voided" : "Cancelled")}: {reason}";
        invoice.ModifiedAt = _dateTime.Now;

        await _db.SaveChangesAsync(ct);
    }

    public async Task RecalculateTotalsAsync(Guid invoiceId, Guid companyId, CancellationToken ct)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId && i.CompanyId == companyId, ct)
            ?? throw new DomainException("Invoice not found.");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new DomainException("Can only recalculate draft invoices.");

        var items = await _db.InvoiceItems.Where(it => it.InvoiceId == invoiceId).ToListAsync(ct);

        invoice.Items = items;
        invoice.DiscountTotal = items.Sum(i => i.DiscountAmount);
        invoice.UpdateTotalsFromItemsAndCharges();
        invoice.ModifiedAt = _dateTime.Now;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<byte[]> GetPdfAsync(Guid invoiceId, Guid companyId, CancellationToken ct)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId && i.CompanyId == companyId, ct)
            ?? throw new DomainException("Invoice not found.");

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == invoice.CompanyId, ct)
            ?? throw new DomainException("Company not found.");

        return await _pdfGenerator.GenerateInvoicePdfAsync(invoice, company);
    }

    public async Task<IList<InvoiceSummaryDto>> ListOutstandingAsync(Guid companyId, CancellationToken ct)
    {
        var now = _dateTime.Now;
        var query = from i in _db.Invoices
                    join c in _db.Customers on i.CustomerId equals c.Id into cs
                    from c in cs.DefaultIfEmpty()
                    where i.CompanyId == companyId
                          && i.Status != InvoiceStatus.Draft
                          && i.Status != InvoiceStatus.Cancelled
                          && i.Status != InvoiceStatus.Voided
                          && i.GrandTotal - i.PaidAmount > 0.0001m
                    select new InvoiceSummaryDto
                    {
                        Id = i.Id,
                        CompanyId = i.CompanyId,
                        CustomerId = i.CustomerId,
                        CustomerName = c != null ? c.Name : null,
                        InvoiceNumber = i.InvoiceNumber,
                        InvoiceDate = i.InvoiceDate,
                        DueDate = i.DueDate,
                        GrandTotal = i.GrandTotal,
                        PaidAmount = i.PaidAmount,
                        OutstandingAmount = i.GrandTotal - i.PaidAmount,
                        Status = (i.DueDate < now && i.GrandTotal - i.PaidAmount > 0.0001m)
                            ? InvoiceStatus.Overdue
                            : (i.PaidAmount <= 0.0001m ? i.Status :
                               (i.GrandTotal - i.PaidAmount <= 0.0001m ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid))
                    };
        return await query.ToListAsync(ct);
    }

    public async Task<IList<InvoiceSummaryDto>> ListAsync(Guid companyId, CancellationToken ct)
    {
        var query = from i in _db.Invoices
                    join c in _db.Customers on i.CustomerId equals c.Id into cs
                    from c in cs.DefaultIfEmpty()
                    where i.CompanyId == companyId
                    select new InvoiceSummaryDto
                    {
                        Id = i.Id,
                        CompanyId = i.CompanyId,
                        CustomerId = i.CustomerId,
                        CustomerName = c != null ? c.Name : null,
                        InvoiceNumber = i.InvoiceNumber,
                        InvoiceDate = i.InvoiceDate,
                        DueDate = i.DueDate,
                        GrandTotal = i.GrandTotal,
                        PaidAmount = i.PaidAmount,
                        OutstandingAmount = i.GrandTotal - i.PaidAmount,
                        Status = i.Status
                    };
        return await query.ToListAsync(ct);
    }

    private static InvoiceDetailDto MapToDetail(Invoice i, List<InvoiceItem> items, string? customerName) => new()
    {
        Id = i.Id,
        CompanyId = i.CompanyId,
        SaleId = i.SaleId,
        CustomerId = i.CustomerId,
        CustomerName = customerName,
        InvoiceNumber = i.InvoiceNumber,
        InvoiceDate = i.InvoiceDate,
        DueDate = i.DueDate,
        Currency = i.Currency,
        Subtotal = i.Subtotal,
        DiscountTotal = i.DiscountTotal,
        TaxTotal = i.TaxTotal,
        ChargeTotal = i.ChargeTotal,
        GrandTotal = i.GrandTotal,
        PaidAmount = i.PaidAmount,
        OutstandingAmount = i.OutstandingAmount,
        Status = i.Status,
        Notes = i.Notes,
        Terms = i.Terms,
        CreatedAt = i.CreatedAt,
        ModifiedAt = i.ModifiedAt,
        Items = items.Select(MapToItemDto).ToList()
    };

    private static InvoiceItemDto MapToItemDto(InvoiceItem i) => new()
    {
        Id = i.Id,
        InvoiceId = i.InvoiceId,
        SaleItemId = i.SaleItemId,
        Description = i.Description,
        Quantity = i.Quantity,
        UnitPrice = i.UnitPrice,
        DiscountPercent = i.DiscountPercent,
        DiscountAmount = i.DiscountAmount,
        TaxPercent = i.TaxPercent,
        TaxAmount = i.TaxAmount,
        LineTotal = i.LineTotal
    };
}
