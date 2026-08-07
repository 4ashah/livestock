using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Expenses;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Exceptions;
using DE = LivestockManager.Domain.Entities;

namespace LivestockManager.Application.Services.Expenses;

public class ExpenseService : IExpenseService
{
    private readonly IAppDbContext _db;
    private readonly IDateTime _dateTime;

    public ExpenseService(IAppDbContext db, IDateTime dateTime)
    {
        _db = db;
        _dateTime = dateTime;
    }

    public async Task<ExpenseDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct)
    {
        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId && !e.IsDeleted, ct)
            ?? throw new DomainException("Expense not found.");

        return MapToDetail(expense);
    }

    public async Task<IList<ExpenseSummaryDto>> ListAsync(
        Guid companyId,
        Guid? farmId,
        Guid? supplierId,
        Guid? livestockId,
        ExpenseCategory? category,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        CancellationToken ct)
    {
        var query = from e in _db.Expenses
                    join f in _db.Farms on e.FarmId equals f.Id into ff
                    from f in ff.DefaultIfEmpty()
                    join s in _db.Suppliers on e.SupplierId equals s.Id into ss
                    from s in ss.DefaultIfEmpty()
                    join l in _db.Livestock on e.LivestockId equals l.Id into ll
                    from l in ll.DefaultIfEmpty()
                    where e.CompanyId == companyId && !e.IsDeleted
                    select new { e, f, s, l };

        if (farmId.HasValue)
            query = query.Where(x => x.e.FarmId == farmId.Value);

        if (supplierId.HasValue)
            query = query.Where(x => x.e.SupplierId == supplierId.Value);

        if (livestockId.HasValue)
            query = query.Where(x => x.e.LivestockId == livestockId.Value);

        if (category.HasValue)
            query = query.Where(x => x.e.Category == category.Value);

        if (fromDate.HasValue)
            query = query.Where(x => x.e.ExpenseDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.e.ExpenseDate <= toDate.Value);

        var result = await query
            .Select(x => new ExpenseSummaryDto
            {
                Id = x.e.Id,
                ExpenseDate = x.e.ExpenseDate,
                Category = x.e.Category,
                FarmId = x.e.FarmId,
                FarmName = x.f != null ? x.f.Name : string.Empty,
                SupplierId = x.e.SupplierId,
                SupplierName = x.s != null ? x.s.Name : string.Empty,
                LivestockId = x.e.LivestockId,
                LivestockCode = x.l != null ? x.l.LivestockId : string.Empty,
                Description = x.e.Description,
                Amount = x.e.Amount,
                TaxAmount = x.e.TaxAmount,
                Total = x.e.Total,
                PaymentMethod = x.e.PaymentMethod,
                Currency = x.e.Currency,
                CreatedAt = x.e.CreatedAt
            })
            .ToListAsync(ct);

        return result;
    }

    public async Task<ExpenseDetailDto> CreateAsync(ExpenseCreateDto dto, Guid companyId, CancellationToken ct)
    {
        var resolvedCompanyId = companyId == Guid.Empty ? dto.CompanyId : companyId;
        if (resolvedCompanyId == Guid.Empty)
            throw new DomainException("CompanyId is required.");

        if (dto.Amount <= 0)
            throw new DomainException("Amount must be greater than zero.");

        var taxRate = NormalizeTaxRate(dto.TaxRate ?? 0m);
        var taxAmount = Math.Round(dto.Amount * taxRate, 2, MidpointRounding.AwayFromZero);
        var total = dto.Amount + taxAmount;

        var expense = new DE.Expense(resolvedCompanyId, dto.Category, dto.ExpenseDate, dto.Amount)
        {
            FarmId = dto.FarmId,
            SupplierId = dto.SupplierId,
            LivestockId = dto.LivestockId,
            Currency = dto.Currency ?? Currency.USD,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            Total = total,
            PaymentMethod = dto.PaymentMethod,
            Description = dto.Description,
            Reference = dto.Reference,
            DocumentId = dto.DocumentId,
            Notes = dto.Notes
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync(ct);

        return MapToDetail(expense);
    }

    public async Task<ExpenseDetailDto> UpdateAsync(Guid id, ExpenseUpdateDto dto, Guid companyId, CancellationToken ct)
    {
        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId && !e.IsDeleted, ct)
            ?? throw new DomainException("Expense not found.");

        if (dto.Amount <= 0)
            throw new DomainException("Amount must be greater than zero.");

        var taxRate = NormalizeTaxRate(dto.TaxRate ?? expense.TaxRate);
        var taxAmount = Math.Round(dto.Amount * taxRate, 2, MidpointRounding.AwayFromZero);
        var total = dto.Amount + taxAmount;

        expense.FarmId = dto.FarmId;
        expense.SupplierId = dto.SupplierId;
        expense.LivestockId = dto.LivestockId;
        expense.Category = dto.Category;
        expense.ExpenseDate = dto.ExpenseDate;
        expense.Currency = dto.Currency ?? expense.Currency;
        expense.Amount = dto.Amount;
        expense.TaxRate = taxRate;
        expense.TaxAmount = taxAmount;
        expense.Total = total;
        expense.PaymentMethod = dto.PaymentMethod;
        expense.Description = dto.Description;
        expense.Reference = dto.Reference;
        expense.DocumentId = dto.DocumentId;
        expense.Notes = dto.Notes;
        expense.ModifiedAt = _dateTime.Now;

        await _db.SaveChangesAsync(ct);

        return MapToDetail(expense);
    }

    public async Task DeleteAsync(Guid id, string reason, Guid companyId, CancellationToken ct)
    {
        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId && !e.IsDeleted, ct)
            ?? throw new DomainException("Expense not found.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("A reason is required to delete an expense.");

        expense.IsDeleted = true;
        expense.Notes = string.IsNullOrWhiteSpace(expense.Notes)
            ? $"Deleted: {reason}"
            : $"{expense.Notes}; Deleted: {reason}";
        expense.ModifiedAt = _dateTime.Now;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IList<ExpenseCategorySummaryDto>> CategorySummaryAsync(
        Guid companyId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        CancellationToken ct)
    {
        var query = _db.Expenses
            .Where(e => e.CompanyId == companyId && !e.IsDeleted);

        if (fromDate.HasValue)
            query = query.Where(e => e.ExpenseDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(e => e.ExpenseDate <= toDate.Value);

        var grouped = await query
            .GroupBy(e => e.Category)
            .Select(g => new ExpenseCategorySummaryDto
            {
                Category = g.Key,
                ExpenseCount = g.Count(),
                TotalAmount = Math.Round(g.Sum(e => e.Amount), 2, MidpointRounding.AwayFromZero),
                TotalTax = Math.Round(g.Sum(e => e.TaxAmount), 2, MidpointRounding.AwayFromZero),
                TotalWithTax = Math.Round(g.Sum(e => e.Total), 2, MidpointRounding.AwayFromZero)
            })
            .ToListAsync(ct);

        return grouped;
    }

    public async Task<byte[]> ExportCsvAsync(
        Guid companyId,
        Guid? farmId,
        Guid? supplierId,
        Guid? livestockId,
        ExpenseCategory? category,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        CancellationToken ct)
    {
        var query = from e in _db.Expenses
                    join f in _db.Farms on e.FarmId equals f.Id into ff
                    from f in ff.DefaultIfEmpty()
                    join s in _db.Suppliers on e.SupplierId equals s.Id into ss
                    from s in ss.DefaultIfEmpty()
                    join l in _db.Livestock on e.LivestockId equals l.Id into ll
                    from l in ll.DefaultIfEmpty()
                    where e.CompanyId == companyId && !e.IsDeleted
                    select new { e, f, s, l };

        if (farmId.HasValue)
            query = query.Where(x => x.e.FarmId == farmId.Value);

        if (supplierId.HasValue)
            query = query.Where(x => x.e.SupplierId == supplierId.Value);

        if (livestockId.HasValue)
            query = query.Where(x => x.e.LivestockId == livestockId.Value);

        if (category.HasValue)
            query = query.Where(x => x.e.Category == category.Value);

        if (fromDate.HasValue)
            query = query.Where(x => x.e.ExpenseDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.e.ExpenseDate <= toDate.Value);

        var csvRows = await query
            .Select(x => new CsvExportRow
            {
                Date = x.e.ExpenseDate.ToString("yyyy-MM-dd"),
                Category = x.e.Category.ToString(),
                Farm = x.f != null ? x.f.Name : string.Empty,
                Supplier = x.s != null ? x.s.Name : string.Empty,
                Livestock = x.l != null ? x.l.LivestockId : string.Empty,
                Description = x.e.Description ?? string.Empty,
                Amount = x.e.Amount.ToString("F2"),
                TaxRate = (x.e.Amount > 0 ? (x.e.TaxAmount / x.e.Amount) : 0m).ToString("F4"),
                TaxAmount = x.e.TaxAmount.ToString("F2"),
                Total = x.e.Total.ToString("F2"),
                PaymentMethod = x.e.PaymentMethod.ToString(),
                Currency = x.e.Currency.ToString(),
                Reference = x.e.Reference ?? string.Empty
            })
            .ToListAsync(ct);

        var columns = new[] { "Date", "Category", "Farm", "Supplier", "Livestock", "Description", "Amount", "TaxRate", "TaxAmount", "Total", "PaymentMethod", "Currency", "Reference" };

        return CsvExporter.Write(csvRows, columns);
    }

    private static decimal NormalizeTaxRate(decimal taxRate)
    {
        if (taxRate < 0m) taxRate = 0m;
        if (taxRate > 1m) taxRate = 1m;
        return taxRate;
    }

    private static ExpenseDetailDto MapToDetail(DE.Expense e) => new()
    {
        Id = e.Id,
        CompanyId = e.CompanyId,
        FarmId = e.FarmId,
        SupplierId = e.SupplierId,
        LivestockId = e.LivestockId,
        Category = e.Category,
        ExpenseDate = e.ExpenseDate,
        Currency = e.Currency,
        Amount = e.Amount,
        TaxRate = e.TaxRate,
        TaxAmount = e.TaxAmount,
        Total = e.Total,
        PaymentMethod = e.PaymentMethod,
        Description = e.Description,
        Reference = e.Reference,
        DocumentId = e.DocumentId,
        Notes = e.Notes,
        CreatedAt = e.CreatedAt,
        ModifiedAt = e.ModifiedAt
    };
}

public class CsvExportRow
{
    public string Date { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Farm { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public string Livestock { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string TaxRate { get; set; } = string.Empty;
    public string TaxAmount { get; set; } = string.Empty;
    public string Total { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
}
