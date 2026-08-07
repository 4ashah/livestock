using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Customers;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace LivestockManager.Application.Services.Customers;

public class CustomerBalanceService : ICustomerBalanceService
{
    private readonly IAppDbContext _db;
    private readonly IDateTime _dateTime;

    public CustomerBalanceService(
        IAppDbContext db,
        IDateTime dateTime)
    {
        _db = db;
        _dateTime = dateTime;
    }

    public async Task<CustomerBalanceDto> GetBalanceAsync(Guid companyId, Guid customerId, CancellationToken ct)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && c.CompanyId == companyId, ct)
            ?? throw new DomainException($"Customer {customerId} not found.");

        var invoiceStatusesToExclude = new[] { InvoiceStatus.Cancelled, InvoiceStatus.Voided };

        var totalInvoiced = await _db.Invoices
            .Where(i => i.CompanyId == companyId
                && i.CustomerId == customerId
                && !invoiceStatusesToExclude.Contains(i.Status))
            .SumAsync(i => (decimal?)i.GrandTotal, ct) ?? 0m;

        var totalPaid = await _db.Payments
            .Where(p => p.CompanyId == companyId
                && p.CustomerId == customerId
                && !p.IsReversed)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        var today = _dateTime.Now;
        var totalOverdue = await _db.Invoices
            .Where(i => i.CompanyId == companyId
                && i.CustomerId == customerId
                && !invoiceStatusesToExclude.Contains(i.Status)
                && i.DueDate < today
                && (i.GrandTotal - i.PaidAmount) > 0.001m)
            .SumAsync(i => (decimal?)(i.GrandTotal - i.PaidAmount), ct) ?? 0m;

        var lastPaymentDate = await _db.Payments
            .Where(p => p.CompanyId == companyId
                && p.CustomerId == customerId
                && !p.IsReversed)
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => (DateTimeOffset?)p.PaymentDate)
            .FirstOrDefaultAsync(ct);

        var companyCurrency = await _db.Companies
            .Where(c => c.Id == companyId)
            .Select(c => c.Currency)
            .FirstOrDefaultAsync(ct);

        return new CustomerBalanceDto
        {
            CustomerId = customer.Id,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.Name,
            Currency = companyCurrency,
            TotalInvoiced = totalInvoiced,
            TotalPaid = totalPaid,
            TotalOutstanding = totalInvoiced - totalPaid,
            TotalOverdue = totalOverdue,
            LastPaymentDate = lastPaymentDate
        };
    }

    public async Task<IList<CustomerBalanceDto>> ListBalancesAsync(Guid companyId, CancellationToken ct)
    {
        var customers = await _db.Customers
            .Where(c => c.CompanyId == companyId)
            .ToListAsync(ct);

        var invoiceStatusesToExclude = new[] { InvoiceStatus.Cancelled, InvoiceStatus.Voided };
        var today = _dateTime.Now;

        var invoiceAggregates = await _db.Invoices
            .Where(i => i.CompanyId == companyId && !invoiceStatusesToExclude.Contains(i.Status))
            .GroupBy(i => i.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                TotalInvoiced = g.Sum(i => (decimal?)i.GrandTotal) ?? 0m
            })
            .ToDictionaryAsync(g => g.CustomerId, g => g.TotalInvoiced, ct);

        var overdueAggregates = await _db.Invoices
            .Where(i => i.CompanyId == companyId
                && !invoiceStatusesToExclude.Contains(i.Status)
                && i.DueDate < today
                && (i.GrandTotal - i.PaidAmount) > 0.001m)
            .GroupBy(i => i.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                TotalOverdue = g.Sum(i => (decimal?)(i.GrandTotal - i.PaidAmount)) ?? 0m
            })
            .ToDictionaryAsync(g => g.CustomerId, g => g.TotalOverdue, ct);

        var paymentAggregates = await _db.Payments
            .Where(p => p.CompanyId == companyId && !p.IsReversed)
            .GroupBy(p => p.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                TotalPaid = g.Sum(p => (decimal?)p.Amount) ?? 0m,
                LastPaymentDate = g.Max(p => (DateTimeOffset?)p.PaymentDate)
            })
            .ToDictionaryAsync(g => g.CustomerId, ct);

        var companyCurrency = await _db.Companies
            .Where(c => c.Id == companyId)
            .Select(c => c.Currency)
            .FirstOrDefaultAsync(ct);

        var result = new List<CustomerBalanceDto>();
        foreach (var customer in customers)
        {
            var totalInvoiced = invoiceAggregates.TryGetValue(customer.Id, out var inv) ? inv : 0m;
            var totalPaid = paymentAggregates.TryGetValue(customer.Id, out var payAgg) ? payAgg.TotalPaid : 0m;
            var totalOverdue = overdueAggregates.TryGetValue(customer.Id, out var ov) ? ov : 0m;
            var lastPaymentDate = paymentAggregates.TryGetValue(customer.Id, out var payAgg2) ? payAgg2.LastPaymentDate : null;

            result.Add(new CustomerBalanceDto
            {
                CustomerId = customer.Id,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.Name,
                Currency = companyCurrency,
                TotalInvoiced = totalInvoiced,
                TotalPaid = totalPaid,
                TotalOutstanding = totalInvoiced - totalPaid,
                TotalOverdue = totalOverdue,
                LastPaymentDate = lastPaymentDate
            });
        }

        return result;
    }

    public async Task<CustomerStatementDto> GetStatementAsync(Guid companyId, Guid customerId, DateTimeOffset fromDate, DateTimeOffset toDate, CancellationToken ct)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && c.CompanyId == companyId, ct)
            ?? throw new DomainException($"Customer {customerId} not found.");

        var invoiceStatusesToExclude = new[] { InvoiceStatus.Cancelled, InvoiceStatus.Voided };

        var priorInvoiced = await _db.Invoices
            .Where(i => i.CompanyId == companyId
                && i.CustomerId == customerId
                && !invoiceStatusesToExclude.Contains(i.Status)
                && i.InvoiceDate < fromDate)
            .SumAsync(i => (decimal?)i.GrandTotal, ct) ?? 0m;

        var priorPaid = await _db.Payments
            .Where(p => p.CompanyId == companyId
                && p.CustomerId == customerId
                && !p.IsReversed
                && p.PaymentDate < fromDate)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        var openingBalance = priorInvoiced - priorPaid + customer.OpeningBalance;

        var lines = new List<CustomerStatementLineDto>();

        var invoicesInRange = await _db.Invoices
            .Where(i => i.CompanyId == companyId
                && i.CustomerId == customerId
                && !invoiceStatusesToExclude.Contains(i.Status)
                && i.InvoiceDate >= fromDate
                && i.InvoiceDate <= toDate)
            .OrderBy(i => i.InvoiceDate)
            .Select(i => new
            {
                i.Id,
                i.InvoiceDate,
                i.InvoiceNumber,
                i.GrandTotal,
                Status = i.Status.ToString()
            })
            .ToListAsync(ct);

        var paymentsInRange = await _db.Payments
            .Where(p => p.CompanyId == companyId
                && p.CustomerId == customerId
                && !p.IsReversed
                && p.PaymentDate >= fromDate
                && p.PaymentDate <= toDate)
            .OrderBy(p => p.PaymentDate)
            .Select(p => new
            {
                p.Id,
                p.PaymentDate,
                p.Reference,
                p.Amount,
                Method = p.Method.ToString()
            })
            .ToListAsync(ct);

        var receiptsInRange = await _db.Receipts
            .Where(r => r.CompanyId == companyId
                && r.CustomerId == customerId
                && r.Status == ReceiptStatus.Issued
                && r.ReceiptDate >= fromDate
                && r.ReceiptDate <= toDate)
            .OrderBy(r => r.ReceiptDate)
            .Select(r => new
            {
                r.Id,
                r.ReceiptDate,
                r.ReceiptNumber,
                r.AmountReceived,
                r.Notes
            })
            .ToListAsync(ct);

        foreach (var inv in invoicesInRange)
        {
            lines.Add(new CustomerStatementLineDto
            {
                Date = inv.InvoiceDate,
                ReferenceNo = inv.InvoiceNumber,
                Type = CustomerStatementLineType.Invoice,
                Details = $"Invoice {inv.Status}",
                Debit = inv.GrandTotal,
                Credit = 0m
            });
        }

        foreach (var pay in paymentsInRange)
        {
            lines.Add(new CustomerStatementLineDto
            {
                Date = pay.PaymentDate,
                ReferenceNo = pay.Reference,
                Type = CustomerStatementLineType.Payment,
                Details = $"{pay.Method} payment",
                Debit = 0m,
                Credit = pay.Amount
            });
        }

        foreach (var rec in receiptsInRange)
        {
            lines.Add(new CustomerStatementLineDto
            {
                Date = rec.ReceiptDate,
                ReferenceNo = rec.ReceiptNumber,
                Type = CustomerStatementLineType.Receipt,
                Details = rec.Notes ?? "Receipt issued",
                Debit = 0m,
                Credit = rec.AmountReceived
            });
        }

        lines = lines.OrderBy(l => l.Date).ToList();

        var runningBalance = openingBalance;
        foreach (var line in lines)
        {
            runningBalance += line.Debit - line.Credit;
            line.RunningBalance = runningBalance;
        }

        var totalDebit = lines.Sum(l => l.Debit);
        var totalCredit = lines.Sum(l => l.Credit);
        var closingBalance = openingBalance + totalDebit - totalCredit;

        var companyCurrency = await _db.Companies
            .Where(c => c.Id == companyId)
            .Select(c => c.Currency)
            .FirstOrDefaultAsync(ct);

        var customerAddress = FormatAddress(customer.BillingAddress);

        return new CustomerStatementDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CustomerCode = customer.CustomerCode,
            CustomerAddress = customerAddress,
            TaxNumber = customer.TaxNumber,
            Currency = companyCurrency,
            StatementFrom = fromDate,
            StatementTo = toDate,
            OpeningBalance = openingBalance,
            ClosingBalance = closingBalance,
            Lines = lines
        };
    }

    public async Task<IList<AgingBucketDto>> GetAgingBucketsAsync(Guid companyId, Guid customerId, CancellationToken ct)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && c.CompanyId == companyId, ct)
            ?? throw new DomainException($"Customer {customerId} not found.");

        var invoiceStatusesToExclude = new[] { InvoiceStatus.Cancelled, InvoiceStatus.Voided };
        var today = _dateTime.Now;

        var outstandingInvoices = await _db.Invoices
            .Where(i => i.CompanyId == companyId
                && i.CustomerId == customerId
                && !invoiceStatusesToExclude.Contains(i.Status)
                && (i.GrandTotal - i.PaidAmount) > 0.001m)
            .Select(i => new
            {
                i.DueDate,
                Outstanding = i.GrandTotal - i.PaidAmount
            })
            .ToListAsync(ct);

        var buckets = new Dictionary<string, decimal>
        {
            ["0-30"] = 0m,
            ["31-60"] = 0m,
            ["61-90"] = 0m,
            ["91-180"] = 0m,
            ["180+"] = 0m
        };

        foreach (var inv in outstandingInvoices)
        {
            var daysOverdue = (today.Date - inv.DueDate.Date).Days;
            if (daysOverdue < 0) daysOverdue = 0;

            var outstanding = inv.Outstanding;

            if (daysOverdue <= 30)
                buckets["0-30"] += outstanding;
            else if (daysOverdue <= 60)
                buckets["31-60"] += outstanding;
            else if (daysOverdue <= 90)
                buckets["61-90"] += outstanding;
            else if (daysOverdue <= 180)
                buckets["91-180"] += outstanding;
            else
                buckets["180+"] += outstanding;
        }

        return new List<AgingBucketDto>
        {
            new AgingBucketDto { BucketName = "0-30", Amount = buckets["0-30"] },
            new AgingBucketDto { BucketName = "31-60", Amount = buckets["31-60"] },
            new AgingBucketDto { BucketName = "61-90", Amount = buckets["61-90"] },
            new AgingBucketDto { BucketName = "91-180", Amount = buckets["91-180"] },
            new AgingBucketDto { BucketName = "180+", Amount = buckets["180+"] }
        };
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
