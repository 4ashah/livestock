using System.Text;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.StockAddition;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Helpers;
using Microsoft.EntityFrameworkCore;
using DE = LivestockManager.Domain.Entities;

namespace LivestockManager.Application.Services.StockAddition;

public class StockAdditionService : IStockAdditionService
{
    private readonly IAppDbContext _db;
    private readonly IDateTime _dateTime;
    private readonly ISequenceGenerator _sequenceGenerator;
    private readonly IStockAdditionIdempotencyCache _idempotencyCache;

    public StockAdditionService(
        IAppDbContext db,
        IDateTime dateTime,
        ISequenceGenerator sequenceGenerator,
        IStockAdditionIdempotencyCache idempotencyCache)
    {
        _db = db;
        _dateTime = dateTime;
        _sequenceGenerator = sequenceGenerator;
        _idempotencyCache = idempotencyCache;
    }

    public async Task<StockAdditionResultDto> AddPurchasedLivestockAsync(
        StockAdditionPurchasedDto dto,
        Guid companyId,
        Guid userId,
        CancellationToken ct)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("CompanyId cannot be empty.", nameof(companyId));

        var cached = _idempotencyCache.TryGet(companyId, dto.IdempotencyKey);
        if (cached != null)
        {
            cached.IsDuplicate = true;
            return cached;
        }

        using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            var supplier = await _db.Suppliers
                .FirstOrDefaultAsync(s => s.Id == dto.SupplierId && s.CompanyId == companyId, ct);
            if (supplier == null)
                return Fail("Supplier not found.");
            if (!supplier.IsActive)
                return Fail("Supplier is not active.");

            var farm = await _db.Farms
                .FirstOrDefaultAsync(f => f.Id == dto.FarmId && f.CompanyId == companyId, ct);
            if (farm == null)
                return Fail("Farm not found.");
            if (!farm.IsActive)
                return Fail("Farm is not active.");

            var purchasedTypes = new[] { LivestockType.Ah, LivestockType.Su, LivestockType.Sa };
            if (!purchasedTypes.Contains(dto.LivestockTypeId))
                return Fail($"Invalid livestock type for purchased addition. Only Ah, Su, or Sa are allowed.");

            if (dto.PurchaseWeight <= 0)
                return Fail("Purchase weight must be greater than zero.");

            if (dto.PurchaseCost < 0)
                return Fail("Purchase cost cannot be negative.");
            if (dto.PurchaseCost == 0)
                return Fail("Purchase cost must be greater than zero.");

            if (dto.CommissionAmount < 0)
                return Fail("Commission cannot be negative.");
            if (dto.TaxAmount < 0)
                return Fail("Taxes cannot be negative.");
            if (dto.TransportationAmount < 0)
                return Fail("Transportation cannot be negative.");
            if (dto.OtherCostAmount < 0)
                return Fail("Other costs cannot be negative.");

            var otherCostDescription = string.IsNullOrWhiteSpace(dto.OtherCostDescription)
                ? null
                : dto.OtherCostDescription.Trim();
            if (dto.OtherCostAmount > 0 && string.IsNullOrWhiteSpace(otherCostDescription))
                return Fail("Other cost description is required when other costs are greater than zero.");
            if (otherCostDescription != null && otherCostDescription.Length > 500)
                return Fail("Other cost description cannot exceed 500 characters.");

            var maxAmount = 999999999999.99m;
            if (dto.PurchaseCost > maxAmount)
                return Fail("Purchase cost exceeds the allowed financial limit.");
            if (dto.CommissionAmount > maxAmount)
                return Fail("Commission exceeds the allowed financial limit.");
            if (dto.TaxAmount > maxAmount)
                return Fail("Taxes exceed the allowed financial limit.");
            if (dto.TransportationAmount > maxAmount)
                return Fail("Transportation exceeds the allowed financial limit.");
            if (dto.OtherCostAmount > maxAmount)
                return Fail("Other costs exceed the allowed financial limit.");

            if (!Enum.IsDefined(typeof(CostAllocationMethod), dto.CostAllocationMethod))
                return Fail("Invalid cost allocation method.");

