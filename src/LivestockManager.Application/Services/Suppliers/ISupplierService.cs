using LivestockManager.Application.DTOs.Suppliers;

namespace LivestockManager.Application.Services.Suppliers;

public interface ISupplierService
{
    Task<IList<SupplierSummaryDto>> ListAsync(Guid companyId, CancellationToken ct);
    Task<IList<SupplierSummaryDto>> SearchAsync(Guid companyId, string keyword, bool? onlyActive, CancellationToken ct);
    Task<SupplierDetailDto> GetByIdAsync(Guid id, CancellationToken ct);
    Task<SupplierDetailDto> CreateAsync(SupplierCreateDto dto, CancellationToken ct);
    Task<SupplierDetailDto> UpdateAsync(Guid id, SupplierUpdateDto dto, CancellationToken ct);
    Task ArchiveAsync(Guid id, CancellationToken ct);
}
