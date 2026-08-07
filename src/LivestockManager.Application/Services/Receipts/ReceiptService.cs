using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Receipts;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace LivestockManager.Application.Services.Receipts;

public class ReceiptService : IReceiptService
{
    private readonly IAppDbContext _db;
    private readonly IDateTime _dateTime;
    private readonly ISequenceGenerator _sequenceGenerator;

    public ReceiptService(
        IAppDbContext db,
        IDateTime dateTime,
        ISequenceGenerator sequenceGenerator)
    {
        _db = db;
        _dateTime = dateTime;
        _sequenceGenerator = sequenceGenerator;
    }

    public async Task<ReceiptDetailDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var receipt = await _db.Receipts.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new DomainException($"Receipt {id} not found.");

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == receipt.CustomerId, ct);

        string? invoiceNumber = null;
        if (receipt.PaymentId != Guid.Empty)
        {
            var payment = await _db.Payments
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == receipt.PaymentId, ct);
            invoiceNumber = payment?.Invoice?.InvoiceNumber;
        }

        return new ReceiptDetailDto
        {
            Id = receipt.Id,
            CompanyId = receipt.CompanyId,
            PaymentId = receipt.PaymentId,
            CustomerId = receipt.CustomerId,
            ReceiptNumber = receipt.ReceiptNumber,
            ReceiptDate = receipt.ReceiptDate,
            CustomerName = receipt.CustomerName ?? customer?.Name,
            CustomerAddress = receipt.CustomerAddress,
            CustomerTaxNumber = receipt.CustomerTaxNumber ?? customer?.TaxNumber,
            AmountReceived = receipt.AmountReceived,
            Currency = receipt.Currency,
            RunningInvoiceBalance = receipt.RunningInvoiceBalance,
            Notes = receipt.Notes,
            GeneratedByUserId = receipt.GeneratedByUserId,
            Status = receipt.Status,
            ReversalReason = receipt.ReversalReason,
            ReversedAt = receipt.ReversedAt,
            ReversedByUserId = receipt.ReversedByUserId,
            InvoiceNumber = invoiceNumber
        };
    }

    public async Task<IList<ReceiptSummaryDto>> ListForCustomerAsync(Guid companyId, Guid customerId, CancellationToken ct)
    {
        var query = from r in _db.Receipts
                    join c in _db.Customers on r.CustomerId equals c.Id into cs
                    from c in cs.DefaultIfEmpty()
                    where r.CompanyId == companyId && r.CustomerId == customerId
                    orderby r.ReceiptDate descending
                    select new ReceiptSummaryDto
                    {
                        Id = r.Id,
                        ReceiptNumber = r.ReceiptNumber,
                        ReceiptDate = r.ReceiptDate,
                        PaymentId = r.PaymentId,
                        CustomerId = r.CustomerId,
                        CustomerName = r.CustomerName ?? (c != null ? c.Name : null),
                        AmountReceived = r.AmountReceived,
                        Currency = r.Currency,
                        RunningInvoiceBalance = r.RunningInvoiceBalance,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt
                    };
        return await query.ToListAsync(ct);
    }

    public async Task<ReceiptDetailDto> GenerateForPaymentAsync(Guid paymentId, string? notes, CancellationToken ct)
    {
        using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            var payment = await _db.Payments
                .Include(p => p.Customer)
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == paymentId, ct)
                ?? throw new DomainException($"Payment {paymentId} not found.");

            if (payment.IsReversed)
                throw new DomainException("Cannot generate receipt for a reversed payment.");

            var year = payment.PaymentDate.Year;
            var sequencePrefix = $"RCP:{year}";
            var sequenceResult = await _sequenceGenerator.GenerateDocumentNumberAsync(payment.CompanyId, sequencePrefix);
            var receiptNumber = $"RCP-{year}-{sequenceResult.Substring(sequencePrefix.Length).PadLeft(5, '0')}";

            if (payment.Invoice != null)
            {
                var invPayments = await _db.Payments
                    .Where(p => p.InvoiceId == payment.InvoiceId)
                    .ToListAsync(ct);
                payment.Invoice.Payments = invPayments;
                payment.Invoice.RecalculatePaidAmountFromPayments();
                payment.Invoice.UpdateStatusFromBalances(_dateTime.Now);
            }

            var runningBalance = payment.Invoice?.OutstandingAmount ?? 0m;

            var customerAddress = FormatAddress(payment.Customer?.BillingAddress);

            var receipt = new Receipt(payment.CompanyId, payment.Id, payment.CustomerId, payment.PaymentDate, payment.Amount)
            {
                ReceiptNumber = receiptNumber,
                CustomerName = payment.Customer?.Name,
                CustomerAddress = customerAddress,
                CustomerTaxNumber = payment.Customer?.TaxNumber,
                Currency = payment.Invoice?.Currency ?? Currency.USD,
                RunningInvoiceBalance = runningBalance,
                Notes = notes,
                GeneratedByUserId = payment.ProcessedByUserId,
                Status = ReceiptStatus.Issued
            };

            _db.Receipts.Add(receipt);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return await GetByIdAsync(receipt.Id, ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<ReceiptDetailDto> ReverseReceiptAsync(Guid receiptId, string reason, Guid reversedByUserId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reversal reason cannot be empty or whitespace.", nameof(reason));

        using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            var receipt = await _db.Receipts
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r => r.Id == receiptId, ct)
                ?? throw new DomainException($"Receipt {receiptId} not found.");

            if (receipt.Status != ReceiptStatus.Issued)
                throw new DomainException($"Receipt must be in Issued status to reverse. Current status: {receipt.Status}");

            receipt.Status = ReceiptStatus.Reversed;
            receipt.ReversalReason = reason;
            receipt.ReversedAt = DateTimeOffset.UtcNow;
            receipt.ReversedByUserId = reversedByUserId;

            if (receipt.Payment != null && !receipt.Payment.IsReversed)
            {
                decimal invoiceGrandTotal = 0m;
                Invoice? invoice = null;
                if (receipt.Payment.InvoiceId.HasValue)
                {
                    invoice = await _db.Invoices
                        .FirstOrDefaultAsync(i => i.Id == receipt.Payment.InvoiceId.Value, ct);
                    invoiceGrandTotal = invoice?.GrandTotal ?? 0m;
                }

                receipt.Payment.ReversePayment(invoiceGrandTotal, reason, reversedByUserId);

                if (invoice != null)
                {
                    var invPayments = await _db.Payments
                        .Where(p => p.InvoiceId == invoice.Id)
                        .ToListAsync(ct);
                    invoice.Payments = invPayments;
                    invoice.RecalculatePaidAmountFromPayments();
                    invoice.UpdateStatusFromBalances(_dateTime.Now);
                }
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return await GetByIdAsync(receipt.Id, ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static string? FormatAddress(Domain.ValueObjects.Address? address)
    {
        if (address == null) return null;

        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(address.Street1))
            sb.Append(address.Street1);
        if (!string.IsNullOrWhiteSpace(address.Street2))
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(address.Street2);
        }
        if (!string.IsNullOrWhiteSpace(address.City))
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(address.City);
        }
        if (!string.IsNullOrWhiteSpace(address.State))
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(address.State);
        }
        if (!string.IsNullOrWhiteSpace(address.PostalCode))
        {
            if (sb.Length > 0) sb.Append(" ");
            sb.Append(address.PostalCode);
        }
        if (!string.IsNullOrWhiteSpace(address.Country))
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(address.Country);
        }
        return sb.Length == 0 ? null : sb.ToString();
    }
}
