using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Invoices;
using LivestockManager.Application.DTOs.Sales;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LivestockManager.Application.Services.Sales;

public class SaleService : ISaleService
{
    private readonly IAppDbContext _db;
    private readonly IDateTime _dateTime;
    private readonly ISequenceGenerator _sequenceGenerator;

    public SaleService(
        IAppDbContext db,
        IDateTime dateTime,
        ISequenceGenerator sequenceGenerator)
    {
        _db = db;
        _dateTime = dateTime;
        _sequenceGenerator = sequenceGenerator;
    }

    public async Task<SaleDetailDto> CreateDraftAsync(SaleCreateDto dto, Guid companyId, CancellationToken ct)
    {
        if (dto.CompanyId != companyId)
            dto.CompanyId = companyId;

        var sale = new Sale(dto.CompanyId, dto.CustomerId, dto.Date)
        {
            FarmId = dto.FarmId,
            Notes = dto.Notes,
            Status = SaleStatus.Draft
        };

        _db.Sales.Add(sale);
        await _db.SaveChangesAsync(ct);

        var items = new List<SaleItem>();
        foreach (var itemDto in dto.Items)
        {
            var item = new SaleItem(sale.Id, itemDto.UnitPrice, itemDto.Quantity, itemDto.Description)
            {
                LivestockId = itemDto.LivestockId,
                DiscountPercent = itemDto.DiscountPercent,
                TaxPercent = itemDto.TaxPercent
            };
            CalculateItemAmounts(item);
            _db.SaleItems.Add(item);
            items.Add(item);
        }
        await _db.SaveChangesAsync(ct);

        sale.Items = items;
        RecalculateSaleTotals(sale);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(sale.Id, companyId, ct);
    }

    public async Task<SaleDetailDto> ConfirmAsync(Guid saleId, SaleConfirmDto dto, Guid companyId, CancellationToken ct)
    {
        using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            var sale = await _db.Sales.FirstOrDefaultAsync(s => s.Id == saleId && s.CompanyId == companyId, ct)
                ?? throw new DomainException("Sale not found.");

            if (sale.Status != SaleStatus.Draft)
                throw new DomainException("Only draft sales can be confirmed.");

            var items = await _db.SaleItems.Where(i => i.SaleId == saleId).ToListAsync(ct);

            var year = sale.Date.Year;
            var scopedKey = $"SAL:{year}";
            var rawNumber = await _sequenceGenerator.GenerateDocumentNumberAsync(sale.CompanyId, scopedKey);
            var numericSuffix = ExtractNumeric(rawNumber, scopedKey);
            sale.SaleNumber = $"SAL-{year}-{numericSuffix:D5}";

            var invoiceNumber = await _sequenceGenerator.GenerateInvoiceNumberAsync(sale.CompanyId);
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == sale.CustomerId, ct);
            var dueDate = sale.Date.AddDays(customer?.PaymentTermsDays ?? 30);

            var invoice = new Invoice(sale.CompanyId, sale.CustomerId, sale.Date, dueDate)
            {
                SaleId = sale.Id,
                InvoiceNumber = invoiceNumber,
                Currency = Currency.USD,
                Subtotal = sale.Subtotal,
                DiscountTotal = sale.DiscountTotal,
                TaxTotal = sale.TaxTotal,
                ChargeTotal = sale.ChargeTotal,
                GrandTotal = sale.GrandTotal,
                Status = InvoiceStatus.Draft,
                Notes = dto.Notes
            };
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync(ct);

            foreach (var item in items)
            {
                var invoiceItem = new InvoiceItem(invoice.Id, item.Description ?? $"Item {item.Id}", item.Quantity, item.UnitPrice)
                {
                    SaleItemId = item.Id,
                    DiscountPercent = item.DiscountPercent,
                    DiscountAmount = item.DiscountAmount,
                    TaxPercent = item.TaxPercent,
                    TaxAmount = item.TaxAmount
                };
                _db.InvoiceItems.Add(invoiceItem);

                if (item.LivestockId.HasValue)
                {
                    var livestock = await _db.Livestock.FirstOrDefaultAsync(l => l.Id == item.LivestockId.Value && l.CompanyId == companyId, ct);
                    if (livestock != null && livestock.Status == LivestockStatus.Active)
                    {
                        livestock.Status = LivestockStatus.DischargedSold;
                        livestock.SoldAmount = item.LineTotal;
                        livestock.DischargeDate = sale.Date;
                        livestock.DischargeCondition = DischargeCondition.Sold;
                        livestock.SoldViaSaleItemId = item.Id;
                        livestock.ModifiedAt = _dateTime.Now;

                        var dischargeActivity = new LivestockActivity(livestock.Id, LivestockActivityType.Discharge, sale.Date,
                            $"Sold via sale {saleId} for {item.LineTotal}")
                        {
                            Metadata = $"SaleId:{saleId};SaleItemId:{item.Id};Amount:{item.LineTotal}"
                        };
                        _db.LivestockActivities.Add(dischargeActivity);
                    }
                }
            }

