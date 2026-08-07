using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Suppliers;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LivestockManager.Application.Services.Suppliers;

public class SupplierService : ISupplierService
{
    private readonly IAppDbContext _db;
    private readonly IDateTime _dateTime;

    public SupplierService(
        IAppDbContext db,
        IDateTime dateTime)
    {
        _db = db;
        _dateTime = dateTime;
    }

    public async Task<IList<SupplierSummaryDto>> ListAsync(Guid companyId, CancellationToken ct)
    {
        var entities = await _db.Suppliers
            .Where(s => s.CompanyId == companyId)
            .ToListAsync(ct);
        return entities.Select(MapToSummary).ToList();
    }

    public async Task<IList<SupplierSummaryDto>> SearchAsync(Guid companyId, string keyword, bool? onlyActive, CancellationToken ct)
    {
        var query = _db.Suppliers.Where(s => s.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(s =>
                s.Name.Contains(keyword) ||
                (s.Code != null && s.Code.Contains(keyword)) ||
                (s.TaxNumber != null && s.TaxNumber.Contains(keyword)) ||
                (s.Email != null && s.Email.Contains(keyword)) ||
                (s.Phone != null && s.Phone.Contains(keyword)));
        }

        if (onlyActive.HasValue)
        {
            query = query.Where(s => s.IsActive == onlyActive.Value);
        }

        var entities = await query.ToListAsync(ct);
        return entities.Select(MapToSummary).ToList();
    }

    public async Task<SupplierDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct)
    {
        var entity = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id && s.CompanyId == companyId, ct)
            ?? throw new DomainException("Supplier not found.");
        return MapToDetail(entity);
    }

    public async Task<SupplierDetailDto> CreateAsync(SupplierCreateDto dto, Guid companyId, CancellationToken ct)
    {
        var resolvedCompanyId = companyId == Guid.Empty ? dto.CompanyId : companyId;

        var duplicate = await _db.Suppliers
            .IgnoreQueryFilters()
            .AnyAsync(s => s.CompanyId == resolvedCompanyId && s.Code == dto.Code && !s.IsDeleted, ct);
        if (duplicate)
        {
            throw new DomainException($"Supplier code '{dto.Code}' already exists within the company.");
        }

        var supplier = new Supplier(resolvedCompanyId, dto.Name)
        {
            Code = dto.Code,
            LegalName = dto.LegalName,
            TaxNumber = dto.TaxNumber,
            Address = dto.Address,
            Phone = dto.Phone,
            Email = dto.Email,
            BankAccount = dto.BankAccount,
            PaymentTermsDays = dto.PaymentTermsDays,
            Currency = dto.Currency ?? Currency.USD,
            IsActive = dto.IsActive,
            Notes = dto.Notes
        };

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync(ct);

        return MapToDetail(supplier);
    }

    public async Task<SupplierDetailDto> UpdateAsync(Guid id, SupplierUpdateDto dto, Guid companyId, CancellationToken ct)
    {
        var entity = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id && s.CompanyId == companyId, ct)
            ?? throw new DomainException("Supplier not found.");

        if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code != entity.Code)
        {
            var duplicate = await _db.Suppliers
                .IgnoreQueryFilters()
                .AnyAsync(s => s.CompanyId == entity.CompanyId && s.Code == dto.Code && s.Id != id && !s.IsDeleted, ct);
            if (duplicate)
            {
                throw new DomainException($"Supplier code '{dto.Code}' already exists within the company.");
            }
        }

        entity.Code = dto.Code;
        entity.Name = dto.Name;
        entity.LegalName = dto.LegalName;
        entity.TaxNumber = dto.TaxNumber;
        entity.Address = dto.Address;
        entity.Phone = dto.Phone;
        entity.Email = dto.Email;
        entity.BankAccount = dto.BankAccount;
        entity.PaymentTermsDays = dto.PaymentTermsDays;
        entity.Currency = dto.Currency ?? Currency.USD;
        entity.IsActive = dto.IsActive;
        entity.Notes = dto.Notes;
        entity.ModifiedAt = _dateTime.Now;

        await _db.SaveChangesAsync(ct);
        return MapToDetail(entity);
    }

    public async Task ArchiveAsync(Guid id, Guid companyId, CancellationToken ct)
    {
        var entity = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id && s.CompanyId == companyId, ct)
            ?? throw new DomainException("Supplier not found.");

        entity.IsActive = false;
        entity.IsDeleted = true;
        entity.ModifiedAt = _dateTime.Now;
        await _db.SaveChangesAsync(ct);
    }

    private static SupplierSummaryDto MapToSummary(Supplier s) => new()
    {
        Id = s.Id,
        Code = s.Code ?? string.Empty,
        Name = s.Name,
        IsActive = s.IsActive,
        Email = s.Email,
        Phone = s.Phone,
        TaxNumber = s.TaxNumber,
        PaymentTermsDays = s.PaymentTermsDays,
        CreatedAt = s.CreatedAt
    };

    private static SupplierDetailDto MapToDetail(Supplier s) => new()
    {
        Id = s.Id,
        CompanyId = s.CompanyId,
        Code = s.Code ?? string.Empty,
        Name = s.Name,
        LegalName = s.LegalName,
        TaxNumber = s.TaxNumber,
        Address = s.Address,
        Phone = s.Phone,
        Email = s.Email,
        BankAccount = s.BankAccount,
        PaymentTermsDays = s.PaymentTermsDays,
        Currency = s.Currency,
        IsActive = s.IsActive,
        Notes = s.Notes,
        CreatedAt = s.CreatedAt,
        ModifiedAt = s.ModifiedAt
    };
}
