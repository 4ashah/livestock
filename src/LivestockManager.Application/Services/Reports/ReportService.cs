using System.Reflection;
using System.Text;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Reports;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LivestockManager.Application.Services.Reports;

public class ReportService : IReportService
{
    private readonly IAppDbContext _db;
    private readonly IDateTime _dateTime;

    public ReportService(
        IAppDbContext db,
        IDateTime dateTime)
    {
        _db = db;
        _dateTime = dateTime;
    }

    public async Task<IList<LivestockProfitabilityReportRowDto>> LivestockProfitabilityAsync(Guid companyId, Guid? farmId, CancellationToken ct)
    {
        var query = _db.Livestock.Where(l => l.CompanyId == companyId);
        if (farmId.HasValue)
            query = query.Where(l => l.FarmId == farmId.Value);

        var livestockList = await query.ToListAsync(ct);
        var result = new List<LivestockProfitabilityReportRowDto>();

        foreach (var l in livestockList)
        {
            result.Add(new LivestockProfitabilityReportRowDto
            {
                LivestockId = l.Id,
                LivestockDisplayId = l.LivestockId,
                Type = l.LivestockTypeId,
                AcquisitionDate = l.AcquisitionDate,
                PurchaseAmount = l.PurchaseAmount,
                SoldAmount = l.SoldAmount,
                DirectExpenses = 0,
                BasicProfitLoss = l.BasicProfitLoss,
                CompleteProfitLoss = l.BasicProfitLoss,
                Status = l.Status
            });
        }

        return result;
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
}
