using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Invoices;
using LivestockManager.Application.DTOs.Sales;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Helpers;
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

    public async Task<(decimal SuggestedPrice, SuggestedPricingMethod Method, decimal? Weight, DateTimeOffset? WeightDate, decimal? Rate)>
        GetSuggestedSalePriceAsync(Guid livestockId, Guid companyId, CancellationToken ct)
    {
        var livestock = await _db.Livestock
            .FirstOrDefaultAsync(l => l.Id == livestockId && l.CompanyId == companyId, ct);

        if (livestock == null)
            throw new DomainException("Livestock not found.");

        var suggestedPrice = Math.Round(livestock.PurchaseAmount * 1.3m, 2, MidpointRounding.AwayFromZero);
        var weight = livestock.CurrentWeight;
        var weightDate = livestock.CurrentWeightDate;
        decimal? rate = null;

        return (suggestedPrice, SuggestedPricingMethod.CostMarkupLegacy, weight, weightDate, rate);
    }

    public async Task<SaleDetailDto> CreateDraftAsync(SaleCreateDto dto, Guid companyId, CancellationToken ct)
    {
        if (dto.CompanyId != companyId)
            dto.CompanyId = companyId;

        ValidateCosts(dto.CommissionAmount, dto.SellerTaxAmount, dto.TransportationAmount,
            dto.OtherCostAmount, dto.OtherCostDescription);

        using var tx = await _db.BeginTransactionAsync(ct);
        Sale sale = null!;
        try
        {
            sale = new Sale(dto.CompanyId, dto.CustomerId, dto.Date)
            {
                FarmId = dto.FarmId,
                Notes = dto.Notes,
                Status = SaleStatus.Draft,
                CommissionAmount = dto.CommissionAmount,
                SellerTaxAmount = dto.SellerTaxAmount,
                TransportationAmount = dto.TransportationAmount,
                OtherCostAmount = dto.OtherCostAmount,
                OtherCostDescription = dto.OtherCostDescription,
                CostAllocationMethod = dto.CostAllocationMethod
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

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

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
            sale.Items = items;

            RecalculateSaleTotals(sale);

            ValidateCosts(sale.CommissionAmount, sale.SellerTaxAmount, sale.TransportationAmount,
                sale.OtherCostAmount, sale.OtherCostDescription);

            foreach (var item in items)
            {
                if (item.LivestockId.HasValue)
                {
                    var (suggestedPrice, method, weight, weightDate, rate) =
                        await GetSuggestedSalePriceAsync(item.LivestockId.Value, companyId, ct);

                    item.SuggestedPrice = suggestedPrice;
                    item.SuggestedPriceMethod = method;
                    item.SuggestedWeight = weight;
                    item.SuggestedWeightDate = weightDate;
                    item.SuggestedRate = rate;

                    var roundedUnit = Math.Round(item.UnitPrice, 2, MidpointRounding.AwayFromZero);
                    var roundedSuggested = Math.Round(suggestedPrice, 2, MidpointRounding.AwayFromZero);
                    item.PriceSource = Math.Abs(roundedUnit - roundedSuggested) < 0.005m
                        ? PriceSource.Suggested
                        : PriceSource.ManualOverride;

                    item.FinalSalePrice = roundedUnit;
                }
                else
                {
                    item.FinalSalePrice = Math.Round(item.UnitPrice, 2, MidpointRounding.AwayFromZero);
                }
            }

            ApplyCostAllocation(sale, items);

            var year = sale.Date.Year;
            var scopedKey = $"SAL:{year}";
            var rawNumber = await _sequenceGenerator.GenerateDocumentNumberAsync(sale.CompanyId, scopedKey);
            var numericSuffix = ExtractNumeric(rawNumber, scopedKey);
            sale.SaleNumber = $"SAL-{year}-{numericSuffix:D5}";

            var invoiceNumber = await _sequenceGenerator.GenerateInvoiceNumberAsync(sale.CompanyId);
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == sale.CustomerId && c.CompanyId == sale.CompanyId, ct);
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

    public async Task<SaleBulkAddResultDto> BulkAddLivestockToDraftSaleAsync(
        Guid saleId, Guid companyId, IEnumerable<Guid> livestockIds, Guid? actingUserId, CancellationToken ct)
    {
        using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            var result = new SaleBulkAddResultDto();

            var sale = await _db.Sales
                .FirstOrDefaultAsync(s => s.Id == saleId && s.CompanyId == companyId, ct)
                ?? throw new DomainException("Sale not found.");

            if (sale.Status != SaleStatus.Draft)
                throw new DomainException("Bulk add is only allowed for draft sales.");

            var existingItemLivestockIdsList = await _db.SaleItems
                .Where(i => i.SaleId == saleId && i.LivestockId.HasValue)
                .Select(i => i.LivestockId!.Value)
                .ToListAsync(ct);
            var existingItemLivestockIds = new HashSet<Guid>(existingItemLivestockIdsList);

            var submittedIds = livestockIds?.ToList() ?? new List<Guid>();
            var seenInRequest = new HashSet<Guid>();
            var eligibleIds = new List<Guid>();

            foreach (var lid in submittedIds)
            {
                if (!seenInRequest.Add(lid))
                {
                    result.AlreadyPresentCount++;
                    result.Messages.Add($"Livestock {lid} duplicated in request, skipping.");
                    continue;
                }

                if (existingItemLivestockIds.Contains(lid))
                {
                    result.AlreadyPresentCount++;
                    result.Messages.Add($"Livestock {lid} already in sale items, skipping.");
                    continue;
                }

                eligibleIds.Add(lid);
            }

            var eligibleLivestock = await _db.Livestock
                .Where(l => eligibleIds.Contains(l.Id) && l.CompanyId == companyId)
                .ToListAsync(ct);

            var livestockById = eligibleLivestock.ToDictionary(l => l.Id);
            var addedItems = new List<SaleItem>();

            foreach (var lid in eligibleIds)
            {
                if (!livestockById.TryGetValue(lid, out var l))
                {
                    result.IneligibleCount++;
                    result.Messages.Add($"Livestock {lid} not found or company mismatch.");
                    continue;
                }

                if (l.Status != LivestockStatus.Active)
                {
                    result.IneligibleCount++;
                    result.Messages.Add($"Livestock {l.LivestockId} is not Active (status={l.Status}).");
                    continue;
                }

                if (l.DischargeCondition.HasValue)
                {
                    result.IneligibleCount++;
                    result.Messages.Add($"Livestock {l.LivestockId} has discharge set.");
                    continue;
                }

                var (suggestedPrice, method, weight, weightDate, rate) =
                    await GetSuggestedSalePriceAsync(lid, companyId, ct);

                var unitPrice = Math.Round(suggestedPrice, 2, MidpointRounding.AwayFromZero);

                var item = new SaleItem(sale.Id, unitPrice, 1, $"Livestock #{l.LivestockId}")
                {
                    LivestockId = lid,
                    DiscountPercent = 0,
                    TaxPercent = 0,
                    SuggestedPrice = suggestedPrice,
                    SuggestedPriceMethod = method,
                    SuggestedWeight = weight,
                    SuggestedWeightDate = weightDate,
                    SuggestedRate = rate,
                    FinalSalePrice = unitPrice,
                    PriceSource = PriceSource.Suggested
                };

                CalculateItemAmounts(item);
                _db.SaleItems.Add(item);
                addedItems.Add(item);
                result.AddedCount++;
            }

            await _db.SaveChangesAsync(ct);

            var allItems = await _db.SaleItems.Where(i => i.SaleId == saleId).ToListAsync(ct);
            sale.Items = allItems;
            RecalculateSaleTotals(sale);
            await _db.SaveChangesAsync(ct);

            result.Items = addedItems.Select(MapToItemDto).ToList();

            await tx.CommitAsync(ct);
            return result;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<SaleDetailDto> ReverseSaleAsync(
        SaleReversalDto dto, Guid companyId, Guid actingUserId, string? actingUserRole, CancellationToken ct)
    {
        using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            if (dto == null)
                throw new DomainException("Reversal DTO required.");

            var trimmedReason = (dto.ReversalReason ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedReason))
                throw new DomainException("Reversal reason is required.");
            if (trimmedReason.Length > 500)
                throw new DomainException("Reversal reason exceeds maximum length of 500.");

            var trimmedNotes = dto.ReversalNotes?.Trim();
            if (!string.IsNullOrEmpty(trimmedNotes) && trimmedNotes.Length > 2000)
                throw new DomainException("Reversal notes exceed maximum length of 2000.");

            var sale = await _db.Sales
                .Include(s => s.Invoices)
                .FirstOrDefaultAsync(s => s.Id == dto.Id && s.CompanyId == companyId, ct)
                ?? throw new DomainException("Sale not found.");

            if (sale.Status == SaleStatus.Reversed)
            {
                await tx.CommitAsync(ct);
                return await GetByIdAsync(sale.Id, companyId, ct);
            }

            if (sale.Status != SaleStatus.Confirmed)
                throw new DomainException("Only confirmed sales can be reversed.");

            var items = await _db.SaleItems
                .Where(i => i.SaleId == sale.Id)
                .ToListAsync(ct);

            var invoices = sale.Invoices ?? new List<Invoice>();

            var hasPaidOrPartiallyPaid = invoices.Any(i =>
                i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Paid);

            var invoiceIds = invoices.Select(i => i.Id).ToList();
            var hasPayments = invoiceIds.Count > 0 && await _db.Payments.AnyAsync(p => p.InvoiceId.HasValue && invoiceIds.Contains(p.InvoiceId.Value), ct);
            var hasReceipts = hasPayments && await _db.Receipts.AnyAsync(r => _db.Payments.Any(p => p.Id == r.PaymentId && p.InvoiceId.HasValue && invoiceIds.Contains(p.InvoiceId.Value)), ct);

            if (hasPaidOrPartiallyPaid || hasPayments || hasReceipts)
            {
                throw new DomainException(
                    "Sale has payments/receipts. Reverse all receipts, reverse all payment allocations, recalculate balance, void invoice, then retry reverse.");
            }

            var livestockItems = items.Where(i => i.LivestockId.HasValue).ToList();
            var conflicts = new List<string>();
            var livestockIdsToCheck = livestockItems.Select(i => i.LivestockId!.Value).ToList();

            var livestockList = await _db.Livestock
                .Where(l => livestockIdsToCheck.Contains(l.Id) && l.CompanyId == companyId)
                .ToListAsync(ct);
            var livestockDict = livestockList.ToDictionary(l => l.Id);

            foreach (var item in livestockItems)
            {
                if (!livestockDict.TryGetValue(item.LivestockId!.Value, out var l))
                {
                    conflicts.Add($"SaleItem {item.Id}: Livestock not found.");
                    continue;
                }

                if (l.Status != LivestockStatus.DischargedSold)
                {
                    conflicts.Add($"Livestock {l.LivestockId} status is {l.Status}, expected DischargedSold.");
                }

                if (l.DischargeCondition != DischargeCondition.Sold)
                {
                    conflicts.Add($"Livestock {l.LivestockId} DischargeCondition is {l.DischargeCondition}, expected Sold.");
                }

                if (l.SoldViaSaleItemId != item.Id)
                {
                    conflicts.Add($"Livestock {l.LivestockId} SoldViaSaleItemId mismatch.");
                }
            }

            if (conflicts.Count > 0)
            {
                throw new DomainException("Cannot reverse sale: " + string.Join(" | ", conflicts));
            }

            var hasUnpaidFinalized = invoices.Any(i => i.Status == InvoiceStatus.Unpaid);

            if (hasUnpaidFinalized)
            {
                foreach (var inv in invoices.Where(i => i.Status == InvoiceStatus.Unpaid))
                {
                    inv.Status = InvoiceStatus.Voided;
                    inv.ModifiedAt = _dateTime.Now;
                    var existingNotes = string.IsNullOrWhiteSpace(inv.Notes) ? "" : inv.Notes + " ";
                    inv.Notes = $"{existingNotes}Voided via sale reversal: {trimmedReason}".Trim();
                }
            }

            foreach (var inv in invoices.Where(i => i.Status == InvoiceStatus.Draft || i.Status == InvoiceStatus.Confirmed))
            {
                inv.Status = InvoiceStatus.Cancelled;
                inv.ModifiedAt = _dateTime.Now;
            }

            foreach (var item in livestockItems)
            {
                var l = livestockDict[item.LivestockId!.Value];

                var alreadyReversed = await _db.LivestockActivities.AnyAsync(a =>
                    a.LivestockId == l.Id &&
                    a.ActivityType == LivestockActivityType.Note &&
                    a.Metadata != null &&
                    a.Metadata.Contains($"SaleReversed;SaleId:" + sale.Id)
                    , ct);

                l.Status = LivestockStatus.Active;
                l.SoldViaSaleItemId = null;
                l.DischargeCondition = null;
                l.DischargeDate = null;
                l.ModifiedAt = _dateTime.Now;

                if (!alreadyReversed)
                {
                    var revActivity = new LivestockActivity(l.Id, LivestockActivityType.Note, dto.ReversalDate,
                        $"Sale reversed: {trimmedReason}")
                    {
                        PerformedByUserId = actingUserId,
                        Metadata = $"SaleReversed;SaleId:{sale.Id};OriginalSaleItemId:{item.Id};Reason:{trimmedReason}"
                    };
                    _db.LivestockActivities.Add(revActivity);
                }
            }

            sale.Status = SaleStatus.Reversed;
            sale.ReversedAt = dto.ReversalDate;
            sale.ReversedByUserId = actingUserId;
            sale.ReversalReason = trimmedReason;
            sale.ReversalNotes = trimmedNotes;
            sale.ModifiedAt = _dateTime.Now;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return await GetByIdAsync(sale.Id, companyId, ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<SaleDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct)
    {
        var sale = await _db.Sales.FirstOrDefaultAsync(s => s.Id == id && s.CompanyId == companyId, ct)
            ?? throw new DomainException("Sale not found.");
        var items = await _db.SaleItems.Where(i => i.SaleId == id).ToListAsync(ct);
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == sale.CustomerId && c.CompanyId == companyId, ct);
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
        sale.Subtotal = Math.Round(items.Sum(i => i.Quantity * i.UnitPrice - i.DiscountAmount), 2, MidpointRounding.AwayFromZero);
        sale.DiscountTotal = Math.Round(items.Sum(i => i.DiscountAmount), 2, MidpointRounding.AwayFromZero);
        sale.TaxTotal = Math.Round(items.Sum(i => i.TaxAmount), 2, MidpointRounding.AwayFromZero);

        var additionalCharges = sale.ChargeTotal;
        sale.GrandTotal = Math.Round(sale.Subtotal + sale.TaxTotal + additionalCharges, 2, MidpointRounding.AwayFromZero);

        ValidateCosts(sale.CommissionAmount, sale.SellerTaxAmount, sale.TransportationAmount,
            sale.OtherCostAmount, sale.OtherCostDescription);

        sale.TotalAdditionalSaleCosts = Math.Round(
            sale.CommissionAmount + sale.SellerTaxAmount + sale.TransportationAmount + sale.OtherCostAmount,
            2, MidpointRounding.AwayFromZero);

        sale.NetSaleProceeds = Math.Round(sale.Subtotal - sale.TotalAdditionalSaleCosts, 2, MidpointRounding.AwayFromZero);
    }

    private static void ValidateCosts(decimal commission, decimal sellerTax, decimal transportation,
        decimal otherCost, string? otherCostDescription)
    {
        if (commission < 0)
            throw new DomainException("Commission amount cannot be negative.");
        if (sellerTax < 0)
            throw new DomainException("Seller tax amount cannot be negative.");
        if (transportation < 0)
            throw new DomainException("Transportation amount cannot be negative.");
        if (otherCost < 0)
            throw new DomainException("Other cost amount cannot be negative.");

        if (otherCost > 0)
        {
            var trimmed = (otherCostDescription ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new DomainException("Other cost description is required when other cost amount is greater than zero.");
            if (trimmed.Length > 500)
                throw new DomainException("Other cost description exceeds maximum length of 500.");
        }
    }

    private static void ApplyCostAllocation(Sale sale, List<SaleItem> items)
    {
        var livestockItems = items.Where(i => i.LivestockId.HasValue).ToList();
        var n = livestockItems.Count;

        if (n == 0)
        {
            foreach (var item in items)
            {
                item.AllocatedCommission = 0;
                item.AllocatedSellerTax = 0;
                item.AllocatedTransportation = 0;
                item.AllocatedOtherCost = 0;
                item.NetSaleProceeds = Math.Round(item.LineTotal, 2, MidpointRounding.AwayFromZero);
            }
            return;
        }

        AllocateCostEqual(livestockItems, sale.CommissionAmount, (i, v) => i.AllocatedCommission = v);
        AllocateCostEqual(livestockItems, sale.SellerTaxAmount, (i, v) => i.AllocatedSellerTax = v);
        AllocateCostEqual(livestockItems, sale.TransportationAmount, (i, v) => i.AllocatedTransportation = v);
        AllocateCostEqual(livestockItems, sale.OtherCostAmount, (i, v) => i.AllocatedOtherCost = v);

        foreach (var ni in items.Where(i => !i.LivestockId.HasValue))
        {
            ni.AllocatedCommission = 0;
            ni.AllocatedSellerTax = 0;
            ni.AllocatedTransportation = 0;
            ni.AllocatedOtherCost = 0;
        }

        foreach (var li in livestockItems)
        {
            li.NetSaleProceeds = Math.Round(
                li.FinalSalePrice - li.AllocatedCommission - li.AllocatedSellerTax
                - li.AllocatedTransportation - li.AllocatedOtherCost,
                2, MidpointRounding.AwayFromZero);
        }
    }

    private static void AllocateCostEqual(List<SaleItem> livestockItems, decimal totalCost, Action<SaleItem, decimal> setter)
    {
        var n = livestockItems.Count;
        if (n == 0 || totalCost == 0)
        {
            foreach (var it in livestockItems) setter(it, 0);
            return;
        }

        var perItem = Math.Round(totalCost / n, 2, MidpointRounding.AwayFromZero);
        var allocatedSoFar = 0m;
        for (var i = 0; i < n - 1; i++)
        {
            setter(livestockItems[i], perItem);
            allocatedSoFar += perItem;
        }
        var remainder = Math.Round(totalCost - allocatedSoFar, 2, MidpointRounding.AwayFromZero);
        setter(livestockItems[n - 1], remainder);
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
        Items = items.Select(MapToItemDto).ToList(),

        CommissionAmount = s.CommissionAmount,
        SellerTaxAmount = s.SellerTaxAmount,
        TransportationAmount = s.TransportationAmount,
        OtherCostAmount = s.OtherCostAmount,
        OtherCostDescription = s.OtherCostDescription,
        TotalAdditionalSaleCosts = s.TotalAdditionalSaleCosts,
        NetSaleProceeds = s.NetSaleProceeds,
        CostAllocationMethod = s.CostAllocationMethod,
        ReversedAt = s.ReversedAt,
        ReversedByUserId = s.ReversedByUserId,
        ReversalReason = s.ReversalReason,
        ReversalNotes = s.ReversalNotes
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
        LineTotal = i.LineTotal,

        SuggestedPrice = i.SuggestedPrice,
        SuggestedPriceMethod = i.SuggestedPriceMethod,
        SuggestedWeight = i.SuggestedWeight,
        SuggestedWeightDate = i.SuggestedWeightDate,
        SuggestedRate = i.SuggestedRate,
        FinalSalePrice = i.FinalSalePrice,
        PriceSource = i.PriceSource,
        AllocatedCommission = i.AllocatedCommission,
        AllocatedSellerTax = i.AllocatedSellerTax,
        AllocatedTransportation = i.AllocatedTransportation,
        AllocatedOtherCost = i.AllocatedOtherCost,
        NetSaleProceeds = i.NetSaleProceeds
    };
}
