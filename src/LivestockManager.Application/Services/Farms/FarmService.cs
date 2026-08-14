using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Farms;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LivestockManager.Application.Services.Farms;

public class FarmService : IFarmService
{
    private readonly IAppDbContext _db;
    private readonly IDateTime _dateTime;

    public FarmService(
        IAppDbContext db,
        IDateTime dateTime)
    {
        _db = db;
        _dateTime = dateTime;
    }

    public async Task<FarmDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct)
    {
        var entity = await _db.Farms
            .FirstOrDefaultAsync(f => f.Id == id && f.CompanyId == companyId && !f.IsDeleted, ct)
            ?? throw new DomainException("Farm not found.");

        var livestockCounts = await _db.Livestock
            .Where(l => l.FarmId == id && l.CompanyId == companyId && !l.IsDeleted)
            .GroupBy(l => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(l => l.Status == LivestockStatus.Active)
            })
            .FirstOrDefaultAsync(ct);

        var detail = MapToDetail(entity);
        detail.LivestockCount = livestockCounts?.Total ?? 0;
        detail.ActiveLivestockCount = livestockCounts?.Active ?? 0;
        return detail;
    }

    public async Task<IList<FarmSummaryDto>> ListAsync(Guid companyId, CancellationToken ct)
    {
        return await ListByCompanyAsync(companyId, null, ct);
    }

    public async Task<IList<FarmSummaryDto>> ListByCompanyAsync(Guid companyId, string? search, CancellationToken ct)
    {
        var query = _db.Farms
            .Where(f => f.CompanyId == companyId && !f.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(f =>
                f.Name.Contains(s) ||
                f.Code.Contains(s));
        }

        return await query
            .OrderBy(f => f.Name)
            .Select(f => new FarmSummaryDto
            {
                Id = f.Id,
                CompanyId = f.CompanyId,
                Name = f.Name,
                Code = f.Code,
                City = f.Address != null ? f.Address.City : null,
                ManagerUserId = f.ManagerUserId,
                Currency = f.Currency,
                WeightUnit = f.WeightUnit,
                IsActive = f.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<FarmDetailDto> CreateAsync(FarmCreateDto dto, Guid companyId, CancellationToken ct)
    {
        var resolvedCompanyId = companyId == Guid.Empty ? dto.CompanyId : companyId;
        if (resolvedCompanyId == Guid.Empty)
            throw new DomainException("CompanyId is required.");

        var farm = new Farm(resolvedCompanyId, dto.Name, dto.Code)
        {
            Address = dto.Address,
            ManagerUserId = dto.ManagerUserId,
            Currency = dto.Currency,
            WeightUnit = dto.WeightUnit,
            IsActive = true
        };

        _db.Farms.Add(farm);
        await _db.SaveChangesAsync(ct);

        return MapToDetail(farm);
    }

    public async Task<FarmDetailDto> UpdateAsync(Guid id, FarmUpdateDto dto, Guid companyId, CancellationToken ct)
    {
        var entity = await _db.Farms
            .FirstOrDefaultAsync(f => f.Id == id && f.CompanyId == companyId && !f.IsDeleted, ct)
            ?? throw new DomainException("Farm not found.");

        entity.Name = dto.Name;
        entity.Code = dto.Code;
        entity.Address = dto.Address;
        entity.ManagerUserId = dto.ManagerUserId;
        entity.Currency = dto.Currency;
        entity.WeightUnit = dto.WeightUnit;
        entity.IsActive = dto.IsActive;
        entity.ModifiedAt = _dateTime.Now;

        await _db.SaveChangesAsync(ct);
        return MapToDetail(entity);
    }

    public async Task DeleteAsync(Guid id, Guid companyId, CancellationToken ct)
    {
        var entity = await _db.Farms
            .FirstOrDefaultAsync(f => f.Id == id && f.CompanyId == companyId && !f.IsDeleted, ct)
            ?? throw new DomainException("Farm not found.");
        entity.IsDeleted = true;
        entity.ModifiedAt = _dateTime.Now;
        await _db.SaveChangesAsync(ct);
    }

    private static FarmSummaryDto MapToSummary(Farm f) => new()
    {
        Id = f.Id,
        CompanyId = f.CompanyId,
        Name = f.Name,
        Code = f.Code,
        City = f.Address != null ? f.Address.City : null,
        ManagerUserId = f.ManagerUserId,
        Currency = f.Currency,
        WeightUnit = f.WeightUnit,
        IsActive = f.IsActive
    };

    private static FarmDetailDto MapToDetail(Farm f) => new()
    {
        Id = f.Id,
        CompanyId = f.CompanyId,
        Name = f.Name,
        Code = f.Code,
        Address = f.Address,
        ManagerUserId = f.ManagerUserId,
        Currency = f.Currency,
        WeightUnit = f.WeightUnit,
        IsActive = f.IsActive,
        LivestockCount = 0,
        ActiveLivestockCount = 0,
        CreatedAt = f.CreatedAt,
        ModifiedAt = f.ModifiedAt
    };
}
