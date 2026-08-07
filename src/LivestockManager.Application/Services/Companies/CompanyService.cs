using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Companies;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LivestockManager.Application.Services.Companies;

public class CompanyService : ICompanyService
{
    private readonly IAppDbContext _db;
    private readonly IDateTime _dateTime;

    public CompanyService(
        IAppDbContext db,
        IDateTime dateTime)
    {
        _db = db;
        _dateTime = dateTime;
    }

    public async Task<CompanyDetailDto> GetDefaultAsync(CancellationToken ct)
    {
        var entity = await _db.Companies.FirstOrDefaultAsync(ct)
            ?? throw new DomainException("No default company found.");
        return MapToDetail(entity);
    }

    public async Task<CompanyDetailDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new DomainException($"Company {id} not found.");
        return MapToDetail(entity);
    }

    public async Task<IList<CompanySummaryDto>> ListAsync(CancellationToken ct)
    {
        return await _db.Companies.AsQueryable().Select(MapToSummary).AsQueryable().ToListAsync(ct);
    }

    public async Task<CompanyDetailDto> UpdateAsync(Guid id, CompanyUpdateDto dto, CancellationToken ct)
    {
        var entity = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new DomainException($"Company {id} not found.");

        entity.Name = dto.Name;
        entity.RegistrationNumber = dto.RegistrationNumber;
        entity.TaxNumber = dto.TaxNumber;
        entity.Address = dto.Address;
        entity.Phone = dto.Phone;
        entity.Email = dto.Email;
        entity.LogoBlobName = dto.LogoBlobName;
        entity.Currency = dto.Currency;
        entity.WeightUnit = dto.WeightUnit;
        entity.InvoicePrefix = dto.InvoicePrefix;
        entity.ReceiptPrefix = dto.ReceiptPrefix;
        entity.TaxSettings = dto.TaxSettings;
        entity.FinancialYearStartMonth = dto.FinancialYearStartMonth;
        entity.IsActive = dto.IsActive;
        entity.ModifiedAt = _dateTime.Now;

        await _db.SaveChangesAsync(ct);
        return MapToDetail(entity);
    }

    public Task SwitchCompanyAsync(Guid companyId, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    private static CompanySummaryDto MapToSummary(Company c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        RegistrationNumber = c.RegistrationNumber,
        TaxNumber = c.TaxNumber,
        Currency = c.Currency,
        WeightUnit = c.WeightUnit,
        IsActive = c.IsActive
    };

    private static CompanyDetailDto MapToDetail(Company c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        RegistrationNumber = c.RegistrationNumber,
        TaxNumber = c.TaxNumber,
        Address = c.Address,
        Phone = c.Phone,
        Email = c.Email,
        LogoBlobName = c.LogoBlobName,
        Currency = c.Currency,
        WeightUnit = c.WeightUnit,
        InvoicePrefix = c.InvoicePrefix,
        ReceiptPrefix = c.ReceiptPrefix,
        TaxSettings = c.TaxSettings,
        FinancialYearStartMonth = c.FinancialYearStartMonth,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt,
        ModifiedAt = c.ModifiedAt
    };
}
