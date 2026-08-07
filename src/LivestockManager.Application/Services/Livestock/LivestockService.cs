using System.Text;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Livestock;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LivestockManager.Application.Services.Livestock;

public class LivestockService : ILivestockService
{
    private readonly IAppDbContext _db;
    private readonly IDateTime _dateTime;
    private readonly ISequenceGenerator _sequenceGenerator;

    public LivestockService(
        IAppDbContext db,
        IDateTime dateTime,
        ISequenceGenerator sequenceGenerator)
    {
        _db = db;
        _dateTime = dateTime;
        _sequenceGenerator = sequenceGenerator;
    }

    public async Task<LivestockDetailDto> RegisterAsync(LivestockRegisterDto dto, CancellationToken ct)
    {
        if (dto.LivestockType == LivestockType.Ad || dto.LivestockType == LivestockType.Sd)
        {
            if (dto.PurchaseAmount != 0)
                throw new DomainException("Bred livestock (Ad/Sd) must have PurchaseAmount = 0.");
        }
        else if (dto.LivestockType == LivestockType.Ah || dto.LivestockType == LivestockType.Su || dto.LivestockType == LivestockType.Sa)
        {
            if (dto.PurchaseAmount <= 0)
                throw new DomainException("Purchased livestock (Ah/Su/Sa) must have PurchaseAmount > 0.");
        }

        var defaultCompany = await _db.Companies.FirstAsync(ct);
        var companyId = dto.CompanyId == Guid.Empty ? defaultCompany.Id : dto.CompanyId;

        var livestockId = await _sequenceGenerator.GenerateLivestockIdAsync(companyId, dto.LivestockType);

        var livestock = new LivestockManager.Domain.Entities.Livestock(
            companyId,
            livestockId,
            dto.LivestockType,
            dto.AcquisitionDate,
            dto.InitialWeight,
            dto.WeightUnit,
            dto.PurchaseAmount,
            dto.FarmId)
        {
            CurrentWeight = dto.InitialWeight,
            CurrentWeightDate = dto.AcquisitionDate,
            Comments = dto.Comments
        };

        _db.Livestock.Add(livestock);

        var initialWeight = new LivestockWeight(livestock.Id, dto.InitialWeight, dto.WeightUnit, dto.AcquisitionDate);
        _db.LivestockWeights.Add(initialWeight);

        var initialActivity = new LivestockActivity(livestock.Id, LivestockActivityType.Created, dto.AcquisitionDate, "Livestock registered")
        {
            Metadata = $"PurchaseAmount:{dto.PurchaseAmount};InitialWeight:{dto.InitialWeight}"
        };
        _db.LivestockActivities.Add(initialActivity);

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(livestock.Id, ct);
    }

    public async Task<LivestockDetailDto> UpdateAsync(Guid livestockId, LivestockEditDto dto, CancellationToken ct)
    {
        var livestock = await _db.Livestock.FirstOrDefaultAsync(l => l.Id == livestockId, ct)
            ?? throw new DomainException($"Livestock {livestockId} not found.");

        livestock.FarmId = dto.FarmId;
        livestock.AcquisitionDate = dto.AcquisitionDate;
        livestock.InitialWeight = dto.InitialWeight;
        livestock.WeightUnit = dto.WeightUnit;
        livestock.Comments = dto.Comments;
        livestock.ModifiedAt = _dateTime.Now;

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(livestockId, ct);
    }

    public async Task AddWeightAsync(Guid livestockId, LivestockWeightAddDto dto, CancellationToken ct)
    {
        var livestock = await _db.Livestock.FirstOrDefaultAsync(l => l.Id == livestockId, ct)
            ?? throw new DomainException($"Livestock {livestockId} not found.");

        if (livestock.Status != LivestockStatus.Active)
            throw new InvalidDischargeException("Cannot add weight to discharged livestock.");

        var weightRecord = new LivestockWeight(livestockId, dto.Weight, dto.Unit, dto.WeighedAt)
        {
            Notes = dto.Notes
        };
        _db.LivestockWeights.Add(weightRecord);

        livestock.CurrentWeight = dto.Weight;
        livestock.CurrentWeightDate = dto.WeighedAt;
        livestock.ModifiedAt = _dateTime.Now;

        var activity = new LivestockActivity(livestockId, LivestockActivityType.WeightAdded, dto.WeighedAt, $"Weight recorded: {dto.Weight} {dto.Unit}")
        {
            Metadata = $"Weight:{dto.Weight};Unit:{dto.Unit}"
        };
        _db.LivestockActivities.Add(activity);

        await _db.SaveChangesAsync(ct);
    }

