using LivestockManager.Application.DTOs.Expenses;

namespace LivestockManager.Application.Services.Expenses;

public interface IExpenseService
{
    Task<ExpenseDetailDto> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IList<ExpenseSummaryDto>> ListAsync(Guid companyId, Guid? farmId, Guid? supplierId, Guid? livestockId, ExpenseCategory? category, DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct);
    Task<ExpenseDetailDto> CreateAsync(ExpenseCreateDto dto, CancellationToken ct);
    Task<ExpenseDetailDto> UpdateAsync(Guid id, ExpenseUpdateDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, string reason, CancellationToken ct);
    Task<IList<ExpenseCategorySummaryDto>> CategorySummaryAsync(Guid companyId, DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct);
    Task<byte[]> ExportCsvAsync(Guid companyId, Guid? farmId, Guid? supplierId, Guid? livestockId, ExpenseCategory? category, DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct);
}
