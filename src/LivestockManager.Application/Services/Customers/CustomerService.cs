using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Customers;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LivestockManager.Application.Services.Customers;

public class CustomerService : ICustomerService
{
    private readonly IAppDbContext _db;
    private readonly IDateTime _dateTime;

    public CustomerService(
        IAppDbContext db,
        IDateTime dateTime)
    {
        _db = db;
        _dateTime = dateTime;
    }

    public async Task<CustomerDetailDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new DomainException($"Customer {id} not found.");
        return MapToDetail(entity);
    }

    public async Task<IList<CustomerSummaryDto>> ListAsync(Guid companyId, CancellationToken ct)
    {
        return await _db.Customers.Where(c => c.CompanyId == companyId).AsQueryable().Select(MapToSummary).AsQueryable().ToListAsync(ct);
    }

    public async Task<IList<CustomerSummaryDto>> SearchAsync(Guid companyId, string keyword, CancellationToken ct)
    {
        var query = _db.Customers.Where(c => c.CompanyId == companyId);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(c =>
                c.Name.Contains(keyword) ||
                c.CustomerCode.Contains(keyword) ||
                (c.TaxNumber != null && c.TaxNumber.Contains(keyword)) ||
                (c.Email != null && c.Email.Contains(keyword)) ||
                (c.Phone != null && c.Phone.Contains(keyword)));
        }
        return await query.AsQueryable().Select(MapToSummary).AsQueryable().ToListAsync(ct);
    }

    public async Task<CustomerDetailDto> CreateAsync(CustomerCreateDto dto, CancellationToken ct)
    {
        var customer = new Customer(dto.CompanyId, dto.CustomerCode, dto.Name)
        {
            IsBusiness = dto.IsBusiness,
            TaxNumber = dto.TaxNumber,
            BillingAddress = dto.BillingAddress,
            DeliveryAddress = dto.DeliveryAddress,
            Phone = dto.Phone,
            Email = dto.Email,
            CreditLimit = dto.CreditLimit.GetValueOrDefault(),
            PaymentTermsDays = dto.PaymentTermsDays.GetValueOrDefault(30),
            OpeningBalance = dto.OpeningBalance,
            Notes = dto.Notes,
            IsActive = true
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);

        return MapToDetail(customer);
    }

    public async Task<CustomerDetailDto> UpdateAsync(Guid id, CustomerUpdateDto dto, CancellationToken ct)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new DomainException($"Customer {id} not found.");

        entity.Name = dto.Name;
        entity.CustomerCode = dto.CustomerCode;
        entity.IsBusiness = dto.IsBusiness;
        entity.TaxNumber = dto.TaxNumber;
        entity.BillingAddress = dto.BillingAddress;
        entity.DeliveryAddress = dto.DeliveryAddress;
        entity.Phone = dto.Phone;
        entity.Email = dto.Email;
        entity.CreditLimit = dto.CreditLimit.GetValueOrDefault();
        entity.PaymentTermsDays = dto.PaymentTermsDays.GetValueOrDefault(30);
        entity.Notes = dto.Notes;
        entity.IsActive = dto.IsActive;
        entity.ModifiedAt = _dateTime.Now;

        await _db.SaveChangesAsync(ct);
        return MapToDetail(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new DomainException($"Customer {id} not found.");
        entity.IsDeleted = true;
        entity.ModifiedAt = _dateTime.Now;
        await _db.SaveChangesAsync(ct);
    }

    private static CustomerSummaryDto MapToSummary(Customer c) => new()
    {
        Id = c.Id,
        CompanyId = c.CompanyId,
        CustomerCode = c.CustomerCode,
        Name = c.Name,
        IsBusiness = c.IsBusiness,
        TaxNumber = c.TaxNumber,
        Email = c.Email,
        Phone = c.Phone,
        OpeningBalance = c.OpeningBalance,
        IsActive = c.IsActive
    };

    private static CustomerDetailDto MapToDetail(Customer c) => new()
    {
        Id = c.Id,
        CompanyId = c.CompanyId,
        CustomerCode = c.CustomerCode,
        Name = c.Name,
        IsBusiness = c.IsBusiness,
        TaxNumber = c.TaxNumber,
        BillingAddress = c.BillingAddress,
        DeliveryAddress = c.DeliveryAddress,
        Phone = c.Phone,
        Email = c.Email,
        CreditLimit = c.CreditLimit,
        PaymentTermsDays = c.PaymentTermsDays,
        OpeningBalance = c.OpeningBalance,
        Notes = c.Notes,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt,
        ModifiedAt = c.ModifiedAt
    };
}