    public async Task<LivestockDetailDto> DischargeAsync(Guid livestockId, LivestockDischargeDto dto, CancellationToken ct)
    {
        var livestock = await _db.Livestock.FirstOrDefaultAsync(l => l.Id == livestockId, ct)
            ?? throw new DomainException($"Livestock {livestockId} not found.");

        if (livestock.Status != LivestockStatus.Active)
            throw new LivestockAlreadySoldException("Livestock has already been discharged.");

        livestock.Status = dto.Condition switch
        {
            DischargeCondition.Sold => LivestockStatus.DischargedSold,
            DischargeCondition.Deceased => LivestockStatus.DischargedDeceased,
            DischargeCondition.Lost => LivestockStatus.DischargedLost,
            DischargeCondition.Stolen => LivestockStatus.DischargedStolen,
            _ => LivestockStatus.DischargedOther
        };

        livestock.DischargeDate = dto.Date;
        livestock.DischargeCondition = dto.Condition;
        livestock.DischargeDetails = dto.Details;

        if (dto.Condition == DischargeCondition.Sold && dto.SoldAmount.HasValue)
        {
            livestock.SoldAmount = dto.SoldAmount.Value;
        }

        livestock.ModifiedAt = _dateTime.Now;

        var activity = new LivestockActivity(livestockId, LivestockActivityType.Discharge, dto.Date,
            $"Discharged: {dto.Condition}" + (dto.SoldAmount.HasValue ? $" - Sold for {dto.SoldAmount.Value}" : ""))
        {
            Metadata = $"Condition:{dto.Condition};SoldAmount:{dto.SoldAmount};Details:{dto.Details}"
        };
        _db.LivestockActivities.Add(activity);

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(livestockId, ct);
    }

    public async Task<decimal> CalculateCompleteProfitLossAsync(Guid livestockId, CancellationToken ct)
    {
        var livestock = await _db.Livestock.FirstOrDefaultAsync(l => l.Id == livestockId, ct)
            ?? throw new DomainException($"Livestock {livestockId} not found.");

        if (!livestock.BasicProfitLoss.HasValue)
            return 0m;

        return livestock.BasicProfitLoss.Value;
    }

    public async Task<IList<LivestockSummaryDto>> SearchAsync(string farmId, string type, string status, string keyword, CancellationToken ct)
    {
        var query = _db.Livestock.AsQueryable();

        if (!string.IsNullOrWhiteSpace(farmId) && Guid.TryParse(farmId, out var farmGuid))
        {
            query = query.Where(l => l.FarmId == farmGuid);
        }

        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<LivestockType>(type, out var typeEnum))
        {
            query = query.Where(l => l.LivestockTypeId == typeEnum);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LivestockStatus>(status, out var statusEnum))
        {
            query = query.Where(l => l.Status == statusEnum);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(l =>
                l.LivestockId.Contains(keyword) ||
                (l.Comments != null && l.Comments.Contains(keyword)));
        }