            var commission = Math.Round(dto.CommissionAmount, 2, MidpointRounding.AwayFromZero);
            var tax = Math.Round(dto.TaxAmount, 2, MidpointRounding.AwayFromZero);
            var transport = Math.Round(dto.TransportationAmount, 2, MidpointRounding.AwayFromZero);
            var other = Math.Round(dto.OtherCostAmount, 2, MidpointRounding.AwayFromZero);
            var purchaseCost = Math.Round(dto.PurchaseCost, 2, MidpointRounding.AwayFromZero);

            var additionalAcquisitionCost = Math.Round(commission + tax + transport + other, 2, MidpointRounding.AwayFromZero);
            var totalAcquisitionCost = Math.Round(purchaseCost + additionalAcquisitionCost, 2, MidpointRounding.AwayFromZero);

            var allocation = AllocateSingleAnimalAcquisitionCosts(
                purchaseCost,
                commission,
                tax,
                transport,
                other,
                dto.CostAllocationMethod);

            var year = dto.PurchaseDate.Year;
            var scopedKey = $"PUR:{year}";
            var rawNumber = await _sequenceGenerator.GenerateDocumentNumberAsync(companyId, scopedKey);
            var numericSuffix = ExtractNumeric(rawNumber, scopedKey);
            var purchaseNumber = $"PUR-{year}-{numericSuffix:D5}";

            var purchase = new Purchase(companyId, dto.FarmId, dto.SupplierId, dto.PurchaseDate)
            {
                PurchaseNumber = purchaseNumber,
                SupplierReference = dto.SupplierReference,
                Status = PurchaseStatus.Posted,
                Currency = Currency.USD,
                DiscountPct = 0,
                TaxRate = 0,
                Subtotal = purchaseCost,
                TaxTotal = tax,
                GrandTotal = totalAcquisitionCost,
                AmountPaid = 0,
                OutstandingAmount = totalAcquisitionCost,
                CostAllocationMethod = dto.CostAllocationMethod,
                TotalLivestockPurchaseCost = purchaseCost,
                TotalCommission = commission,
                TotalTax = tax,
                TotalTransportation = transport,
                TotalOtherCost = other,
                OtherCostDescription = otherCostDescription,
                AdditionalAcquisitionCost = additionalAcquisitionCost,
                TotalAcquisitionCost = totalAcquisitionCost,
                Notes = dto.Comments,
                DocumentId = dto.DocumentId
            };
            _db.Purchases.Add(purchase);
            await _db.SaveChangesAsync(ct);

            var livestockIdSeq = await _sequenceGenerator.GenerateLivestockIdAsync(companyId, dto.LivestockTypeId);

            var livestock = new DE.Livestock(
                companyId,
                livestockIdSeq,
                dto.LivestockTypeId,
                dto.PurchaseDate,
                dto.PurchaseWeight,
                dto.WeightUnit,
                purchaseCost,
                dto.FarmId)
            {
                StockSource = StockSource.Purchased,
                AllocatedCommission = allocation.AllocatedCommission,
                AllocatedTax = allocation.AllocatedTax,
                AllocatedTransportation = allocation.AllocatedTransportation,
                AllocatedOtherCost = allocation.AllocatedOtherCost,
                OtherCostDescription = otherCostDescription,
                TotalAcquisitionCost = allocation.TotalAcquisitionCost,
                CurrentWeight = dto.PurchaseWeight,
                CurrentWeightDate = dto.PurchaseDate,
                Comments = dto.Comments
            };
            _db.Livestock.Add(livestock);
            await _db.SaveChangesAsync(ct);

            var purchaseItem = new PurchaseItem(purchase.Id, 1, PurchaseItemType.Livestock, 1, purchaseCost)
            {
                Description = $"{dto.LivestockTypeId} - {livestockIdSeq}",
                UnitWeight = dto.PurchaseWeight,
                WeightUnit = dto.WeightUnit,
                DiscountPct = 0,
                TaxRate = 0,
                LineTotal = totalAcquisitionCost,
                LivestockPurchaseCost = purchaseCost,
                CommissionAmount = allocation.AllocatedCommission,
                TaxAmount = allocation.AllocatedTax,
                TransportationAmount = allocation.AllocatedTransportation,
                OtherCostAmount = allocation.AllocatedOtherCost,
                OtherCostDescription = otherCostDescription,
                AdditionalAcquisitionCost = allocation.AdditionalAcquisitionCost,
                TotalAcquisitionCost = allocation.TotalAcquisitionCost,
                CostAllocationMethod = dto.CostAllocationMethod,
                LivestockId = livestock.Id
            };
            _db.PurchaseItems.Add(purchaseItem);

