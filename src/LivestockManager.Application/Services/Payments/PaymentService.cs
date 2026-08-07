using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Payments;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LivestockManager.Application.Services.Payments;

public class PaymentService : IPaymentService
{
    private readonly IAppDbContext _db;
    private readonly IDateTime _dateTime;
    private readonly ISequenceGenerator _sequenceGenerator;
    private readonly IPdfGenerator _pdfGenerator;

    public PaymentService(
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

    public async Task<PaymentDetailDto> PostAsync(PaymentCreateDto dto, CancellationToken ct)
    {
        using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            var payment = new Payment(dto.CompanyId, dto.CustomerId, dto.PaymentDate, dto.Method, dto.Amount)
            {
                Reference = dto.Reference,
                Notes = dto.Notes
            };

            if (dto.Allocations.Count == 1)
            {
                payment.InvoiceId = dto.Allocations[0].InvoiceId;
            }

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync(ct);

            foreach (var allocDto in dto.Allocations)
            {
                var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == allocDto.InvoiceId, ct)
                    ?? throw new DomainException($"Invoice {allocDto.InvoiceId} not found.");

                var invPayments = await _db.Payments.Where(p => p.InvoiceId == allocDto.InvoiceId).ToListAsync(ct);
                invoice.Payments = invPayments;
                invoice.RecalculatePaidAmountFromPayments();
                invoice.UpdateStatusFromBalances(_dateTime.Now);
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return await GetByIdAsync(payment.Id, ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task ReverseAsync(Guid paymentId, string reason, CancellationToken ct)
    {
        using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, ct)
                ?? throw new DomainException($"Payment {paymentId} not found.");

            if (payment.IsReversed)
                throw new DomainException("Payment is already reversed.");

            decimal invoiceGrandTotal = 0m;
            if (payment.InvoiceId.HasValue)
            {
                var invoiceForGrandTotal = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == payment.InvoiceId.Value, ct);
                invoiceGrandTotal = invoiceForGrandTotal?.GrandTotal ?? 0m;
            }

            var reversedByUserId = Guid.Empty;
            payment.ReversePayment(invoiceGrandTotal, reason, reversedByUserId);
            payment.ModifiedAt = _dateTime.Now;

            if (payment.InvoiceId.HasValue)
            {
                var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == payment.InvoiceId.Value, ct);
                if (invoice != null)
                {
                    var invPayments = await _db.Payments.Where(p => p.InvoiceId == payment.InvoiceId.Value).ToListAsync(ct);
                    invoice.Payments = invPayments;
                    invoice.RecalculatePaidAmountFromPayments();
                    invoice.UpdateStatusFromBalances(_dateTime.Now);
                }
            }

            var auditLog = new AuditLog("Payment.Reverse", _dateTime.Now)
            {
                CompanyId = payment.CompanyId,
                EntityType = "Payment",
                EntityId = paymentId.ToString(),
                NewValuesJson = $"ReversalReason:{reason}"
            };
            _db.AuditLogs.Add(auditLog);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<IList<PaymentSummaryDto>> ListAsync(Guid companyId, CancellationToken ct)
    {
        var query = from p in _db.Payments
                    join c in _db.Customers on p.CustomerId equals c.Id into cs
                    from c in cs.DefaultIfEmpty()
                    where p.CompanyId == companyId
                    select new PaymentSummaryDto
                    {
                        Id = p.Id,
                        CompanyId = p.CompanyId,
                        CustomerId = p.CustomerId,
                        CustomerName = c != null ? c.Name : null,
                        PaymentDate = p.PaymentDate,
                        Method = p.Method,
                        Amount = p.Amount,
                        Reference = p.Reference,
                        IsReversed = p.IsReversed
                    };
        return await query.ToListAsync(ct);
    }

    public async Task<PaymentDetailDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new DomainException($"Payment {id} not found.");

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == payment.CustomerId, ct);

        var allocations = new List<PaymentAllocationDto>();
        if (payment.InvoiceId.HasValue)
        {
            var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == payment.InvoiceId.Value, ct);
            allocations.Add(new PaymentAllocationDto
            {
                PaymentId = payment.Id,
                InvoiceId = payment.InvoiceId.Value,
                InvoiceNumber = invoice?.InvoiceNumber,
                Amount = payment.Amount,
                AllocatedAt = payment.CreatedAt
            });
        }

        return new PaymentDetailDto
        {
            Id = payment.Id,
            CompanyId = payment.CompanyId,
            CustomerId = payment.CustomerId,
            CustomerName = customer?.Name,
            InvoiceId = payment.InvoiceId,
            PaymentDate = payment.PaymentDate,
            Method = payment.Method,
            Amount = payment.Amount,
            Reference = payment.Reference,
            Notes = payment.Notes,
            IsReversed = payment.IsReversed,
            ReversalReason = payment.ReversalReason,
            ReversedAt = payment.ReversedAt,
            CreatedAt = payment.CreatedAt,
            ModifiedAt = payment.ModifiedAt,
            Allocations = allocations
        };
    }

    private void UpdateInvoiceStatus(Invoice invoice)
    {
        if (invoice.Status == InvoiceStatus.Cancelled || invoice.Status == InvoiceStatus.Voided)
            return;

        if (invoice.Status == InvoiceStatus.Draft)
            invoice.Status = InvoiceStatus.Confirmed;

        var outstanding = invoice.GrandTotal - invoice.PaidAmount;
        if (outstanding <= 0.0001m)
        {
            invoice.Status = InvoiceStatus.Paid;
        }
        else if (invoice.PaidAmount > 0.0001m)
        {
            invoice.Status = InvoiceStatus.PartiallyPaid;
        }
        else if (invoice.DueDate < _dateTime.Now)
        {
            invoice.Status = InvoiceStatus.Overdue;
        }
        else if (invoice.Status == InvoiceStatus.Overdue && invoice.DueDate >= _dateTime.Now)
        {
            invoice.Status = InvoiceStatus.Confirmed;
        }
    }
}
