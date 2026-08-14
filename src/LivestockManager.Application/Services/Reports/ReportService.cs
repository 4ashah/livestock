using System.Reflection;
using System.Text;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Dashboard;
using LivestockManager.Application.DTOs.Reports;
using LivestockManager.Application.Services.Expenses;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LivestockManager.Application.Services.Reports;

public class ReportService : IReportService
{
    private readonly IAppDbContext _db;
    private readonly IDateTime _dateTime;
    private readonly IExpenseService _expenseService;

    public ReportService(
        IAppDbContext db,
        IDateTime dateTime,
        IExpenseService expenseService)
    {
        _db = db;
        _dateTime = dateTime;
        _expenseService = expenseService;
    }

    public async Task<DashboardKpisDto> GetDashboardKpisAsync(Guid companyId, CancellationToken ct)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, ct)
            ?? throw new InvalidOperationException($"Company {companyId} not found.");

        var now = _dateTime.Now;
        var today = now.Date;
        var monthStart = new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var thirtyDaysAgo = now.AddDays(-30);

        var activeLivestockCount = await _db.Livestock
            .CountAsync(l => l.CompanyId == companyId && l.Status == LivestockStatus.Active, ct);

        var newRegistrationsThisMonth = await _db.Livestock
            .CountAsync(l => l.CompanyId == companyId && l.AcquisitionDate >= monthStart, ct);

        var dischargesToday = await _db.Livestock
            .CountAsync(l => l.CompanyId == companyId
                && l.DischargeDate.HasValue
                && l.DischargeDate.Value.Date == today, ct);

        var activeLivestockForWeightGain = await _db.Livestock
            .Where(l => l.CompanyId == companyId
                && l.Status == LivestockStatus.Active
                && l.AcquisitionDate >= monthStart
                && l.CurrentWeight.HasValue)
            .ToListAsync(ct);

        decimal? avgDailyWeightGain = null;
        if (activeLivestockForWeightGain.Count > 0)
        {
            var totalAdg = 0m;
            var count = 0;
            foreach (var l in activeLivestockForWeightGain)
            {
                var daysOwned = (int)Math.Max(1, Math.Floor((now - l.AcquisitionDate).TotalDays));
                var weightGain = (l.CurrentWeight ?? 0) - l.InitialWeight;
                if (daysOwned > 0 && weightGain >= 0)
                {
                    totalAdg += weightGain / daysOwned;
                    count++;
                }
            }
            if (count > 0)
                avgDailyWeightGain = Math.Round(totalAdg / count, 4);
        }

        var livestockLast30d = await _db.Livestock
            .Where(l => l.CompanyId == companyId
                && (l.Status == LivestockStatus.Active
                    || (l.DischargeDate.HasValue && l.DischargeDate.Value >= thirtyDaysAgo)))
            .ToListAsync(ct);

        var dischargedDead30d = livestockLast30d.Count(l =>
            l.Status == LivestockStatus.DischargedDeceased
            && l.DischargeDate.HasValue
            && l.DischargeDate.Value >= thirtyDaysAgo);

        decimal? mortalityRate30dPct = null;
        if (livestockLast30d.Count > 0)
        {
            mortalityRate30dPct = Math.Round((decimal)dischargedDead30d / livestockLast30d.Count * 100, 2);
        }

        var sevenDaysAgo = now.AddDays(-7);
        var pendingWeighingsCount = await _db.Livestock
            .CountAsync(l => l.CompanyId == companyId
                && l.Status == LivestockStatus.Active
                && (!l.CurrentWeightDate.HasValue || l.CurrentWeightDate.Value < sevenDaysAgo), ct);

        var farmsActive = await _db.Farms
            .CountAsync(f => f.CompanyId == companyId && f.IsActive, ct);

        var revenueStatuses = new[]
        {
            InvoiceStatus.Confirmed,
            InvoiceStatus.Unpaid,
            InvoiceStatus.PartiallyPaid,
            InvoiceStatus.Paid,
            InvoiceStatus.Overdue
        };

        var revenueMonthToDate = await _db.Invoices
            .Where(i => i.CompanyId == companyId
                && i.InvoiceDate >= monthStart
                && revenueStatuses.Contains(i.Status))
            .SumAsync(i => (decimal?)i.GrandTotal ?? 0, ct);

        var paidReceiptsMonthToDate = await _db.Payments
            .Where(p => p.CompanyId == companyId
                && !p.IsReversed
                && p.PaymentDate >= monthStart)
            .SumAsync(p => (decimal?)p.Amount ?? 0, ct);

        var outstandingInvoicesTotal = await _db.Invoices
            .Where(i => i.CompanyId == companyId
                && i.Status != InvoiceStatus.Cancelled
                && i.Status != InvoiceStatus.Voided
                && i.GrandTotal - i.PaidAmount > 0.0001m)
            .SumAsync(i => (decimal?)(i.GrandTotal - i.PaidAmount) ?? 0, ct);

        var overdueInvoicesTotal = await _db.Invoices
            .Where(i => i.CompanyId == companyId
                && i.Status != InvoiceStatus.Cancelled
                && i.Status != InvoiceStatus.Voided
                && i.DueDate < now
                && i.GrandTotal - i.PaidAmount > 0.0001m)
            .SumAsync(i => (decimal?)(i.GrandTotal - i.PaidAmount) ?? 0, ct);

        var purchasesMonthToDateAmount = await _db.Purchases
            .Where(p => p.CompanyId == companyId
                && p.Status == PurchaseStatus.Posted
                && p.PurchaseDate >= monthStart)
            .SumAsync(p => (decimal?)p.GrandTotal ?? 0, ct);

        var expensesMonthToDateAmount = await _db.Expenses
            .Where(e => e.CompanyId == companyId
                && !e.IsDeleted
                && e.ExpenseDate >= monthStart)
            .SumAsync(e => (decimal?)e.Total ?? 0, ct);

        var netOperatingResult = revenueMonthToDate - (purchasesMonthToDateAmount + expensesMonthToDateAmount);

        return new DashboardKpisDto
        {
            TodayUtc = now,
            CompanyName = company.Name,
            CompanyCurrency = company.Currency,

            ActiveLivestockCount = activeLivestockCount,
            NewRegistrationsThisMonth = newRegistrationsThisMonth,
            DischargesToday = dischargesToday,
            AverageDailyWeightGainKgCurrentMonth = avgDailyWeightGain,
            LivestockMortalityRate30dPct = mortalityRate30dPct,
            PendingWeighingsCount = pendingWeighingsCount,
            FarmsActive = farmsActive,

            RevenueMonthToDate = revenueMonthToDate,
            PaidReceiptsMonthToDate = paidReceiptsMonthToDate,
            OutstandingInvoicesAmountTotal = outstandingInvoicesTotal,
            OverdueInvoicesAmountTotal = overdueInvoicesTotal,
            PurchasesMonthToDateAmount = purchasesMonthToDateAmount,
            ExpensesMonthToDateAmount = expensesMonthToDateAmount,
            NetOperatingResultMonthToDate = netOperatingResult
        };
    }

    public async Task<IList<LivestockProfitabilityReportRowDto>> CompleteLivestockProfitabilityAsync(
        Guid companyId,
        Guid? farmId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        CancellationToken ct)
    {
        var query = _db.Livestock.Where(l => l.CompanyId == companyId);
        if (farmId.HasValue)
            query = query.Where(l => l.FarmId == farmId.Value);

        var soldLivestock = await query
            .Where(l => l.Status == LivestockStatus.DischargedSold)
            .Include(l => l.Farm)
            .Include(l => l.SaleItem!)
                .ThenInclude(si => si.Sale!)
                .ThenInclude(s => s.Farm!)
            .AsNoTracking()
            .ToListAsync(ct);

        var soldLivestockIds = soldLivestock.Select(l => l.Id).ToList();

        var linkedSaleItemIds = soldLivestock
            .Where(l => l.SaleItemId.HasValue)
            .Select(l => l.SaleItemId.GetValueOrDefault())
            .ToList();

        var excludedInvoices = new List<Guid>();
        if (linkedSaleItemIds.Count > 0)
        {
            var saleIds = await _db.SaleItems
                .Where(si => linkedSaleItemIds.Contains(si.Id))
                .Select(si => si.SaleId)
                .Distinct()
                .ToListAsync(ct);

            if (saleIds.Count > 0)
            {
                excludedInvoices = await _db.Invoices
                    .Where(i => i.SaleId.HasValue
                        && saleIds.Contains(i.SaleId.GetValueOrDefault())
                        && (i.Status == InvoiceStatus.Cancelled || i.Status == InvoiceStatus.Voided))
                    .Select(i => i.SaleId.GetValueOrDefault())
                    .Distinct()
                    .ToListAsync(ct);
            }
        }

        var validSoldLivestock = soldLivestock;
        if (linkedSaleItemIds.Count > 0 && excludedInvoices.Count > 0)
        {
            var validSaleItemIds = await _db.SaleItems
                .Where(si => linkedSaleItemIds.Contains(si.Id)
                    && !excludedInvoices.Contains(si.SaleId))
                .Select(si => si.Id)
                .ToListAsync(ct);

            validSoldLivestock = soldLivestock
                .Where(l => !l.SaleItemId.HasValue || validSaleItemIds.Contains(l.SaleItemId.Value))
                .ToList();
        }

        if (fromDate.HasValue)
            validSoldLivestock = validSoldLivestock
                .Where(l => l.DischargeDate.HasValue && l.DischargeDate.Value >= fromDate.Value)
                .ToList();

        if (toDate.HasValue)
            validSoldLivestock = validSoldLivestock
                .Where(l => l.DischargeDate.HasValue && l.DischargeDate.Value <= toDate.Value)
                .ToList();

        var validLivestockIds = validSoldLivestock.Select(l => l.Id).ToList();

        var directExpensesByLivestock = await _db.Expenses
            .Where(e => e.CompanyId == companyId
                && !e.IsDeleted
                && e.LivestockId.HasValue
                && validLivestockIds.Contains(e.LivestockId.GetValueOrDefault()))
            .GroupBy(e => e.LivestockId.GetValueOrDefault())
            .Select(g => new { LivestockId = g.Key, Total = g.Sum(e => e.Total) })
            .ToDictionaryAsync(g => g.LivestockId, g => g.Total, ct);

        var result = new List<LivestockProfitabilityReportRowDto>();
        foreach (var l in validSoldLivestock)
        {
            var directExpenses = directExpensesByLivestock.TryGetValue(l.Id, out var de) ? de : 0m;
            var saleAmount = l.SoldAmount ?? 0m;
            var basicProfit = saleAmount - l.PurchaseAmount;

            var additionalAcquisitionCosts = l.AllocatedCommission + l.AllocatedTax + l.AllocatedTransportation + l.AllocatedOtherCost;
            var sale = l.SaleItem?.Sale;
            var saleFarm = sale?.Farm ?? l.Farm;
            var saleFarmId = sale?.FarmId ?? l.FarmId;
            var saleFarmName = saleFarm?.Name;
            var saleDate = sale?.Date ?? l.DischargeDate;
            var additionalSaleCosts = sale?.TotalAdditionalSaleCosts ?? 0m;
            var netSaleProceeds = l.SaleItem != null ? l.SaleItem.NetSaleProceeds : (sale?.NetSaleProceeds ?? saleAmount);
            var totalAcquisitionCost = l.TotalAcquisitionCost != 0m
                ? l.TotalAcquisitionCost
                : l.PurchaseAmount + additionalAcquisitionCosts;
            var completeProfit = netSaleProceeds - totalAcquisitionCost - directExpenses;

            result.Add(new LivestockProfitabilityReportRowDto
            {
                LivestockId = l.Id,
                LivestockDisplayId = l.LivestockId,
                Type = l.LivestockTypeId,
                AcquisitionDate = l.AcquisitionDate,
                PurchaseAmount = l.PurchaseAmount,
                SoldAmount = saleAmount,
                DirectExpenses = directExpenses,
                BasicProfitLoss = basicProfit,
                CompleteProfitLoss = completeProfit,
                Status = l.Status,
                SaleFarmId = saleFarmId,
                SaleFarmName = saleFarmName,
                SaleDate = saleDate,
                AdditionalAcquisitionCosts = additionalAcquisitionCosts,
                TotalAcquisitionCosts = totalAcquisitionCost,
                AdditionalSaleCosts = additionalSaleCosts,
                NetSaleProceeds = netSaleProceeds
            });
        }

        return result;
    }

    public async Task<IList<FarmProfitabilityReportRowDto>> CompleteFarmProfitabilityAsync(
        Guid companyId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        CancellationToken ct)
    {
        var farms = await _db.Farms
            .Where(f => f.CompanyId == companyId)
            .ToListAsync(ct);

        var result = new List<FarmProfitabilityReportRowDto>();
        foreach (var farm in farms)
        {
            var activeLivestockCount = await _db.Livestock.CountAsync(l =>
                l.FarmId == farm.Id && l.Status == LivestockStatus.Active, ct);

            var livestockForFarm = await _db.Livestock
                .Where(l => l.FarmId == farm.Id)
                .Select(l => new { l.Id, l.Status, l.DischargeDate, l.SoldAmount, l.PurchaseAmount })
                .ToListAsync(ct);

            var soldLivestock = livestockForFarm
                .Where(l => l.Status == LivestockStatus.DischargedSold)
                .ToList();

            if (fromDate.HasValue)
                soldLivestock = soldLivestock
                    .Where(l => l.DischargeDate.HasValue && l.DischargeDate.Value >= fromDate.Value)
                    .ToList();

            if (toDate.HasValue)
                soldLivestock = soldLivestock
                    .Where(l => l.DischargeDate.HasValue && l.DischargeDate.Value <= toDate.Value)
                    .ToList();

            var soldIds = soldLivestock.Select(l => l.Id).ToList();

            var totalSales = soldLivestock.Sum(l => l.SoldAmount ?? 0m);
            var totalPurchases = soldLivestock.Sum(l => l.PurchaseAmount);

            var totalExpenses = 0m;
            if (soldIds.Count > 0)
            {
                totalExpenses = await _db.Expenses
                    .Where(e => e.CompanyId == companyId
                        && !e.IsDeleted
                        && e.LivestockId.HasValue
                        && soldIds.Contains(e.LivestockId.Value))
                    .SumAsync(e => (decimal?)e.Total ?? 0, ct);
            }

            result.Add(new FarmProfitabilityReportRowDto
            {
                FarmId = farm.Id,
                FarmName = farm.Name,
                ActiveLivestockCount = activeLivestockCount,
                TotalPurchases = totalPurchases,
                TotalSales = totalSales,
                TotalExpenses = totalExpenses,
                NetProfit = totalSales - totalPurchases - totalExpenses
            });
        }

        return result;
    }

    public async Task<IList<OutstandingInvoiceRowDto>> ReceivableAgingAsync(Guid companyId, CancellationToken ct)
    {
        var now = _dateTime.Now;
        var query = from i in _db.Invoices
                    join c in _db.Customers on i.CustomerId equals c.Id
                    where i.CompanyId == companyId
                          && i.GrandTotal - i.PaidAmount > 0.0001m
                          && i.Status != InvoiceStatus.Cancelled
                          && i.Status != InvoiceStatus.Voided
                    select new OutstandingInvoiceRowDto
                    {
                        InvoiceId = i.Id,
                        InvoiceNumber = i.InvoiceNumber ?? i.Id.ToString(),
                        CustomerId = i.CustomerId,
                        CustomerName = c.Name,
                        InvoiceDate = i.InvoiceDate,
                        DueDate = i.DueDate,
                        DaysOverdue = (int)Math.Floor((now - i.DueDate).TotalDays),
                        GrandTotal = i.GrandTotal,
                        OutstandingAmount = i.GrandTotal - i.PaidAmount
                    };

        return await query.ToListAsync(ct);
    }

    public async Task<byte[]> ExportPurchasesCsvAsync(
        Guid companyId,
        Guid? supplierId,
        PurchaseStatus? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct)
    {
        var query = from p in _db.Purchases
                    join s in _db.Suppliers on p.SupplierId equals s.Id into ss
                    from s in ss.DefaultIfEmpty()
                    join f in _db.Farms on p.FarmId equals f.Id into ff
                    from f in ff.DefaultIfEmpty()
                    where p.CompanyId == companyId
                    select new { p, s, f };

        if (supplierId.HasValue)
            query = query.Where(x => x.p.SupplierId == supplierId.Value);

        if (status.HasValue)
            query = query.Where(x => x.p.Status == status.Value);

        if (from.HasValue)
            query = query.Where(x => x.p.PurchaseDate >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.p.PurchaseDate <= to.Value);

        var rows = await query
            .Select(x => new PurchaseCsvRow
            {
                PurchaseNumber = x.p.PurchaseNumber ?? x.p.Id.ToString(),
                PurchaseDate = x.p.PurchaseDate.ToString("yyyy-MM-dd"),
                Status = x.p.Status.ToString(),
                Supplier = x.s != null ? x.s.Name : string.Empty,
                Farm = x.f != null ? x.f.Name : string.Empty,
                Subtotal = x.p.Subtotal.ToString("F2"),
                TaxTotal = x.p.TaxTotal.ToString("F2"),
                GrandTotal = x.p.GrandTotal.ToString("F2"),
                AmountPaid = x.p.AmountPaid.ToString("F2"),
                Outstanding = x.p.OutstandingAmount.ToString("F2"),
                Currency = x.p.Currency.ToString()
            })
            .ToListAsync(ct);

        var columns = new[]
        {
            "PurchaseNumber", "PurchaseDate", "Status", "Supplier", "Farm",
            "Subtotal", "TaxTotal", "GrandTotal", "AmountPaid", "Outstanding", "Currency"
        };

        return CsvExporter.Write(rows, columns);
    }

    public Task<byte[]> ExportExpensesCsvAsync(
        Guid companyId,
        Guid? farmId,
        Guid? supplierId,
        Guid? livestockId,
        ExpenseCategory? category,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct)
    {
        return _expenseService.ExportCsvAsync(companyId, farmId, supplierId, livestockId, category, from, to, ct);
    }

    public async Task<IList<LivestockProfitabilityReportRowDto>> LivestockProfitabilityAsync(Guid companyId, Guid? farmId, CancellationToken ct)
    {
        return await CompleteLivestockProfitabilityAsync(companyId, farmId, null, null, ct);
    }

    public async Task<IList<FarmProfitabilityReportRowDto>> FarmProfitabilityAsync(Guid companyId, CancellationToken ct)
    {
        var farms = await _db.Farms.Where(f => f.CompanyId == companyId).ToListAsync(ct);
        var result = new List<FarmProfitabilityReportRowDto>();

        foreach (var farm in farms)
        {
            var activeLivestockCount = await _db.Livestock.CountAsync(l => l.FarmId == farm.Id && l.Status == LivestockStatus.Active, ct);

            var livestockIdsForFarm = await _db.Livestock.Where(l => l.FarmId == farm.Id).Select(l => l.Id).ToListAsync(ct);
            var totalSales = await (from s in _db.Sales
                              join si in _db.SaleItems on s.Id equals si.SaleId
                              where s.CompanyId == companyId && si.LivestockId.HasValue && livestockIdsForFarm.Contains(si.LivestockId.Value)
                              select si.LineTotal).SumAsync(ct);

            result.Add(new FarmProfitabilityReportRowDto
            {
                FarmId = farm.Id,
                FarmName = farm.Name,
                ActiveLivestockCount = activeLivestockCount,
                TotalPurchases = 0,
                TotalSales = totalSales,
                TotalExpenses = 0,
                NetProfit = totalSales
            });
        }

        return result;
    }

    public async Task<IList<OutstandingInvoiceRowDto>> OutstandingInvoicesAsync(Guid companyId, CancellationToken ct)
    {
        var now = _dateTime.Now;
        var query = from i in _db.Invoices
                    join c in _db.Customers on i.CustomerId equals c.Id
                    where i.CompanyId == companyId
                          && i.GrandTotal - i.PaidAmount > 0.0001m
                          && i.DueDate < now
                          && i.Status != InvoiceStatus.Cancelled
                          && i.Status != InvoiceStatus.Voided
                          && i.Status != InvoiceStatus.Paid
                    select new OutstandingInvoiceRowDto
                    {
                        InvoiceId = i.Id,
                        InvoiceNumber = i.InvoiceNumber ?? i.Id.ToString(),
                        CustomerId = i.CustomerId,
                        CustomerName = c.Name,
                        InvoiceDate = i.InvoiceDate,
                        DueDate = i.DueDate,
                        DaysOverdue = (int)Math.Floor((now - i.DueDate).TotalDays),
                        GrandTotal = i.GrandTotal,
                        OutstandingAmount = i.GrandTotal - i.PaidAmount
                    };

        return await query.ToListAsync(ct);
    }

    public async Task<IList<(LivestockType Type, int Count)>> ActiveLivestockByTypeAsync(Guid companyId, Guid? farmId, CancellationToken ct)
    {
        var query = _db.Livestock.Where(l => l.CompanyId == companyId && l.Status == LivestockStatus.Active);
        if (farmId.HasValue)
            query = query.Where(l => l.FarmId == farmId.Value);

        var rows = await query
            .GroupBy(l => l.LivestockTypeId)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows.Select(g => (g.Type, g.Count)).ToList();
    }

    public async Task<IList<(DateTimeOffset WeekStart, decimal Weight)>> WeeklyWeightsAsync(Guid livestockId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var fromDate = from;
        var toDate = to;
        var weights = await _db.LivestockWeights
            .Where(w => w.LivestockId == livestockId && w.WeighedAt >= fromDate && w.WeighedAt <= toDate)
            .OrderBy(w => w.WeighedAt)
            .ToListAsync(ct);

        var result = new List<(DateTimeOffset WeekStart, decimal Weight)>();
        foreach (var w in weights)
        {
            var weekStart = new DateTimeOffset(w.WeighedAt.Date.AddDays(-(int)w.WeighedAt.DayOfWeek), TimeSpan.Zero);
            var existing = result.LastOrDefault(r => r.WeekStart.Date == weekStart.Date);
            if (existing.Weight == 0 && existing.WeekStart == default)
            {
                result.Add((weekStart, w.Weight));
            }
        }

        return result;
    }

    public async Task<IList<WeightChangeRowDto>> WeightChangesAsync(Guid companyId, Guid? farmId, CancellationToken ct)
    {
        var query = _db.Livestock.Where(l => l.CompanyId == companyId && l.CurrentWeight.HasValue);
        if (farmId.HasValue)
            query = query.Where(l => l.FarmId == farmId.Value);

        var livestockList = await query.ToListAsync(ct);
        var result = new List<WeightChangeRowDto>();

        foreach (var l in livestockList)
        {
            var daysOwned = (int)Math.Max(1, Math.Floor((_dateTime.Now - l.AcquisitionDate).TotalDays));
            var weightGain = (l.CurrentWeight ?? 0) - l.InitialWeight;
            var weightGainPercent = l.InitialWeight > 0 ? (weightGain / l.InitialWeight) * 100 : 0;
            var avgDailyGain = daysOwned > 0 ? weightGain / daysOwned : 0;

            result.Add(new WeightChangeRowDto
            {
                LivestockId = l.Id,
                LivestockDisplayId = l.LivestockId,
                Type = l.LivestockTypeId,
                InitialWeight = l.InitialWeight,
                CurrentWeight = l.CurrentWeight ?? 0,
                WeightGain = weightGain,
                WeightGainPercent = weightGainPercent,
                DaysOwned = daysOwned,
                AverageDailyGain = avgDailyGain
            });
        }

        return result;
    }

    public async Task<IList<(Guid? FarmId, string? FarmName, int Count)>> MortalityAsync(Guid companyId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var fromDate = from;
        var toDate = to;
        var deceased = await _db.Livestock
            .Where(l => l.CompanyId == companyId
                        && l.Status == LivestockStatus.DischargedDeceased
                        && l.DischargeDate >= fromDate
                        && l.DischargeDate <= toDate)
            .ToListAsync(ct);

        return deceased
            .GroupBy(l => new { l.FarmId })
            .Select(g => (g.Key.FarmId, (string?)null, g.Count()))
            .ToList();
    }

    public async Task<IList<(Guid? FarmId, string? FarmName, int Count, decimal TotalValue)>> DischargesAsync(
        Guid companyId, DateTimeOffset from, DateTimeOffset to, DischargeCondition? condition, CancellationToken ct)
    {
        var fromDate = from;
        var toDate = to;
        var query = _db.Livestock.Where(l =>
            l.CompanyId == companyId &&
            l.DischargeDate >= fromDate &&
            l.DischargeDate <= toDate &&
            l.Status != LivestockStatus.Active);

        if (condition.HasValue)
        {
            query = query.Where(l => l.DischargeCondition == condition.Value);
        }

        var discharged = await query.ToListAsync(ct);

        return discharged
            .GroupBy(l => new { l.FarmId })
            .Select(g => (
                g.Key.FarmId,
                (string?)null,
                g.Count(),
                TotalValue: g.Sum(l => l.SoldAmount ?? 0)
            ))
            .ToList();
    }

    public async Task<IList<(Guid CustomerId, string CustomerName, decimal Balance)>> OutstandingBalancesAsync(Guid companyId, CancellationToken ct)
    {
        var invoices = await _db.Invoices
            .Where(i => i.CompanyId == companyId
                        && i.Status != InvoiceStatus.Cancelled
                        && i.Status != InvoiceStatus.Voided
                        && i.GrandTotal - i.PaidAmount > 0.0001m)
            .GroupBy(i => new { i.CustomerId })
            .Select(g => new { CustomerId = g.Key.CustomerId, Balance = g.Sum(i => i.GrandTotal - i.PaidAmount) })
            .ToListAsync(ct);

        var customerIds = invoices.Select(x => x.CustomerId).Distinct().ToList();
        var customers = await _db.Customers
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var result = new List<(Guid CustomerId, string CustomerName, decimal Balance)>();
        foreach (var inv in invoices)
        {
            var name = customers.ContainsKey(inv.CustomerId) ? customers[inv.CustomerId] : "(Unknown)";
            result.Add((inv.CustomerId, name, inv.Balance));
        }
        return result;
    }

    public Task<byte[]> ExportCsv<T>(IEnumerable<T> rows)
    {
        var sb = new StringBuilder();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        sb.AppendLine(string.Join(",", properties.Select(p => p.Name)));

        foreach (var row in rows)
        {
            var values = properties.Select(p =>
            {
                var val = p.GetValue(row);
                var str = val?.ToString() ?? string.Empty;
                if (str.Contains(',') || str.Contains('"') || str.Contains('\n'))
                {
                    str = "\"" + str.Replace("\"", "\"\"") + "\"";
                }
                return str;
            });
            sb.AppendLine(string.Join(",", values));
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    public async Task<ProfitLossReportDto> ProfitLossAsync(Guid companyId, Guid? farmId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var result = new ProfitLossReportDto
        {
            FromDate = from,
            ToDate = to
        };

        var authorizedFarmIds = await _db.Farms
            .Where(f => f.CompanyId == companyId)
            .Select(f => f.Id)
            .ToListAsync(ct);

        if (farmId.HasValue && !authorizedFarmIds.Contains(farmId.Value))
        {
            return result;
        }

        SaleStatus[] validSaleStatuses = [SaleStatus.Confirmed, SaleStatus.Completed];
        var salesQuery = _db.Sales
            .Where(s => s.CompanyId == companyId
                && s.Date >= from
                && s.Date <= to
                && validSaleStatuses.Contains(s.Status));

        if (farmId.HasValue)
            salesQuery = salesQuery.Where(s => s.FarmId == farmId.Value);

        var salesWithinPeriod = await salesQuery
            .Select(s => new { s.Id, s.Date, s.GrandTotal })
            .ToListAsync(ct);

        result.TotalSales = salesWithinPeriod.Sum(s => s.GrandTotal);
        result.SalesCount = salesWithinPeriod.Count;

        var saleIds = salesWithinPeriod.Select(s => s.Id).ToList();
        if (saleIds.Count > 0)
        {
            result.SoldHead = await _db.SaleItems
                .Where(si => saleIds.Contains(si.SaleId) && si.LivestockId.HasValue)
                .CountAsync(ct);
        }

        var purchasesQuery = _db.Purchases
            .Where(p => p.CompanyId == companyId
                && p.PurchaseDate >= from
                && p.PurchaseDate <= to
                && p.Status == PurchaseStatus.Posted);

        if (farmId.HasValue)
            purchasesQuery = purchasesQuery.Where(p => p.FarmId == farmId.Value);

        var purchasesWithinPeriod = await purchasesQuery
            .Select(p => new { p.Id, p.GrandTotal })
            .ToListAsync(ct);

        result.TotalPurchases = purchasesWithinPeriod.Sum(p => p.GrandTotal);
        result.PurchaseCount = purchasesWithinPeriod.Count;

        var lossesQuery = _db.LivestockLosses
            .Where(l => l.CompanyId == companyId
                && l.LossDate >= from
                && l.LossDate <= to
                && !l.IsReversed);

        if (farmId.HasValue)
            lossesQuery = lossesQuery.Where(l => l.FarmId == farmId.Value);

        var lossesWithinPeriod = await lossesQuery
            .Select(l => new { l.Id, l.LossAmount })
            .ToListAsync(ct);

        result.TotalLosses = lossesWithinPeriod.Sum(l => l.LossAmount);
        result.LossCount = lossesWithinPeriod.Count;

        var dischargedLostQuery = _db.Livestock
            .Where(l =>
                l.CompanyId == companyId
                && (l.Status == LivestockStatus.DischargedLost
                    || l.Status == LivestockStatus.DischargedStolen
                    || l.Status == LivestockStatus.DischargedDeceased
                    || l.Status == LivestockStatus.DischargedOther)
                && l.DischargeDate.HasValue
                && l.DischargeDate.Value >= from
                && l.DischargeDate.Value <= to);

        if (farmId.HasValue)
            dischargedLostQuery = dischargedLostQuery.Where(l => l.FarmId == farmId.Value);

        var dischargedLostLivestock = await dischargedLostQuery.CountAsync(ct);

        result.LostHead = Math.Max(result.LossCount, dischargedLostLivestock);

        var expensesQuery = _db.Expenses
            .Where(e => e.CompanyId == companyId
                && !e.IsDeleted
                && e.ExpenseDate >= from
                && e.ExpenseDate <= to);

        if (farmId.HasValue)
            expensesQuery = expensesQuery.Where(e => e.FarmId == farmId.Value);

        var expensesWithinPeriod = await expensesQuery
            .Select(e => new { e.Id, e.Total })
            .ToListAsync(ct);

        result.TotalExpenses = expensesWithinPeriod.Sum(e => e.Total);
        result.ExpenseCount = expensesWithinPeriod.Count;

        result.GrossProfit = result.TotalSales - result.TotalPurchases;
        result.OperatingProfit = result.GrossProfit - result.TotalExpenses;
        result.NetProfit = result.OperatingProfit - result.TotalLosses;

        return result;
    }

    public async Task<IList<ActiveLivestockByTypeReportDto>> ActiveLivestockByTypeReportAsync(
        Guid companyId,
        Guid? farmId,
        CancellationToken ct)
    {
        var authorizedFarmIds = await _db.Farms
            .Where(f => f.CompanyId == companyId)
            .Select(f => f.Id)
            .ToListAsync(ct);

        if (farmId.HasValue && !authorizedFarmIds.Contains(farmId.Value))
        {
            return new List<ActiveLivestockByTypeReportDto>();
        }

        var query = _db.Livestock.Where(l => l.CompanyId == companyId && l.Status == LivestockStatus.Active);
        if (farmId.HasValue)
            query = query.Where(l => l.FarmId == farmId.Value);

        var rows = await query
            .AsNoTracking()
            .GroupBy(l => l.LivestockTypeId)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var totalCount = rows.Sum(r => r.Count);

        var result = new List<ActiveLivestockByTypeReportDto>();
        foreach (var row in rows)
        {
            result.Add(new ActiveLivestockByTypeReportDto
            {
                LivestockType = row.Type,
                TypeCode = row.Type.GetCode(),
                TypeLabel = row.Type.GetDisplayName(),
                Count = row.Count,
                PercentageOfTotal = totalCount > 0 ? Math.Round((decimal)row.Count / totalCount * 100, 2) : 0,
                CountFilteredTotalContext = totalCount
            });
        }

        return result;
    }

    public async Task<SalesByPeriodReportDto> SalesByPeriodReportAsync(
        Guid companyId,
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        Guid? farmId,
        Guid? customerId,
        string? status,
        CancellationToken ct)
    {
        var authorizedFarmIds = await _db.Farms
            .Where(f => f.CompanyId == companyId)
            .Select(f => f.Id)
            .ToListAsync(ct);

        if (farmId.HasValue && !authorizedFarmIds.Contains(farmId.Value))
        {
            return new SalesByPeriodReportDto
            {
                FromDate = fromDate,
                ToDate = toDate
            };
        }

        SaleStatus[] validSaleStatuses = [SaleStatus.Confirmed, SaleStatus.Completed];

        var salesQuery = _db.Sales
            .Where(s => s.CompanyId == companyId
                && s.Date >= fromDate
                && s.Date <= toDate
                && validSaleStatuses.Contains(s.Status));

        if (farmId.HasValue)
            salesQuery = salesQuery.Where(s => s.FarmId == farmId.Value);

        if (customerId.HasValue)
            salesQuery = salesQuery.Where(s => s.CustomerId == customerId.Value);

        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<SaleStatus>(status, true, out var requestedStatus)
            && (requestedStatus == SaleStatus.Confirmed || requestedStatus == SaleStatus.Completed))
        {
            salesQuery = salesQuery.Where(s => s.Status == requestedStatus);
        }

        var sales = await salesQuery
            .Include(s => s.Farm)
            .Include(s => s.Customer)
            .Include(s => s.Items)
            .AsNoTracking()
            .ToListAsync(ct);

        var saleIds = sales.Select(s => s.Id).ToList();

        var livestockCountBySale = new Dictionary<Guid, int>();
        if (saleIds.Count > 0)
        {
            livestockCountBySale = await _db.SaleItems
                .Where(si => saleIds.Contains(si.SaleId) && si.LivestockId.HasValue)
                .GroupBy(si => si.SaleId)
                .Select(g => new { SaleId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.SaleId, g => g.Count, ct);
        }

        var farmGroups = sales
            .GroupBy(s => new { s.FarmId, FarmName = s.Farm != null ? s.Farm.Name : "(Unassigned)" })
            .Select(g => new
            {
                g.Key.FarmId,
                g.Key.FarmName,
                NumberSold = g.Sum(s => livestockCountBySale.TryGetValue(s.Id, out var cnt) ? cnt : 0),
                GrossSalesAmount = g.Sum(s => s.GrandTotal),
                AdditionalSaleCosts = g.Sum(s => s.TotalAdditionalSaleCosts),
                NetSaleProceeds = g.Sum(s => s.NetSaleProceeds)
            })
            .ToList();

        var farmSummaries = farmGroups.Select(fg => new SalesByPeriodFarmSummaryDto
        {
            FarmId = fg.FarmId,
            FarmName = fg.FarmName,
            NumberSold = fg.NumberSold,
            GrossSalesAmount = fg.GrossSalesAmount,
            AdditionalSaleCosts = fg.AdditionalSaleCosts,
            NetSaleProceeds = fg.NetSaleProceeds
        }).ToList();

        var saleDetails = sales.Select(s => new SalesByPeriodSaleDetailDto
        {
            SaleDate = s.Date,
            SaleNumber = s.SaleNumber ?? s.Id.ToString(),
            FarmId = s.FarmId,
            FarmName = s.Farm != null ? s.Farm.Name : "(Unassigned)",
            CustomerName = s.Customer != null ? s.Customer.Name : "(Unknown)",
            NumberSold = livestockCountBySale.TryGetValue(s.Id, out var cnt) ? cnt : 0,
            GrossSaleAmount = s.GrandTotal,
            Status = s.Status,
            SaleId = s.Id
        }).ToList();

        return new SalesByPeriodReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalNumberSold = farmSummaries.Sum(f => f.NumberSold),
            TotalGrossSalesAmount = sales.Sum(s => s.GrandTotal),
            TotalAdditionalSaleCosts = sales.Sum(s => s.TotalAdditionalSaleCosts),
            TotalNetSaleProceeds = sales.Sum(s => s.NetSaleProceeds),
            FarmSummaries = farmSummaries,
            SaleDetails = saleDetails
        };
    }

    public async Task<IList<LivestockProfitabilityReportRowDto>> LivestockProfitabilityWithDatesAsync(
        Guid companyId,
        Guid? farmId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        CancellationToken ct)
    {
        return await CompleteLivestockProfitabilityAsync(companyId, farmId, fromDate, toDate, ct);
    }
}

public class PurchaseCsvRow
{
    public string PurchaseNumber { get; set; } = string.Empty;
    public string PurchaseDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public string Farm { get; set; } = string.Empty;
    public string Subtotal { get; set; } = string.Empty;
    public string TaxTotal { get; set; } = string.Empty;
    public string GrandTotal { get; set; } = string.Empty;
    public string AmountPaid { get; set; } = string.Empty;
    public string Outstanding { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
}