            var initialWeight = new LivestockWeight(livestock.Id, dto.PurchaseWeight, dto.WeightUnit, dto.PurchaseDate)
            {
                Notes = "Initial weight from purchase"
            };
            _db.LivestockWeights.Add(initialWeight);

            var metadata = BuildPurchasedMetadata(
                dto,
                purchaseNumber,
                livestockIdSeq,
                userId,
                commission,
                tax,
                transport,
                other,
                otherCostDescription,
                additionalAcquisitionCost,
                totalAcquisitionCost,
                allocation,
                dto.CostAllocationMethod);
            var activity = new LivestockActivity(livestock.Id, LivestockActivityType.Created, dto.PurchaseDate,
                "Livestock added via purchased stock addition with acquisition cost breakdown")
            {
                PerformedByUserId = userId != Guid.Empty ? userId : null,
                Metadata = metadata
            };
            _db.LivestockActivities.Add(activity);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var result = new StockAdditionResultDto
            {
                Succeeded = true,
                LivestockEntityId = livestock.Id,
                LivestockId = livestock.LivestockId,
                StockSource = StockSource.Purchased,
                FarmName = farm.Name,
                SupplierName = supplier.Name,
                PurchaseDate = dto.PurchaseDate,
                StartingWeight = dto.PurchaseWeight,
                WeightUnit = dto.WeightUnit,
                PurchaseCost = purchaseCost,
                CommissionAmount = commission,
                TaxAmount = tax,
                TransportationAmount = transport,
                OtherCostAmount = other,
                OtherCostDescription = otherCostDescription,
                AdditionalAcquisitionCost = additionalAcquisitionCost,
                TotalAcquisitionCost = totalAcquisitionCost,
                PurchaseNumber = purchaseNumber,
                IsDuplicate = false
            };

