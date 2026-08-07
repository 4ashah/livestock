using System.Text;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Dashboard;
using LivestockManager.Application.Services.Expenses;
using LivestockManager.Application.Services.Reports;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LivestockManager.UnitTests.Reports;

public class DashboardAndProfitabilityTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static ReportTestScope CreateTestScope(DateTimeOffset? fixedNow = null)
    {
        var db = CreateInMemoryDb();
        var now = fixedNow ?? new DateTimeOffset(2026, 8, 15, 10, 0, 0, TimeSpan.Zero);
        var clock = new TestDashboardClock { Now = now };
        var expenseService = new ExpenseService(db, clock);
        var reportService = new ReportService(db, clock, expenseService);

        var company = new Company("Test Ranch Inc") { Currency = Currency.USD, InvoicePrefix = "INV", ReceiptPrefix = "RCT" };
        db.Companies.Add(company);
        db.SaveChanges();

        return new ReportTestScope(db, reportService, expenseService, company.Id, clock);
    }

    [Fact]
    public async Task GetDashboardKpis_ReturnsCorrect14CountShape()
    {
        using var scope = CreateTestScope();
        var (db, svc, _, companyId, _) = scope;

        var farm = new Farm(companyId, "Farm A", "F001");
        db.Farms.Add(farm);

        var ls = new Livestock(companyId, "LS001", LivestockType.PurchasedCastratedRam,
            new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            50m, WeightUnit.Kg, 500m, farm.Id);
        ls.CurrentWeight = 60m;
        ls.CurrentWeightDate = new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
        db.Livestock.Add(ls);

        var customer = new Customer(companyId, "CUST01", "Cust A");
        db.Customers.Add(customer);

        var inv = new Invoice(companyId, customer.Id,
            new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 25, 0, 0, 0, TimeSpan.Zero))
        {
            Status = InvoiceStatus.Confirmed,
            GrandTotal = 1000m,
            PaidAmount = 0m
        };
        db.Invoices.Add(inv);

        var supplier = new Supplier(companyId, "Sup A");
        db.Suppliers.Add(supplier);

        var purch = new Purchase(companyId, supplier.Id,
            new DateTimeOffset(2026, 8, 3, 0, 0, 0, TimeSpan.Zero))
        {
            Status = PurchaseStatus.Posted,
            GrandTotal = 300m
        };
        db.Purchases.Add(purch);

        var exp = new Expense(companyId, ExpenseCategory.Feed,
            new DateTimeOffset(2026, 8, 7, 0, 0, 0, TimeSpan.Zero), 100m)
        {
            TaxRate = 0.1m,
            TaxAmount = 10m,
            Total = 110m
        };
        db.Expenses.Add(exp);

        var pay = new Payment(companyId, customer.Id, null,
            new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero),
            PaymentMethod.Cash, 500m);
        db.Payments.Add(pay);

        await db.SaveChangesAsync();

        var kpis = await svc.GetDashboardKpisAsync(companyId, CancellationToken.None);

        Assert.NotNull(kpis);
        Assert.Equal("Test Ranch Inc", kpis.CompanyName);
        Assert.Equal(Currency.USD, kpis.CompanyCurrency);

        var ops = new int?[] { kpis.ActiveLivestockCount, kpis.NewRegistrationsThisMonth, kpis.DischargesToday, kpis.PendingWeighingsCount, kpis.FarmsActive };
        Assert.All(ops, v => Assert.True(v.HasValue));

        Assert.True(kpis.RevenueMonthToDate >= 0);
        Assert.True(kpis.PaidReceiptsMonthToDate >= 0);
        Assert.True(kpis.OutstandingInvoicesAmountTotal >= 0);
        Assert.True(kpis.OverdueInvoicesAmountTotal >= 0);
        Assert.True(kpis.PurchasesMonthToDateAmount >= 0);
        Assert.True(kpis.ExpensesMonthToDateAmount >= 0);
    }

    [Fact]
    public async Task CompleteLivestockProfitability_ExcludesUnrealizedLivestock()
    {
        using var scope = CreateTestScope();
        var (db, svc, _, companyId, _) = scope;

        var farm = new Farm(companyId, "F1", "F001");
        db.Farms.Add(farm);

        var sold = new Livestock(companyId, "SOLD01", LivestockType.PurchasedCastratedRam,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            50m, WeightUnit.Kg, 500m, farm.Id);
        sold.Discharge(DischargeCondition.Sold, new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), 800m);
        db.Livestock.Add(sold);

        var active = new Livestock(companyId, "ACT01", LivestockType.PurchasedEwe,
            new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
            40m, WeightUnit.Kg, 300m, farm.Id);
        db.Livestock.Add(active);

        var deceased = new Livestock(companyId, "DEC01", LivestockType.PurchasedCastratedRam,
            new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            50m, WeightUnit.Kg, 400m, farm.Id);
        deceased.Discharge(DischargeCondition.Deceased, new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero));
        db.Livestock.Add(deceased);

        await db.SaveChangesAsync();

        var result = await svc.CompleteLivestockProfitabilityAsync(companyId, null, null, null, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("SOLD01", result[0].LivestockDisplayId);
        Assert.DoesNotContain(result, r => r.LivestockDisplayId == "ACT01");
        Assert.DoesNotContain(result, r => r.LivestockDisplayId == "DEC01");
    }

    [Fact]
    public async Task CompleteLivestockProfitability_DeductsDirectExpenses()
    {
        using var scope = CreateTestScope();
        var (db, svc, _, companyId, _) = scope;

        var farm = new Farm(companyId, "F1", "F001");
        db.Farms.Add(farm);

        var sold = new Livestock(companyId, "SOLD02", LivestockType.PurchasedCastratedRam,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            50m, WeightUnit.Kg, 500m, farm.Id);
        sold.Discharge(DischargeCondition.Sold, new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), 1000m);
        db.Livestock.Add(sold);
        await db.SaveChangesAsync();

        var directExp1 = new Expense(companyId, ExpenseCategory.Veterinary,
            new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero), 80m)
        {
            FarmId = farm.Id,
            LivestockId = sold.Id,
            TaxRate = 0.1m,
            TaxAmount = 8m,
            Total = 88m
        };
        var directExp2 = new Expense(companyId, ExpenseCategory.Feed,
            new DateTimeOffset(2026, 7, 15, 0, 0, 0, TimeSpan.Zero), 50m)
        {
            FarmId = farm.Id,
            LivestockId = sold.Id,
            TaxRate = 0m,
            TaxAmount = 0m,
            Total = 50m
        };
        db.Expenses.AddRange(directExp1, directExp2);
        await db.SaveChangesAsync();

        var result = await svc.CompleteLivestockProfitabilityAsync(companyId, null, null, null, CancellationToken.None);

        Assert.Single(result);
        var row = result[0];
        Assert.Equal(1000m, row.SoldAmount);
        Assert.Equal(500m, row.PurchaseAmount);
        Assert.Equal(500m, row.BasicProfitLoss);
        Assert.Equal(138m, row.DirectExpenses);
        Assert.Equal(362m, row.CompleteProfitLoss);
    }

    [Fact]
    public async Task CompleteLivestockProfitability_ExcludesCancelledVoidedAndReversed()
    {
        using var scope = CreateTestScope();
        var (db, svc, _, companyId, clock) = scope;

        var farm = new Farm(companyId, "F1", "F001");
        var customer = new Customer(companyId, "CUST02", "Cust A");
        db.Farms.Add(farm);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var saleGood = new Sale(companyId, customer.Id, new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero)) { Status = SaleStatus.Completed };
        var saleCancelled = new Sale(companyId, customer.Id, new DateTimeOffset(2026, 8, 2, 0, 0, 0, TimeSpan.Zero)) { Status = SaleStatus.Cancelled };
        db.Sales.AddRange(saleGood, saleCancelled);
        await db.SaveChangesAsync();

        var siGood = new SaleItem(saleGood.Id, 800m, 1m, "Animal");
        var siCancelled = new SaleItem(saleCancelled.Id, 900m, 1m, "Animal2");
        db.SaleItems.AddRange(siGood, siCancelled);
        await db.SaveChangesAsync();

        var invCancelled = new Invoice(companyId, customer.Id,
            new DateTimeOffset(2026, 8, 2, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 20, 0, 0, 0, TimeSpan.Zero))
        {
            SaleId = saleCancelled.Id,
            Status = InvoiceStatus.Cancelled,
            GrandTotal = 900m
        };
        db.Invoices.Add(invCancelled);
        await db.SaveChangesAsync();

        var soldGood = new Livestock(companyId, "GOOD01", LivestockType.PurchasedCastratedRam,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            50m, WeightUnit.Kg, 500m, farm.Id)
        {
            SaleItemId = siGood.Id
        };
        soldGood.Discharge(DischargeCondition.Sold, new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero), 800m);

        var soldCancelled = new Livestock(companyId, "CAN01", LivestockType.PurchasedEwe,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            40m, WeightUnit.Kg, 400m, farm.Id)
        {
            SaleItemId = siCancelled.Id
        };
        soldCancelled.Discharge(DischargeCondition.Sold, new DateTimeOffset(2026, 8, 2, 0, 0, 0, TimeSpan.Zero), 900m);

        db.Livestock.AddRange(soldGood, soldCancelled);
        await db.SaveChangesAsync();

        var result = await svc.CompleteLivestockProfitabilityAsync(companyId, null, null, null, CancellationToken.None);

        Assert.Contains(result, r => r.LivestockDisplayId == "GOOD01");
        Assert.DoesNotContain(result, r => r.LivestockDisplayId == "CAN01");
    }

    [Fact]
    public async Task CompleteFarmProfitability_AggregatesPerFarm()
    {
        using var scope = CreateTestScope();
        var (db, svc, _, companyId, _) = scope;

        var farmA = new Farm(companyId, "Farm A", "F001");
        var farmB = new Farm(companyId, "Farm B", "F002");
        db.Farms.AddRange(farmA, farmB);
        await db.SaveChangesAsync();

        var soldA1 = new Livestock(companyId, "FA1", LivestockType.PurchasedCastratedRam,
            new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            50m, WeightUnit.Kg, 500m, farmA.Id);
        soldA1.Discharge(DischargeCondition.Sold, new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), 800m);

        var soldA2 = new Livestock(companyId, "FA2", LivestockType.PurchasedEwe,
            new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            40m, WeightUnit.Kg, 300m, farmA.Id);
        soldA2.Discharge(DischargeCondition.Sold, new DateTimeOffset(2026, 8, 6, 0, 0, 0, TimeSpan.Zero), 500m);

        var soldB1 = new Livestock(companyId, "FB1", LivestockType.PurchasedCastratedRam,
            new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            50m, WeightUnit.Kg, 600m, farmB.Id);
        soldB1.Discharge(DischargeCondition.Sold, new DateTimeOffset(2026, 8, 7, 0, 0, 0, TimeSpan.Zero), 900m);

        var activeA = new Livestock(companyId, "ACT", LivestockType.PurchasedEwe,
            new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
            40m, WeightUnit.Kg, 300m, farmA.Id);

        db.Livestock.AddRange(soldA1, soldA2, soldB1, activeA);
        await db.SaveChangesAsync();

        var result = await svc.CompleteFarmProfitabilityAsync(companyId, null, null, CancellationToken.None);

        Assert.Equal(2, result.Count);
        var fa = result.First(f => f.FarmId == farmA.Id);
        var fb = result.First(f => f.FarmId == farmB.Id);

        Assert.Equal(1, fa.ActiveLivestockCount);
        Assert.Equal(800m, fa.TotalPurchases);
        Assert.Equal(1300m, fa.TotalSales);

        Assert.Equal(0, fb.ActiveLivestockCount);
        Assert.Equal(600m, fb.TotalPurchases);
        Assert.Equal(900m, fb.TotalSales);
    }

    [Fact]
    public async Task ReceivableAging_HasFiveBuckets()
    {
        using var scope = CreateTestScope();
        var (db, svc, _, companyId, clock) = scope;
        var now = clock.Now;

        var customer = new Customer(companyId, "CUST03", "Cust A");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var invoices = new[]
        {
            new { Status = InvoiceStatus.Confirmed, DueOffset = -60, Total = 500m, Paid = 0m },
            new { Status = InvoiceStatus.PartiallyPaid, DueOffset = -15, Total = 400m, Paid = 100m },
            new { Status = InvoiceStatus.Confirmed, DueOffset = -5, Total = 300m, Paid = 0m },
            new { Status = InvoiceStatus.Confirmed, DueOffset = 5, Total = 200m, Paid = 0m },
            new { Status = InvoiceStatus.Cancelled, DueOffset = -10, Total = 1000m, Paid = 0m }
        };

        foreach (var i in invoices)
        {
            var inv = new Invoice(companyId, customer.Id,
                now.AddDays(i.DueOffset - 10),
                now.AddDays(i.DueOffset))
            {
                Status = i.Status,
                GrandTotal = i.Total,
                PaidAmount = i.Paid
            };
            db.Invoices.Add(inv);
        }
        await db.SaveChangesAsync();

        var result = await svc.ReceivableAgingAsync(companyId, CancellationToken.None);

        Assert.Equal(4, result.Count);
        Assert.DoesNotContain(result, r => r.GrandTotal == 1000m);
        Assert.All(result, r => Assert.True(r.OutstandingAmount > 0));
    }

    [Fact]
    public async Task PurchasesCsvExport_HasHeaderAndRows()
    {
        using var scope = CreateTestScope();
        var (db, svc, _, companyId, _) = scope;

        var supplier = new Supplier(companyId, "Sup Alpha");
        var farm = new Farm(companyId, "Farm 1", "F001");
        db.Suppliers.Add(supplier);
        db.Farms.Add(farm);
        await db.SaveChangesAsync();

        var p1 = new Purchase(companyId, farm.Id, supplier.Id,
            new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero))
        {
            PurchaseNumber = "PO-001",
            Status = PurchaseStatus.Posted,
            Subtotal = 1000m,
            TaxTotal = 100m,
            GrandTotal = 1100m,
            AmountPaid = 500m,
            OutstandingAmount = 600m,
            Currency = Currency.USD
        };
        db.Purchases.Add(p1);
        await db.SaveChangesAsync();

        var bytes = await svc.ExportPurchasesCsvAsync(companyId, null, null, null, null, CancellationToken.None);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        var utf8 = new UTF8Encoding(true);
        var csv = utf8.GetString(bytes);

        Assert.Contains("PurchaseNumber,PurchaseDate,Status,Supplier,Farm", csv);
        Assert.Contains("PO-001", csv);
        Assert.Contains("Posted", csv);
        Assert.Contains("Sup Alpha", csv);
        Assert.Contains("Farm 1", csv);
        Assert.Contains("1100.00", csv);
    }

    [Fact]
    public async Task NetOperatingResultMonthToDate_EqualsRevenueMinusPurchasesMinusExpenses()
    {
        using var scope = CreateTestScope(new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero));
        var (db, svc, _, companyId, _) = scope;

        var farm = new Farm(companyId, "F1", "F001");
        var customer = new Customer(companyId, "CUST04", "C1");
        var supplier = new Supplier(companyId, "S1");
        db.Farms.Add(farm);
        db.Customers.Add(customer);
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        var inv1 = new Invoice(companyId, customer.Id,
            new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 25, 0, 0, 0, TimeSpan.Zero))
        {
            Status = InvoiceStatus.Paid,
            GrandTotal = 5000m,
            PaidAmount = 5000m
        };
        var inv2 = new Invoice(companyId, customer.Id,
            new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero))
        {
            Status = InvoiceStatus.Confirmed,
            GrandTotal = 3000m,
            PaidAmount = 0m
        };
        db.Invoices.AddRange(inv1, inv2);

        var purch = new Purchase(companyId, farm.Id, supplier.Id,
            new DateTimeOffset(2026, 8, 3, 0, 0, 0, TimeSpan.Zero))
        {
            Status = PurchaseStatus.Posted,
            GrandTotal = 2000m
        };
        db.Purchases.Add(purch);

        var exp1 = new Expense(companyId, ExpenseCategory.Feed,
            new DateTimeOffset(2026, 8, 7, 0, 0, 0, TimeSpan.Zero), 800m)
        {
            TaxRate = 0m, TaxAmount = 0m, Total = 800m
        };
        var exp2 = new Expense(companyId, ExpenseCategory.Labor,
            new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero), 700m)
        {
            TaxRate = 0m, TaxAmount = 0m, Total = 700m
        };
        db.Expenses.AddRange(exp1, exp2);
        await db.SaveChangesAsync();

        var kpis = await svc.GetDashboardKpisAsync(companyId, CancellationToken.None);

        Assert.Equal(8000m, kpis.RevenueMonthToDate);
        Assert.Equal(2000m, kpis.PurchasesMonthToDateAmount);
        Assert.Equal(1500m, kpis.ExpensesMonthToDateAmount);
        Assert.Equal(4500m, kpis.NetOperatingResultMonthToDate);
    }

    [Fact]
    public async Task OverdueAmountOnlyAggregatesDueDatePassed()
    {
        using var scope = CreateTestScope(new DateTimeOffset(2026, 8, 15, 10, 0, 0, TimeSpan.Zero));
        var (db, svc, _, companyId, clock) = scope;
        var now = clock.Now;

        var customer = new Customer(companyId, "CUST05", "C1");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var overdue = new Invoice(companyId, customer.Id,
            now.AddDays(-20),
            now.AddDays(-5))
        {
            Status = InvoiceStatus.Overdue,
            GrandTotal = 1000m,
            PaidAmount = 300m
        };

        var notDueYet = new Invoice(companyId, customer.Id,
            now.AddDays(-5),
            now.AddDays(10))
        {
            Status = InvoiceStatus.Confirmed,
            GrandTotal = 2000m,
            PaidAmount = 0m
        };

        var dueToday = new Invoice(companyId, customer.Id,
            now.AddDays(-30),
            now)
        {
            Status = InvoiceStatus.Confirmed,
            GrandTotal = 500m,
            PaidAmount = 0m
        };

        db.Invoices.AddRange(overdue, notDueYet, dueToday);
        await db.SaveChangesAsync();

        var kpis = await svc.GetDashboardKpisAsync(companyId, CancellationToken.None);

        Assert.Equal(700m, kpis.OverdueInvoicesAmountTotal);
        Assert.Equal(3200m, kpis.OutstandingInvoicesAmountTotal);
    }

    [Fact]
    public async Task MortalityRate_UsesLast30DaysDischarges()
    {
        using var scope = CreateTestScope(new DateTimeOffset(2026, 8, 15, 10, 0, 0, TimeSpan.Zero));
        var (db, svc, _, companyId, clock) = scope;
        var now = clock.Now;

        var farm = new Farm(companyId, "F1", "F001");
        db.Farms.Add(farm);
        await db.SaveChangesAsync();

        var active1 = new Livestock(companyId, "A1", LivestockType.PurchasedCastratedRam,
            now.AddDays(-60), 50m, WeightUnit.Kg, 500m, farm.Id);

        var dead30d1 = new Livestock(companyId, "D1", LivestockType.PurchasedEwe,
            now.AddDays(-45), 40m, WeightUnit.Kg, 300m, farm.Id);
        dead30d1.Discharge(DischargeCondition.Deceased, now.AddDays(-10));

        var dead30d2 = new Livestock(companyId, "D2", LivestockType.PurchasedCastratedRam,
            now.AddDays(-50), 50m, WeightUnit.Kg, 400m, farm.Id);
        dead30d2.Discharge(DischargeCondition.Deceased, now.AddDays(-20));

        var deadOld = new Livestock(companyId, "D3", LivestockType.PurchasedEwe,
            now.AddDays(-100), 40m, WeightUnit.Kg, 300m, farm.Id);
        deadOld.Discharge(DischargeCondition.Deceased, now.AddDays(-60));

        var sold30d = new Livestock(companyId, "S1", LivestockType.PurchasedCastratedRam,
            now.AddDays(-40), 50m, WeightUnit.Kg, 500m, farm.Id);
        sold30d.Discharge(DischargeCondition.Sold, now.AddDays(-5), 800m);

        db.Livestock.AddRange(active1, dead30d1, dead30d2, deadOld, sold30d);
        await db.SaveChangesAsync();

        var kpis = await svc.GetDashboardKpisAsync(companyId, CancellationToken.None);

        Assert.NotNull(kpis.LivestockMortalityRate30dPct);
        Assert.True(kpis.LivestockMortalityRate30dPct > 0);
    }
}

public sealed class ReportTestScope : IDisposable
{
    public AppDbContext Db { get; }
    public ReportService ReportService { get; }
    public ExpenseService ExpenseService { get; }
    public Guid CompanyId { get; }
    public TestDashboardClock Clock { get; }

    public ReportTestScope(AppDbContext db, ReportService reportService, ExpenseService expenseService, Guid companyId, TestDashboardClock clock)
    {
        Db = db;
        ReportService = reportService;
        ExpenseService = expenseService;
        CompanyId = companyId;
        Clock = clock;
    }

    public void Deconstruct(out AppDbContext db, out ReportService reportService, out ExpenseService expenseService, out Guid companyId, out TestDashboardClock clock)
    {
        db = Db;
        reportService = ReportService;
        expenseService = ExpenseService;
        companyId = CompanyId;
        clock = Clock;
    }

    public void Dispose()
    {
        Db.Dispose();
    }
}

public class TestDashboardClock : IDateTime
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
}
