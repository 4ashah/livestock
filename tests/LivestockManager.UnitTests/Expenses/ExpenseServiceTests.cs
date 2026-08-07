using LivestockManager.Application.DTOs.Expenses;
using LivestockManager.Application.Services.Expenses;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace LivestockManager.UnitTests.Expenses;

public class ExpenseServiceTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static ExpenseTestScope CreateTestScope(IDateTime? dateTime = null)
    {
        var db = CreateInMemoryDb();
        var company = new Company("Test Co") { Currency = Currency.USD, InvoicePrefix = "INV", ReceiptPrefix = "RCT" };
        db.Companies.Add(company);
        var supplier = new Supplier(company.Id, "Test Supplier");
        db.Suppliers.Add(supplier);
        var farm = new Farm(company.Id, "Test Farm", "FARM01");
        db.Farms.Add(farm);
        var livestock = new Livestock(
            company.Id,
            "LS001",
            LivestockType.PurchasedCastratedRam,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            50m,
            WeightUnit.Kg,
            500m,
            farm.Id);
        db.Livestock.Add(livestock);
        db.SaveChanges();

        var dt = dateTime ?? new TestExpenseDateTime { Now = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero) };
        var service = new ExpenseService(db, dt);
        return new ExpenseTestScope(db, service, company.Id, supplier.Id, farm.Id, livestock.Id);
    }

    [Fact]
    public async Task Create_SetsAmountAndCalculatesTaxAndTotal()
    {
        var scope = CreateTestScope();
        var (_, service, companyId, supplierId, farmId, livestockId) = scope;

        var dto = new ExpenseCreateDto
        {
            CompanyId = companyId,
            FarmId = farmId,
            SupplierId = supplierId,
            LivestockId = livestockId,
            Category = ExpenseCategory.Feed,
            ExpenseDate = new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero),
            Currency = Currency.USD,
            Amount = 100m,
            TaxRate = 0.15m,
            PaymentMethod = PaymentMethod.Cash,
            Description = "Animal feed",
            Reference = "INV-001",
            Notes = "Monthly feed order"
        };

        var result = await service.CreateAsync(dto, companyId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(100m, result.Amount);
        Assert.Equal(0.15m, result.TaxRate);
        Assert.Equal(15m, result.TaxAmount);
        Assert.Equal(115m, result.Total);
        Assert.Equal(ExpenseCategory.Feed, result.Category);
        Assert.Equal(PaymentMethod.Cash, result.PaymentMethod);
        Assert.Equal(Currency.USD, result.Currency);
        Assert.Equal("Animal feed", result.Description);
        Assert.Equal("INV-001", result.Reference);
        Assert.Equal("Monthly feed order", result.Notes);
        Assert.Equal(companyId, result.CompanyId);
        Assert.Equal(farmId, result.FarmId);
        Assert.Equal(supplierId, result.SupplierId);
        Assert.Equal(livestockId, result.LivestockId);
    }

    [Fact]
    public async Task Create_ZeroAmount_Throws()
    {
        var scope = CreateTestScope();
        var (_, service, companyId, _, _, _) = scope;

        var dto = new ExpenseCreateDto
        {
            CompanyId = companyId,
            Category = ExpenseCategory.Feed,
            ExpenseDate = new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero),
            Amount = 0m,
            PaymentMethod = PaymentMethod.Cash
        };

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(dto, companyId, CancellationToken.None));
        Assert.Contains("Amount must be greater than zero", ex.Message);
    }

    [Fact]
    public async Task Update_RecalculatesTaxAndTotal()
    {
        var scope = CreateTestScope();
        var (_, service, companyId, _, _, _) = scope;

        var createDto = new ExpenseCreateDto
        {
            CompanyId = companyId,
            Category = ExpenseCategory.Veterinary,
            ExpenseDate = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
            Amount = 200m,
            TaxRate = 0.10m,
            PaymentMethod = PaymentMethod.BankTransfer
        };
        var created = await service.CreateAsync(createDto, companyId, CancellationToken.None);
        Assert.Equal(20m, created.TaxAmount);
        Assert.Equal(220m, created.Total);

        var updateDto = new ExpenseUpdateDto
        {
            Id = created.Id,
            Category = ExpenseCategory.Medication,
            ExpenseDate = new DateTimeOffset(2026, 1, 12, 0, 0, 0, TimeSpan.Zero),
            Amount = 300m,
            TaxRate = 0.20m,
            PaymentMethod = PaymentMethod.Card,
            Description = "Updated desc"
        };

        var updated = await service.UpdateAsync(created.Id, updateDto, companyId, CancellationToken.None);

        Assert.Equal(300m, updated.Amount);
        Assert.Equal(0.20m, updated.TaxRate);
        Assert.Equal(60m, updated.TaxAmount);
        Assert.Equal(360m, updated.Total);
        Assert.Equal(ExpenseCategory.Medication, updated.Category);
        Assert.Equal(PaymentMethod.Card, updated.PaymentMethod);
        Assert.Equal("Updated desc", updated.Description);
    }

    [Fact]
    public async Task Delete_SetsIsDeletedAndAppendsReasonToNotes()
    {
        var scope = CreateTestScope();
        var (db, service, companyId, _, _, _) = scope;

        var createDto = new ExpenseCreateDto
        {
            CompanyId = companyId,
            Category = ExpenseCategory.Maintenance,
            ExpenseDate = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
            Amount = 75m,
            TaxRate = 0,
            PaymentMethod = PaymentMethod.Cash,
            Notes = "Original note"
        };
        var created = await service.CreateAsync(createDto, companyId, CancellationToken.None);

        await service.DeleteAsync(created.Id, "Duplicate entry", companyId, CancellationToken.None);

        var entity = await db.Expenses.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == created.Id);
        Assert.NotNull(entity);
        Assert.True(entity.IsDeleted);
        Assert.Contains("Original note", entity.Notes);
        Assert.Contains("Duplicate entry", entity.Notes);
        Assert.Contains("Deleted:", entity.Notes);

        await Assert.ThrowsAsync<DomainException>(() => service.GetByIdAsync(created.Id, companyId, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_NullOrEmptyReason_Throws()
    {
        var scope = CreateTestScope();
        var (_, service, companyId, _, _, _) = scope;

        var createDto = new ExpenseCreateDto
        {
            CompanyId = companyId,
            Category = ExpenseCategory.Utilities,
            ExpenseDate = new DateTimeOffset(2026, 2, 5, 0, 0, 0, TimeSpan.Zero),
            Amount = 120m,
            PaymentMethod = PaymentMethod.BankTransfer
        };
        var created = await service.CreateAsync(createDto, companyId, CancellationToken.None);

        var ex1 = await Assert.ThrowsAsync<DomainException>(() => service.DeleteAsync(created.Id, null!, companyId, CancellationToken.None));
        Assert.Contains("reason is required", ex1.Message, StringComparison.OrdinalIgnoreCase);

        var ex2 = await Assert.ThrowsAsync<DomainException>(() => service.DeleteAsync(created.Id, "   ", companyId, CancellationToken.None));
        Assert.Contains("reason is required", ex2.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task List_FiltersByCategoryAndDateRange()
    {
        var scope = CreateTestScope();
        var (_, service, companyId, _, _, _) = scope;

        async Task<Guid> CreateExpense(ExpenseCategory cat, string date, decimal amt)
        {
            var dto = new ExpenseCreateDto
            {
                CompanyId = companyId,
                Category = cat,
                ExpenseDate = DateTimeOffset.Parse(date),
                Amount = amt,
                TaxRate = 0,
                PaymentMethod = PaymentMethod.Cash
            };
            var c = await service.CreateAsync(dto, companyId, CancellationToken.None);
            return c.Id;
        }

        var id1 = await CreateExpense(ExpenseCategory.Feed, "2026-01-10", 100m);
        var id2 = await CreateExpense(ExpenseCategory.Veterinary, "2026-01-15", 200m);
        var id3 = await CreateExpense(ExpenseCategory.Feed, "2026-02-01", 150m);
        var id4 = await CreateExpense(ExpenseCategory.Labor, "2026-01-20", 300m);

        var feedList = await service.ListAsync(companyId, null, null, null, ExpenseCategory.Feed, null, null, CancellationToken.None);
        Assert.Equal(2, feedList.Count);
        Assert.All(feedList, f => Assert.Equal(ExpenseCategory.Feed, f.Category));

        var janList = await service.ListAsync(companyId, null, null, null, null,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
        Assert.Equal(3, janList.Count);

        var feedJan = await service.ListAsync(companyId, null, null, null, ExpenseCategory.Feed,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero),
            CancellationToken.None);
        Assert.Single(feedJan);
        Assert.Equal(id1, feedJan.Single().Id);
    }

    [Fact]
    public async Task List_FiltersByFarmSupplierLivestock()
    {
        var scope = CreateTestScope();
        var (db, service, companyId, supplierId, farmId, livestockId) = scope;

        var supplier2 = new Supplier(companyId, "Supplier 2");
        db.Suppliers.Add(supplier2);
        var farm2 = new Farm(companyId, "Farm 2", "FARM02");
        db.Farms.Add(farm2);
        var livestock2 = new Livestock(
            companyId,
            "LS002",
            LivestockType.PurchasedCastratedRam,
            new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero),
            45m,
            WeightUnit.Kg,
            450m,
            farm2.Id);
        db.Livestock.Add(livestock2);
        db.SaveChanges();

        async Task<Guid> CreateExpense(Guid? fId, Guid? sId, Guid? lId)
        {
            var dto = new ExpenseCreateDto
            {
                CompanyId = companyId,
                FarmId = fId,
                SupplierId = sId,
                LivestockId = lId,
                Category = ExpenseCategory.Feed,
                ExpenseDate = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
                Amount = 50m,
                TaxRate = 0,
                PaymentMethod = PaymentMethod.Cash
            };
            var c = await service.CreateAsync(dto, companyId, CancellationToken.None);
            return c.Id;
        }

        var e1 = await CreateExpense(farmId, supplierId, livestockId);
        var e2 = await CreateExpense(farm2.Id, supplier2.Id, livestock2.Id);
        var e3 = await CreateExpense(farmId, supplier2.Id, null);
        var e4 = await CreateExpense(null, null, null);

        var farm1List = await service.ListAsync(companyId, farmId, null, null, null, null, null, CancellationToken.None);
        Assert.Equal(2, farm1List.Count);

        var sup1List = await service.ListAsync(companyId, null, supplierId, null, null, null, null, CancellationToken.None);
        Assert.Single(sup1List);
        Assert.Equal(e1, sup1List.Single().Id);

        var ls2List = await service.ListAsync(companyId, null, null, livestock2.Id, null, null, null, CancellationToken.None);
        Assert.Single(ls2List);
        Assert.Equal(e2, ls2List.Single().Id);
        Assert.Equal("Supplier 2", ls2List.Single().SupplierName);
        Assert.Equal("Farm 2", ls2List.Single().FarmName);
        Assert.Equal("LS002", ls2List.Single().LivestockCode);
    }

    [Fact]
    public async Task CategorySummary_ComputesPerCategory()
    {
        var scope = CreateTestScope();
        var (_, service, companyId, _, _, _) = scope;

        var expenses = new[]
        {
            new { Cat = ExpenseCategory.Feed, Amt = 100m, Tax = 0.10m },
            new { Cat = ExpenseCategory.Feed, Amt = 200m, Tax = 0.15m },
            new { Cat = ExpenseCategory.Veterinary, Amt = 300m, Tax = 0.05m },
            new { Cat = ExpenseCategory.Labor, Amt = 500m, Tax = 0m }
        };

        foreach (var e in expenses)
        {
            await service.CreateAsync(new ExpenseCreateDto
            {
                CompanyId = companyId,
                Category = e.Cat,
                ExpenseDate = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
                Amount = e.Amt,
                TaxRate = e.Tax,
                PaymentMethod = PaymentMethod.Cash
            }, companyId, CancellationToken.None);
        }

        var summary = await service.CategorySummaryAsync(companyId, null, null, CancellationToken.None);

        Assert.Equal(3, summary.Count);

        var feed = summary.First(s => s.Category == ExpenseCategory.Feed);
        Assert.Equal(2, feed.ExpenseCount);
        Assert.Equal(300m, feed.TotalAmount);
        decimal expectedFeedTax = Math.Round(100m * 0.10m, 2, MidpointRounding.AwayFromZero) + Math.Round(200m * 0.15m, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(expectedFeedTax, feed.TotalTax);
        Assert.Equal(300m + expectedFeedTax, feed.TotalWithTax);

        var vet = summary.First(s => s.Category == ExpenseCategory.Veterinary);
        Assert.Equal(1, vet.ExpenseCount);
        Assert.Equal(300m, vet.TotalAmount);
        Assert.Equal(Math.Round(300m * 0.05m, 2, MidpointRounding.AwayFromZero), vet.TotalTax);

        var labor = summary.First(s => s.Category == ExpenseCategory.Labor);
        Assert.Equal(1, labor.ExpenseCount);
        Assert.Equal(500m, labor.TotalAmount);
        Assert.Equal(0m, labor.TotalTax);
        Assert.Equal(500m, labor.TotalWithTax);
    }

    [Fact]
    public async Task ExportCsv_ReturnsNonEmptyCsvBytesWithHeader()
    {
        var scope = CreateTestScope();
        var (_, service, companyId, supplierId, farmId, livestockId) = scope;

        var dto = new ExpenseCreateDto
        {
            CompanyId = companyId,
            FarmId = farmId,
            SupplierId = supplierId,
            LivestockId = livestockId,
            Category = ExpenseCategory.Feed,
            ExpenseDate = new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero),
            Currency = Currency.USD,
            Amount = 150.50m,
            TaxRate = 0.10m,
            PaymentMethod = PaymentMethod.BankTransfer,
            Description = "Alfalfa hay",
            Reference = "REF-123"
        };
        await service.CreateAsync(dto, companyId, CancellationToken.None);

        var bytes = await service.ExportCsvAsync(companyId, null, null, null, null, null, null, CancellationToken.None);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        var utf8 = new UTF8Encoding(true);
        var csvContent = utf8.GetString(bytes);

        Assert.Contains("Date,Category,Farm,Supplier,Livestock,Description,Amount,TaxRate,TaxAmount,Total,PaymentMethod,Currency,Reference", csvContent);
        Assert.Contains("2026-03-15", csvContent);
        Assert.Contains("Feed", csvContent);
        Assert.Contains("Test Farm", csvContent);
        Assert.Contains("Test Supplier", csvContent);
        Assert.Contains("LS001", csvContent);
        Assert.Contains("Alfalfa hay", csvContent);
        Assert.Contains("150.50", csvContent);
        Assert.Contains("15.05", csvContent);
        Assert.Contains("165.55", csvContent);
        Assert.Contains("BankTransfer", csvContent);
        Assert.Contains("USD", csvContent);
        Assert.Contains("REF-123", csvContent);
    }

    [Fact]
    public async Task TaxRateClamped_WhenGreaterThan1OrLessThan0()
    {
        var scope = CreateTestScope();
        var (_, service, companyId, _, _, _) = scope;

        var dtoHigh = new ExpenseCreateDto
        {
            CompanyId = companyId,
            Category = ExpenseCategory.Equipment,
            ExpenseDate = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
            Amount = 200m,
            TaxRate = 1.50m,
            PaymentMethod = PaymentMethod.Cash
        };
        var resultHigh = await service.CreateAsync(dtoHigh, companyId, CancellationToken.None);
        Assert.Equal(1m, resultHigh.TaxRate);
        Assert.Equal(200m, resultHigh.TaxAmount);
        Assert.Equal(400m, resultHigh.Total);

        var dtoLow = new ExpenseCreateDto
        {
            CompanyId = companyId,
            Category = ExpenseCategory.Insurance,
            ExpenseDate = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
            Amount = 200m,
            TaxRate = -0.05m,
            PaymentMethod = PaymentMethod.Cash
        };
        var resultLow = await service.CreateAsync(dtoLow, companyId, CancellationToken.None);
        Assert.Equal(0m, resultLow.TaxRate);
        Assert.Equal(0m, resultLow.TaxAmount);
        Assert.Equal(200m, resultLow.Total);

        var updateDto = new ExpenseUpdateDto
        {
            Id = resultHigh.Id,
            Category = ExpenseCategory.Equipment,
            ExpenseDate = resultHigh.ExpenseDate,
            Amount = 100m,
            TaxRate = 5m,
            PaymentMethod = PaymentMethod.Cash
        };
        var updated = await service.UpdateAsync(resultHigh.Id, updateDto, companyId, CancellationToken.None);
        Assert.Equal(1m, updated.TaxRate);
        Assert.Equal(100m, updated.TaxAmount);
        Assert.Equal(200m, updated.Total);
    }
}

public sealed class ExpenseTestScope : IDisposable
{
    public AppDbContext Db { get; }
    public ExpenseService Service { get; }
    public Guid CompanyId { get; }
    public Guid SupplierId { get; }
    public Guid? FarmId { get; }
    public Guid? LivestockId { get; }

    public ExpenseTestScope(AppDbContext db, ExpenseService service, Guid companyId, Guid supplierId, Guid? farmId, Guid? livestockId)
    {
        Db = db;
        Service = service;
        CompanyId = companyId;
        SupplierId = supplierId;
        FarmId = farmId;
        LivestockId = livestockId;
    }

    public void Deconstruct(out AppDbContext db, out ExpenseService service, out Guid companyId, out Guid supplierId, out Guid? farmId, out Guid? livestockId)
    {
        db = Db;
        service = Service;
        companyId = CompanyId;
        supplierId = SupplierId;
        farmId = FarmId;
        livestockId = LivestockId;
    }

    public void Dispose()
    {
        Db.Dispose();
    }
}

public class TestExpenseDateTime : IDateTime
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
}