            _idempotencyCache.Set(companyId, dto.IdempotencyKey, result);
            return result;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return Fail($"Unexpected error: {SafeMessage(ex)}");
        }
    }

    public async Task<StockAdditionResultDto> AddNewbornLivestockAsync(
        StockAdditionNewbornDto dto,
        Guid companyId,
        Guid userId,
        CancellationToken ct)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("CompanyId cannot be empty.", nameof(companyId));

        var cached = _idempotencyCache.TryGet(companyId, dto.IdempotencyKey);
        if (cached != null)
        {
            cached.IsDuplicate = true;
            return cached;
        }

        using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            var now = _dateTime.Now;
            var maxFuture = now.AddDays(1);
            var minPast = now.AddYears(-20);
            if (dto.DateOfBirth > maxFuture)
                return Fail("Date of birth cannot be more than 1 day in the future.");
            if (dto.DateOfBirth < minPast)
                return Fail("Date of birth cannot be more than 20 years in the past.");

            var farm = await _db.Farms
                .FirstOrDefaultAsync(f => f.Id == dto.FarmId && f.CompanyId == companyId, ct);
            if (farm == null)
                return Fail("Farm not found.");
            if (!farm.IsActive)
                return Fail("Farm is not active.");

            if (dto.LivestockTypeId == LivestockType.Su)
                return Fail("An approved livestock ID code is required for a newborn uncastrated male.");
            if (dto.LivestockTypeId == LivestockType.Ah)
                return Fail("Newborn livestock cannot be a purchased castrated ram type (Ah). Use Ad for bred castrated ram or Sd for bred ewe.");
            if (dto.LivestockTypeId == LivestockType.Sa)
                return Fail("Newborn livestock cannot be a purchased ewe type (Sa). Use Sd for bred ewe.");

            var allowedNewbornTypes = new[] { LivestockType.Ad, LivestockType.Sd };
            if (!allowedNewbornTypes.Contains(dto.LivestockTypeId))
                return Fail($"Invalid livestock type for newborn addition. Only Ad or Sd are allowed.");

            if (dto.BirthWeight <= 0)
                return Fail("Birth weight must be greater than zero.");

            if (dto.MotherLivestockId == dto.FatherLivestockId)
                return Fail("Mother and father livestock must be different.");

            var mother = await _db.Livestock
                .FirstOrDefaultAsync(l => l.Id == dto.MotherLivestockId && l.CompanyId == companyId, ct);
            if (mother == null)
                return Fail("Mother livestock not found.");
            if (mother.Status != LivestockStatus.Active)
                return Fail("Mother livestock is not active.");
            var eweTypes = new[] { LivestockType.Sa, LivestockType.Sd };
            if (!eweTypes.Contains(mother.LivestockTypeId))
                return Fail("Mother livestock must be a ewe type (Sa or Sd).");

            var father = await _db.Livestock
                .FirstOrDefaultAsync(l => l.Id == dto.FatherLivestockId && l.CompanyId == companyId, ct);
            if (father == null)
                return Fail("Father livestock not found.");
            if (father.Status != LivestockStatus.Active)
                return Fail("Father livestock is not active.");
            var ramTypes = new[] { LivestockType.Su, LivestockType.Ah, LivestockType.Ad };
            if (!ramTypes.Contains(father.LivestockTypeId))
                return Fail("Father livestock must be a ram type (Su, Ah, or Ad).");

            var livestockIdSeq = await _sequenceGenerator.GenerateLivestockIdAsync(companyId, dto.LivestockTypeId);

            var livestock = new DE.Livestock(
                companyId,
                livestockIdSeq,
                dto.LivestockTypeId,
                dto.DateOfBirth,
                dto.BirthWeight,
                dto.WeightUnit,
                0.00m,
                dto.FarmId)
            {
                StockSource = StockSource.Newborn,
                DateOfBirth = dto.DateOfBirth,
                MotherLivestockId = dto.MotherLivestockId,
                FatherLivestockId = dto.FatherLivestockId,
                CurrentWeight = dto.BirthWeight,
                CurrentWeightDate = dto.DateOfBirth,
                Comments = dto.BirthComments
            };
            _db.Livestock.Add(livestock);
            await _db.SaveChangesAsync(ct);

            var initialWeight = new LivestockWeight(livestock.Id, dto.BirthWeight, dto.WeightUnit, dto.DateOfBirth)
            {
                Notes = "Birth weight"
            };
            _db.LivestockWeights.Add(initialWeight);

            var metadata = BuildNewbornMetadata(dto, livestockIdSeq, mother.LivestockId, father.LivestockId, userId);
            var activity = new LivestockActivity(livestock.Id, LivestockActivityType.Created, dto.DateOfBirth,
                "Livestock added via newborn stock addition")
            {
                PerformedByUserId = userId != Guid.Empty ? userId : null,
                Metadata = metadata
            };
            _db.LivestockActivities.Add(activity);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var result = new StockAdditionResultDto
            {
                Succeeded = true,
                LivestockEntityId = livestock.Id,
                LivestockId = livestock.LivestockId,
                StockSource = StockSource.Newborn,
                FarmName = farm.Name,
                DateOfBirth = dto.DateOfBirth,
                StartingWeight = dto.BirthWeight,
                WeightUnit = dto.WeightUnit,
                PurchaseCost = 0.00m,
                MotherLivestockId = mother.LivestockId,
                FatherLivestockId = father.LivestockId,
                IsDuplicate = false
            };

            _idempotencyCache.Set(companyId, dto.IdempotencyKey, result);
            return result;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return Fail($"Unexpected error: {SafeMessage(ex)}");
        }
    }

    public async Task<IList<ParentSearchItemDto>> SearchEligibleEwesAsync(
        string keyword,
        Guid companyId,
        int limit = 25,
        CancellationToken ct = default)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("CompanyId cannot be empty.", nameof(companyId));

        var eweTypes = new[] { LivestockType.Sa, LivestockType.Sd };

        var query = _db.Livestock
            .Where(l => l.CompanyId == companyId
                && l.Status == LivestockStatus.Active
                && eweTypes.Contains(l.LivestockTypeId));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(l =>
                l.LivestockId.Contains(keyword) ||
                (l.Comments != null && l.Comments.Contains(keyword)));
        }

        var results = await query
            .Take(limit)
            .Select(l => new { l.Id, l.LivestockId, l.LivestockTypeId, l.FarmId, l.Status })
            .ToListAsync(ct);

        var farmIds = results.Select(r => r.FarmId).Where(f => f.HasValue).Distinct().ToList();
        var farms = await _db.Farms
            .Where(f => farmIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.Name, ct);

        return results.Select(r => new ParentSearchItemDto
        {
            Id = r.Id,
            LivestockId = r.LivestockId,
            Type = r.LivestockTypeId,
            TypeDescription = GetTypeDescription(r.LivestockTypeId),
            FarmId = r.FarmId,
            FarmName = r.FarmId.HasValue && farms.TryGetValue(r.FarmId.Value, out var fn) ? fn : null,
            Status = r.Status
        }).ToList();
    }

    public async Task<IList<ParentSearchItemDto>> SearchEligibleRamsAsync(
        string keyword,
        Guid companyId,
        int limit = 25,
        CancellationToken ct = default)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("CompanyId cannot be empty.", nameof(companyId));

        var ramTypes = new[] { LivestockType.Su, LivestockType.Ah, LivestockType.Ad };

        var query = _db.Livestock
            .Where(l => l.CompanyId == companyId
                && l.Status == LivestockStatus.Active
                && ramTypes.Contains(l.LivestockTypeId));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(l =>
                l.LivestockId.Contains(keyword) ||
                (l.Comments != null && l.Comments.Contains(keyword)));
        }

        var results = await query
            .Take(limit)
            .Select(l => new { l.Id, l.LivestockId, l.LivestockTypeId, l.FarmId, l.Status })
            .ToListAsync(ct);

        var farmIds = results.Select(r => r.FarmId).Where(f => f.HasValue).Distinct().ToList();
        var farms = await _db.Farms
            .Where(f => farmIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.Name, ct);

        return results.Select(r => new ParentSearchItemDto
        {
            Id = r.Id,
            LivestockId = r.LivestockId,
            Type = r.LivestockTypeId,
            TypeDescription = GetTypeDescription(r.LivestockTypeId),
            FarmId = r.FarmId,
            FarmName = r.FarmId.HasValue && farms.TryGetValue(r.FarmId.Value, out var fn) ? fn : null,
            Status = r.Status
        }).ToList();
    }

    private static StockAdditionResultDto Fail(string errorMessage) => new()
    {
        Succeeded = false,
        ErrorMessage = errorMessage,
        IsDuplicate = false
    };

    private sealed class AllocatedCosts
    {
        public decimal AllocatedCommission { get; set; }
        public decimal AllocatedTax { get; set; }
        public decimal AllocatedTransportation { get; set; }
        public decimal AllocatedOtherCost { get; set; }
        public decimal AdditionalAcquisitionCost { get; set; }
        public decimal TotalAcquisitionCost { get; set; }
    }

    private static AllocatedCosts AllocateSingleAnimalAcquisitionCosts(
        decimal purchaseCost,
        decimal commission,
        decimal tax,
        decimal transport,
        decimal other,
        CostAllocationMethod method)
    {
        var additional = Math.Round(commission + tax + transport + other, 2, MidpointRounding.AwayFromZero);
        var total = Math.Round(purchaseCost + additional, 2, MidpointRounding.AwayFromZero);
        return new AllocatedCosts
        {
            AllocatedCommission = Math.Round(commission, 2, MidpointRounding.AwayFromZero),
            AllocatedTax = Math.Round(tax, 2, MidpointRounding.AwayFromZero),
            AllocatedTransportation = Math.Round(transport, 2, MidpointRounding.AwayFromZero),
            AllocatedOtherCost = Math.Round(other, 2, MidpointRounding.AwayFromZero),
            AdditionalAcquisitionCost = additional,
            TotalAcquisitionCost = total
        };
    }

    private static string SafeMessage(Exception ex)
    {
        if (ex is ArgumentException || ex is InvalidOperationException)
            return ex.Message;
        return "An unexpected error occurred. Please try again.";
    }

    private static string BuildPurchasedMetadata(
        StockAdditionPurchasedDto dto,
        string purchaseNumber,
        string livestockId,
        Guid userId,
        decimal commission,
        decimal tax,
        decimal transport,
        decimal other,
        string? otherCostDescription,
        decimal additionalAcquisitionCost,
        decimal totalAcquisitionCost,
        AllocatedCosts allocation,
        CostAllocationMethod method)
    {
        var sb = new StringBuilder();
        sb.Append($"Idempotency:{dto.IdempotencyKey:N};");
        sb.Append($"Source:Purchased;");
        sb.Append($"PurchaseNumber:{purchaseNumber};");
        sb.Append($"LivestockId:{livestockId};");
        sb.Append($"SupplierId:{dto.SupplierId:N};");
        sb.Append($"FarmId:{dto.FarmId:N};");
        sb.Append($"Type:{dto.LivestockTypeId};");
        sb.Append($"PurchaseWeight:{dto.PurchaseWeight};");
        sb.Append($"WeightUnit:{dto.WeightUnit};");
        sb.Append($"PurchaseCost:{dto.PurchaseCost};");
        sb.Append($"Commission:{commission};");
        sb.Append($"Tax:{tax};");
        sb.Append($"Transportation:{transport};");
        sb.Append($"OtherCost:{other};");
        if (!string.IsNullOrWhiteSpace(otherCostDescription))
            sb.Append($"OtherCostDesc:{otherCostDescription};");
        sb.Append($"AdditionalAcqCost:{additionalAcquisitionCost};");
        sb.Append($"TotalAcqCost:{totalAcquisitionCost};");
        sb.Append($"AllocationMethod:{method};");
        sb.Append($"AllocCommission:{allocation.AllocatedCommission};");
        sb.Append($"AllocTax:{allocation.AllocatedTax};");
        sb.Append($"AllocTransport:{allocation.AllocatedTransportation};");
        sb.Append($"AllocOther:{allocation.AllocatedOtherCost};");
        if (!string.IsNullOrWhiteSpace(dto.SupplierReference))
            sb.Append($"SupplierRef:{dto.SupplierReference};");
        if (!string.IsNullOrWhiteSpace(dto.Comments))
            sb.Append($"Comments:{dto.Comments};");
        if (dto.DocumentId.HasValue)
            sb.Append($"DocumentId:{dto.DocumentId.Value:N};");
        if (userId != Guid.Empty)
            sb.Append($"UserId:{userId:N};");
        return sb.ToString();
    }

    private static string BuildNewbornMetadata(
        StockAdditionNewbornDto dto,
        string livestockId,
        string motherDisplayId,
        string fatherDisplayId,
        Guid userId)
    {
        var sb = new StringBuilder();
        sb.Append($"Idempotency:{dto.IdempotencyKey:N};");
        sb.Append($"Source:Newborn;");
        sb.Append($"LivestockId:{livestockId};");
        sb.Append($"MotherId:{dto.MotherLivestockId:N}({motherDisplayId});");
        sb.Append($"FatherId:{dto.FatherLivestockId:N}({fatherDisplayId});");
        sb.Append($"FarmId:{dto.FarmId:N};");
        sb.Append($"Type:{dto.LivestockTypeId};");
        sb.Append($"BirthWeight:{dto.BirthWeight};");
        sb.Append($"WeightUnit:{dto.WeightUnit};");
        sb.Append($"DateOfBirth:{dto.DateOfBirth:O};");
        if (!string.IsNullOrWhiteSpace(dto.BirthComments))
            sb.Append($"BirthComments:{dto.BirthComments};");
        if (userId != Guid.Empty)
            sb.Append($"UserId:{userId:N};");
        return sb.ToString();
    }

    private static string GetTypeDescription(LivestockType type)
        => LivestockTypeDisplay.GetDisplayName(type);

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
}