        return await query.AsQueryable().Select(MapToSummary).AsQueryable().ToListAsync(ct);
    }

    public async Task<byte[]> ExportCsvAsync(Guid companyId, CancellationToken ct)
    {
        var livestockList = await _db.Livestock.Where(l => l.CompanyId == companyId).ToListAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("Id,LivestockId,Type,Status,AcquisitionDate,InitialWeight,CurrentWeight,PurchaseAmount,SoldAmount,BasicProfitLoss");

        foreach (var l in livestockList)
        {
            sb.AppendLine($"{l.Id},{l.LivestockId},{l.LivestockTypeId},{l.Status},{l.AcquisitionDate:yyyy-MM-dd},{l.InitialWeight},{l.CurrentWeight},{l.PurchaseAmount},{l.SoldAmount},{l.BasicProfitLoss}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<IList<LivestockWeightHistoryDto>> GetWeightHistoryAsync(Guid livestockId, CancellationToken ct)
    {
        return await _db.LivestockWeights
            .Where(w => w.LivestockId == livestockId)
            .OrderBy(w => w.WeighedAt)
            .Select(w => new LivestockWeightHistoryDto
            {
                Id = w.Id,
                LivestockId = w.LivestockId,
                Weight = w.Weight,
                Unit = w.Unit,
                WeighedAt = w.WeighedAt,
                Notes = w.Notes
            })
            .ToListAsync(ct);
    }

    public async Task AddActivityAsync(Guid livestockId, LivestockActivityDto dto, CancellationToken ct)
    {
        var livestock = await _db.Livestock.FirstOrDefaultAsync(l => l.Id == livestockId, ct)
            ?? throw new DomainException($"Livestock {livestockId} not found.");

        var activity = new LivestockActivity(livestockId, dto.ActivityType, dto.PerformedAt, dto.Description)
        {
            Metadata = dto.Metadata
        };
        _db.LivestockActivities.Add(activity);

        livestock.ModifiedAt = _dateTime.Now;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<LivestockDetailDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var livestock = await _db.Livestock.FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new DomainException($"Livestock {id} not found.");

        var farm = livestock.FarmId.HasValue ? await _db.Farms.FirstOrDefaultAsync(f => f.Id == livestock.FarmId, ct) : null;
        var weights = await _db.LivestockWeights.Where(w => w.LivestockId == id).OrderBy(w => w.WeighedAt).ToListAsync(ct);
        var activities = await _db.LivestockActivities.Where(a => a.LivestockId == id).OrderByDescending(a => a.PerformedAt).ToListAsync(ct);

        var completeProfitLoss = livestock.BasicProfitLoss.HasValue
            ? await CalculateCompleteProfitLossAsync(id, ct)
            : null as decimal?;

        return new LivestockDetailDto
        {
            Id = livestock.Id,
            CompanyId = livestock.CompanyId,
            FarmId = livestock.FarmId,
            FarmName = farm?.Name,
            LivestockId = livestock.LivestockId,
            LivestockTypeId = livestock.LivestockTypeId,
            AcquisitionDate = livestock.AcquisitionDate,
            InitialWeight = livestock.InitialWeight,
            WeightUnit = livestock.WeightUnit,
            PurchaseAmount = livestock.PurchaseAmount,
            CurrentWeight = livestock.CurrentWeight,
            CurrentWeightDate = livestock.CurrentWeightDate,
            Status = livestock.Status,
            Comments = livestock.Comments,
            DischargeDate = livestock.DischargeDate,
            DischargeCondition = livestock.DischargeCondition,
            DischargeDetails = livestock.DischargeDetails,
            SoldAmount = livestock.SoldAmount,
            BasicProfitLoss = livestock.BasicProfitLoss,
            CompleteProfitLoss = completeProfitLoss,
            CreatedAt = livestock.CreatedAt,
            ModifiedAt = livestock.ModifiedAt,
            WeightHistory = weights.Select(w => new LivestockWeightHistoryDto
            {
                Id = w.Id,
                LivestockId = w.LivestockId,
                Weight = w.Weight,
                Unit = w.Unit,
                WeighedAt = w.WeighedAt,
                Notes = w.Notes
            }).ToList(),
            Activities = activities.Select(a => new LivestockActivityDto
            {
                Id = a.Id,
                LivestockId = a.LivestockId,
                ActivityType = a.ActivityType,
                Description = a.Description,
                PerformedAt = a.PerformedAt,
                Metadata = a.Metadata
            }).ToList()
        };
    }

    private static LivestockSummaryDto MapToSummary(Domain.Entities.Livestock l) => new()
    {
        Id = l.Id,
        CompanyId = l.CompanyId,
        FarmId = l.FarmId,
        LivestockId = l.LivestockId,
        LivestockTypeId = l.LivestockTypeId,
        AcquisitionDate = l.AcquisitionDate,
        InitialWeight = l.InitialWeight,
        WeightUnit = l.WeightUnit,
        PurchaseAmount = l.PurchaseAmount,
        CurrentWeight = l.CurrentWeight,
        CurrentWeightDate = l.CurrentWeightDate,
        Status = l.Status
    };
}
