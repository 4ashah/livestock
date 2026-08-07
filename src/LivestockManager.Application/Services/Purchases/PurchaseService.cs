using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Purchases;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using DE = LivestockManager.Domain.Entities;

namespace LivestockManager.Application.Services.Purchases;

public class PurchaseService : IPurchaseService
{
    private readonly IAppDbContext _db;
    private readonly IDateTime _dateTime;
    private readonly ISequenceGenerator _sequenceGenerator;

    public PurchaseService(
        IAppDbContext db,
        IDateTime dateTime,
        ISequenceGenerator sequenceGenerator)
    {
        _db = db;
        _dateTime = dateTime;
        _sequenceGenerator = sequenceGenerator;
    }

    public async Task<PurchaseDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct)
    {
        var purchase = await _db.Purchases.FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, ct)
            ?? throw new DomainException("Purchase not found.");

        var items = await _db.PurchaseItems.Where(i => i.PurchaseId == id).ToListAsync(ct);

        return MapToDetail(purchase, items);
    }

    public async Task<IList<PurchaseSummaryDto>> ListAsync(
        Guid companyId,
        Guid? supplierId,
        PurchaseStatus? status,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        CancellationToken ct)
    {
        var query = from p in _db.Purchases
                    join s in _db.Suppliers on p.SupplierId equals s.Id into ss
                    from s in ss.DefaultIfEmpty()
                    where p.CompanyId == companyId
                    select new { p, s };

        if (supplierId.HasValue)
            query = query.Where(x => x.p.SupplierId == supplierId.Value);

        if (status.HasValue)
            query = query.Where(x => x.p.Status == status.Value);

        if (fromDate.HasValue)
            query = query.Where(x => x.p.PurchaseDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.p.PurchaseDate <= toDate.Value);

        var result = await query
            .Select(x => new PurchaseSummaryDto
            {
                Id = x.p.Id,
                PurchaseNumber = x.p.PurchaseNumber,
                SupplierId = x.p.SupplierId,
                SupplierName = x.s != null ? x.s.Name : string.Empty,
                PurchaseDate = x.p.PurchaseDate,
                Status = x.p.Status,
                Subtotal = x.p.Subtotal,
                TaxTotal = x.p.TaxTotal,
                GrandTotal = x.p.GrandTotal,
                PaymentStatus = x.p.PaymentStatus,
                CreatedAt = x.p.CreatedAt
            })
            .ToListAsync(ct);

        return result;
    }

    public async Task<PurchaseDetailDto> CreateDraftAsync(PurchaseCreateDto dto, Guid companyId, CancellationToken ct)
    {
        var resolvedCompanyId = companyId == Guid.Empty ? dto.CompanyId : companyId;
        if (resolvedCompanyId == Guid.Empty)
            throw new DomainException("CompanyId is required.");

        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == dto.SupplierId && s.CompanyId == resolvedCompanyId, ct)
            ?? throw new DomainException("Supplier not found.");

        var purchase = new DE.Purchase(resolvedCompanyId, dto.FarmId, dto.SupplierId, dto.PurchaseDate)
        {
            SupplierReference = dto.SupplierReference,
            Currency = dto.Currency ?? Currency.USD,
            DiscountPct = dto.DiscountPct ?? 0,
            TaxRate = dto.TaxRate ?? 0,
            PaymentStatus = dto.PaymentStatus,
            Notes = dto.Notes,
            DocumentId = dto.DocumentId,
            Status = PurchaseStatus.Draft
        };

        _db.Purchases.Add(purchase);
        await _db.SaveChangesAsync(ct);

        var items = new List<DE.PurchaseItem>();
        foreach (var itemDto in dto.Items)
        {
            var lineNo = itemDto.LineNo ?? (items.Count + 1);
            var item = new DE.PurchaseItem(purchase.Id, lineNo, itemDto.ItemType, itemDto.Quantity, itemDto.UnitCost)
            {
                Description = itemDto.Description,
                UnitWeight = itemDto.UnitWeight ?? 0,
                WeightUnit = itemDto.WeightUnit ?? WeightUnit.Kg,
                DiscountPct = itemDto.DiscountPct ?? 0,
                TaxRate = itemDto.TaxRate ?? 0,
                LivestockId = itemDto.LivestockId
            };
            CalculateItemTotals(item);
            _db.PurchaseItems.Add(item);
            items.Add(item);
        }

        await _db.SaveChangesAsync(ct);

        purchase.Items = items;
        UpdatePurchaseTotals(purchase);
        purchase.OutstandingAmount = purchase.GrandTotal - purchase.AmountPaid;
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(purchase.Id, resolvedCompanyId, ct);
    }

    public async Task<PurchaseDetailDto> AddItemAsync(Guid purchaseId, PurchaseItemCreateDto itemDto, Guid companyId, CancellationToken ct)
    {
        var purchase = await _db.Purchases.FirstOrDefaultAsync(p => p.Id == purchaseId && p.CompanyId == companyId, ct)
            ?? throw new DomainException("Purchase not found.");

        if (purchase.Status != PurchaseStatus.Draft)
            throw new DomainException("Only draft purchases can be modified.");

        var existingItems = await _db.PurchaseItems.Where(i => i.PurchaseId == purchaseId).ToListAsync(ct);
        var nextLineNo = itemDto.LineNo ?? (existingItems.Max(i => (int?)i.LineNo) ?? 0) + 1;

        var item = new DE.PurchaseItem(purchase.Id, nextLineNo, itemDto.ItemType, itemDto.Quantity, itemDto.UnitCost)
        {
            Description = itemDto.Description,
            UnitWeight = itemDto.UnitWeight ?? 0,
            WeightUnit = itemDto.WeightUnit ?? WeightUnit.Kg,
            DiscountPct = itemDto.DiscountPct ?? 0,
            TaxRate = itemDto.TaxRate ?? 0,
            LivestockId = itemDto.LivestockId
        };
        CalculateItemTotals(item);
        _db.PurchaseItems.Add(item);
        await _db.SaveChangesAsync(ct);

        existingItems.Add(item);
        purchase.Items = existingItems;
        UpdatePurchaseTotals(purchase);
        purchase.OutstandingAmount = purchase.GrandTotal - purchase.AmountPaid;
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(purchaseId, companyId, ct);
    }

    public async Task<PurchaseDetailDto> RemoveItemAsync(Guid purchaseId, Guid purchaseItemId, Guid companyId, CancellationToken ct)
    {
        var purchase = await _db.Purchases.FirstOrDefaultAsync(p => p.Id == purchaseId && p.CompanyId == companyId, ct)
            ?? throw new DomainException("Purchase not found.");

        if (purchase.Status != PurchaseStatus.Draft)
            throw new DomainException("Only draft purchases can be modified.");

        var item = await _db.PurchaseItems.FirstOrDefaultAsync(i => i.Id == purchaseItemId && i.PurchaseId == purchaseId, ct)
            ?? throw new DomainException("Purchase item not found.");

        _db.PurchaseItems.Remove(item);
        await _db.SaveChangesAsync(ct);

        var remainingItems = await _db.PurchaseItems.Where(i => i.PurchaseId == purchaseId).ToListAsync(ct);
        purchase.Items = remainingItems;
        UpdatePurchaseTotals(purchase);
        purchase.OutstandingAmount = purchase.GrandTotal - purchase.AmountPaid;
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(purchaseId, companyId, ct);
    }

    public async Task<PurchaseDetailDto> UpdateTotalsAsync(Guid purchaseId, Guid companyId, CancellationToken ct)
    {
        var purchase = await _db.Purchases.FirstOrDefaultAsync(p => p.Id == purchaseId && p.CompanyId == companyId, ct)
            ?? throw new DomainException("Purchase not found.");

        var items = await _db.PurchaseItems.Where(i => i.PurchaseId == purchaseId).ToListAsync(ct);

        foreach (var item in items)
            CalculateItemTotals(item);

        purchase.Items = items;
        UpdatePurchaseTotals(purchase);
        purchase.OutstandingAmount = purchase.GrandTotal - purchase.AmountPaid;
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(purchaseId, companyId, ct);
    }

    public async Task<PurchaseDetailDto> PostPurchaseAsync(PurchasePostDto dto, Guid companyId, CancellationToken ct)
    {
        using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            var purchase = await _db.Purchases.FirstOrDefaultAsync(p => p.Id == dto.Id && p.CompanyId == companyId, ct)
                ?? throw new DomainException("Purchase not found.");

            if (purchase.Status != PurchaseStatus.Draft)
                throw new DomainException("Only draft purchases can be posted.");

            var items = await _db.PurchaseItems.Where(i => i.PurchaseId == dto.Id).ToListAsync(ct);

            if (items.Count == 0)
                throw new DomainException("Purchase must have at least one item to be posted.");

            var year = purchase.PurchaseDate.Year;
            var scopedKey = $"PUR:{year}";
            var rawNumber = await _sequenceGenerator.GenerateDocumentNumberAsync(purchase.CompanyId, scopedKey);
            var numericSuffix = ExtractNumeric(rawNumber, scopedKey);
            purchase.PurchaseNumber = $"PUR-{year}-{numericSuffix:D5}";

            foreach (var item in items)
            {
                if (item.ItemType == PurchaseItemType.Livestock && item.LivestockId == null)
                {
                    var livestockType = LivestockType.PurchasedCastratedRam;
                    var livestockIdSeq = await _sequenceGenerator.GenerateLivestockIdAsync(purchase.CompanyId, livestockType);
                    var initialWeight = item.UnitWeight > 0 ? item.UnitWeight : 1m;
                    var weightUnit = item.WeightUnit;

                    var livestock = new DE.Livestock(
                        purchase.CompanyId,
                        livestockIdSeq,
                        livestockType,
                        purchase.PurchaseDate,
                        initialWeight,
                        weightUnit,
                        item.LineTotal,
                        purchase.FarmId)
                    {
                        Comments = $"Purchased via {purchase.PurchaseNumber ?? $"Purchase {purchase.Id}"}"
                    };

                    _db.Livestock.Add(livestock);
                    await _db.SaveChangesAsync(ct);

                    if (item.UnitWeight > 0)
                    {
                        var weightRecord = new DE.LivestockWeight(livestock.Id, item.UnitWeight, weightUnit, purchase.PurchaseDate)
                        {
                            Notes = "Initial weight from purchase"
                        };
                        _db.LivestockWeights.Add(weightRecord);
                    }

                    item.LivestockId = livestock.Id;
                }
            }

            purchase.Status = PurchaseStatus.Posted;
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return await GetByIdAsync(dto.Id, companyId, ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<PurchaseDetailDto> VoidPurchaseAsync(Guid id, string reason, Guid companyId, CancellationToken ct)
    {
        var purchase = await _db.Purchases.FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, ct)
            ?? throw new DomainException("Purchase not found.");

        if (purchase.Status != PurchaseStatus.Posted)
            throw new DomainException("Only posted purchases can be voided.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("A reason is required to void a purchase.");

        purchase.Status = PurchaseStatus.Voided;
        purchase.Notes = string.IsNullOrWhiteSpace(purchase.Notes)
            ? $"Voided: {reason}"
            : $"{purchase.Notes}; Voided: {reason}";

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, companyId, ct);
    }

    private static void CalculateItemTotals(DE.PurchaseItem item)
    {
        var netBeforeDiscount = Math.Round(item.Quantity * item.UnitCost, 2, MidpointRounding.AwayFromZero);
        var discountAmt = Math.Round(netBeforeDiscount * item.DiscountPct / 100m, 2, MidpointRounding.AwayFromZero);
        var netAfterDiscount = netBeforeDiscount - discountAmt;
        var itemTax = Math.Round(netAfterDiscount * item.TaxRate / 100m, 2, MidpointRounding.AwayFromZero);
        item.LineTotal = netAfterDiscount + itemTax;
    }

    private static void UpdatePurchaseTotals(DE.Purchase purchase)
    {
        var items = purchase.Items ?? new List<DE.PurchaseItem>();
        decimal subtotal = 0;
        decimal taxTotal = 0;

        foreach (var item in items)
        {
            var netBeforeDiscount = Math.Round(item.Quantity * item.UnitCost, 2, MidpointRounding.AwayFromZero);
            var discountAmt = Math.Round(netBeforeDiscount * item.DiscountPct / 100m, 2, MidpointRounding.AwayFromZero);
            var netAfterDiscount = netBeforeDiscount - discountAmt;
            var itemTax = Math.Round(netAfterDiscount * item.TaxRate / 100m, 2, MidpointRounding.AwayFromZero);
            subtotal += netAfterDiscount;
            taxTotal += itemTax;
        }

        purchase.Subtotal = subtotal;
        purchase.TaxTotal = taxTotal;
        purchase.GrandTotal = subtotal + taxTotal;
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

    private static PurchaseDetailDto MapToDetail(DE.Purchase p, List<DE.PurchaseItem> items) => new()
    {
        Id = p.Id,
        CompanyId = p.CompanyId,
        FarmId = p.FarmId,
        SupplierId = p.SupplierId,
        PurchaseNumber = p.PurchaseNumber,
        SupplierReference = p.SupplierReference,
        PurchaseDate = p.PurchaseDate,
        Status = p.Status,
        Currency = p.Currency,
        DiscountPct = p.DiscountPct,
        TaxRate = p.TaxRate,
        Subtotal = p.Subtotal,
        TaxTotal = p.TaxTotal,
        GrandTotal = p.GrandTotal,
        AmountPaid = p.AmountPaid,
        OutstandingAmount = p.OutstandingAmount,
        PaymentStatus = p.PaymentStatus,
        Notes = p.Notes,
        DocumentId = p.DocumentId,
        Items = items.Select(MapToLineDto).ToList()
    };

    private static PurchaseItemLineDto MapToLineDto(DE.PurchaseItem i) => new()
    {
        Id = i.Id,
        LineNo = i.LineNo,
        ItemType = i.ItemType,
        Description = i.Description,
        Quantity = i.Quantity,
        UnitWeight = i.UnitWeight,
        WeightUnit = i.WeightUnit,
        UnitCost = i.UnitCost,
        DiscountPct = i.DiscountPct,
        TaxRate = i.TaxRate,
        LineTotal = i.LineTotal,
        LivestockId = i.LivestockId
    };
}