            sale.Status = SaleStatus.Confirmed;
            sale.ModifiedAt = _dateTime.Now;
            if (!string.IsNullOrWhiteSpace(dto.Notes))
                sale.Notes = dto.Notes;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return await GetByIdAsync(saleId, companyId, ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task CancelAsync(Guid saleId, string reason, Guid companyId, CancellationToken ct)
    {
        var sale = await _db.Sales.FirstOrDefaultAsync(s => s.Id == saleId && s.CompanyId == companyId, ct)
            ?? throw new DomainException("Sale not found.");

        if (sale.Status != SaleStatus.Draft)
            throw new DomainException("Only draft sales can be cancelled.");

        sale.Status = SaleStatus.Cancelled;
        sale.ModifiedAt = _dateTime.Now;
        sale.Notes = string.IsNullOrWhiteSpace(sale.Notes) ? reason : $"{sale.Notes}; Cancelled: {reason}";
        await _db.SaveChangesAsync(ct);
    }

    public async Task<SaleDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct)
    {
        var sale = await _db.Sales.FirstOrDefaultAsync(s => s.Id == id && s.CompanyId == companyId, ct)
            ?? throw new DomainException("Sale not found.");
        var items = await _db.SaleItems.Where(i => i.SaleId == id).ToListAsync(ct);
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == sale.CustomerId, ct);
        string? farmName = null;
        if (sale.FarmId.HasValue)
        {
            farmName = await _db.Farms
                .Where(f => f.Id == sale.FarmId.Value && f.CompanyId == companyId)
                .Select(f => f.Name)
                .FirstOrDefaultAsync(ct);
        }

        return MapToDetail(sale, items, customer?.Name, farmName);
    }

    public async Task<IList<SaleSummaryDto>> ListAsync(Guid companyId, CancellationToken ct)
    {
        var query = from s in _db.Sales
                    join c in _db.Customers on s.CustomerId equals c.Id into cs
                    from c in cs.DefaultIfEmpty()
                    join f in _db.Farms on s.FarmId equals f.Id into fs
                    from f in fs.DefaultIfEmpty()
                    let itemCount = _db.SaleItems.Where(i => i.SaleId == s.Id).Count()
                    where s.CompanyId == companyId
                    select new SaleSummaryDto
                    {
                        Id = s.Id,
                        CompanyId = s.CompanyId,
                        FarmId = s.FarmId,
                        FarmName = f != null ? f.Name : null,
                        CustomerId = s.CustomerId,
                        CustomerName = c != null ? c.Name : null,
                        SaleNumber = s.SaleNumber,
                        Date = s.Date,
                        GrandTotal = s.GrandTotal,
                        Status = s.Status,
                        ItemCount = itemCount
                    };
        return await query.ToListAsync(ct);
    }

    private static void CalculateItemAmounts(SaleItem item)
    {
        var baseAmount = item.Quantity * item.UnitPrice;
        item.DiscountAmount = baseAmount * item.DiscountPercent;
        var afterDiscount = baseAmount - item.DiscountAmount;
        item.TaxAmount = afterDiscount * item.TaxPercent;
    }

    private static void RecalculateSaleTotals(Sale sale)
    {
        var items = sale.Items ?? new List<SaleItem>();
        sale.Subtotal = items.Sum(i => i.Quantity * i.UnitPrice - i.DiscountAmount);
        sale.DiscountTotal = items.Sum(i => i.DiscountAmount);
        sale.TaxTotal = items.Sum(i => i.TaxAmount);
        sale.ChargeTotal = 0;
        sale.GrandTotal = sale.Subtotal + sale.TaxTotal + sale.ChargeTotal;
    }

    private static long ExtractNumeric(string raw, string prefix)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return 1;

        var suffix = raw.StartsWith(prefix, StringComparison.Ordinal)
            ? raw[prefix.Length..]
            : raw;

        if (long.TryParse(suffix, out var direct))
            return direct;

        var digits = new string(suffix.Where(char.IsDigit).ToArray());
        if (digits.Length > 0 && long.TryParse(digits, out var fromDigits))
            return fromDigits;

        return 1;
    }

    private static SaleDetailDto MapToDetail(Sale s, List<SaleItem> items, string? customerName, string? farmName) => new()
    {
        Id = s.Id,
        CompanyId = s.CompanyId,
        FarmId = s.FarmId,
        FarmName = farmName,
        CustomerId = s.CustomerId,
        CustomerName = customerName,
        SaleNumber = s.SaleNumber,
        Date = s.Date,
        Subtotal = s.Subtotal,
        DiscountTotal = s.DiscountTotal,
        TaxTotal = s.TaxTotal,
        ChargeTotal = s.ChargeTotal,
        GrandTotal = s.GrandTotal,
        Status = s.Status,
        Notes = s.Notes,
        CreatedAt = s.CreatedAt,
        ModifiedAt = s.ModifiedAt,
        Items = items.Select(MapToItemDto).ToList()
    };

    private static SaleItemDto MapToItemDto(SaleItem i) => new()
    {
        Id = i.Id,
        SaleId = i.SaleId,
        LivestockId = i.LivestockId,
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
