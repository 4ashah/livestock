using Microsoft.EntityFrameworkCore;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.LivestockLosses;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Exceptions;

namespace LivestockManager.Application.Services.LivestockLosses;

public class LivestockLossService : ILivestockLossService
{
    private readonly IAppDbContext _db;
    private readonly IDateTime _dateTime;
    private readonly ISequenceGenerator _sequenceGenerator;

    public LivestockLossService(
        IAppDbContext db,
        IDateTime dateTime,
        ISequenceGenerator sequenceGenerator)
    {
        _db = db;
        _dateTime = dateTime;
        _sequenceGenerator = sequenceGenerator;
    }

    private static long ExtractNumericSuffix(string raw, string expectedPrefix)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 1;
        var prefix = expectedPrefix ?? string.Empty;
        var suffix = raw.StartsWith(prefix, StringComparison.Ordinal)
            ? raw[prefix.Length..] : raw;
        long result;
        if (long.TryParse(suffix, out result)) return result;
        var digits = new string(suffix.Where(char.IsDigit).ToArray());
        if (digits.Length > 0 && long.TryParse(digits, out result)) return result;
        return 1;
    }

    public async Task<IList<LivestockLossSummaryDto>> ListAsync(
        Guid companyId,
        Guid? farmId,
        Guid? livestockId,
        DischargeCondition? lossType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        bool includeReversed,
        CancellationToken ct)
    {
        var q = from loss in _db.LivestockLosses
                join l in _db.Livestock on loss.LivestockId equals l.Id into ll
                from ls in ll.DefaultIfEmpty()
                join f in _db.Farms on loss.FarmId equals f.Id into ff
                from f in ff.DefaultIfEmpty()
                where loss.CompanyId == companyId
                select new { loss, ls, f };

        if (farmId.HasValue)
            q = q.Where(x => x.loss.FarmId == farmId.Value);

        if (livestockId.HasValue)
            q = q.Where(x => x.loss.LivestockId == livestockId.Value);

        if (lossType.HasValue)
            q = q.Where(x => x.loss.LossType == lossType.Value);

        if (from.HasValue)
            q = q.Where(x => x.loss.LossDate >= from.Value);

        if (to.HasValue)
            q = q.Where(x => x.loss.LossDate <= to.Value);

        if (!includeReversed)
            q = q.Where(x => !x.loss.IsReversed);

        var list = await q
            .OrderByDescending(x => x.loss.LossDate)
            .ThenByDescending(x => x.loss.CreatedAt)
            .Select(x => new LivestockLossSummaryDto
            {
                Id = x.loss.Id,
                LossNumber = x.loss.LossNumber,
                LossDate = x.loss.LossDate,
                LossType = x.loss.LossType,
                FarmId = x.loss.FarmId,
                FarmName = x.f != null ? x.f.Name : null,
                LivestockId = x.loss.LivestockId,
                LivestockCode = x.ls != null ? x.ls.LivestockId : string.Empty,
                LivestockDisplayId = x.ls != null ? x.ls.LivestockId : null,
                LivestockType = x.ls != null ? x.ls.LivestockTypeId : 0,
                BookValue = x.loss.BookValue,
                SalvageValue = x.loss.SalvageValue,
                LossAmount = x.loss.LossAmount,
                Reason = x.loss.Reason,
                IsReversed = x.loss.IsReversed,
                CreatedAt = x.loss.CreatedAt
            })
            .ToListAsync(ct);

        return list;
    }

    public async Task<LivestockLossDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct)
    {
        var row = await (
            from loss in _db.LivestockLosses
            join ls in _db.Livestock on loss.LivestockId equals ls.Id into lsg
            from ls in lsg.DefaultIfEmpty()
            join f in _db.Farms on loss.FarmId equals f.Id into fg
            from f in fg.DefaultIfEmpty()
            where loss.Id == id && loss.CompanyId == companyId
            select new { loss, ls, f })
            .FirstOrDefaultAsync(ct);

        if (row == null)
            throw new DomainException($"Livestock loss {id} not found.");

        return new LivestockLossDetailDto
        {
            Id = row.loss.Id,
            CompanyId = row.loss.CompanyId,
            FarmId = row.loss.FarmId,
            FarmName = row.f != null ? row.f.Name : null,
            LivestockId = row.loss.LivestockId,
            LivestockCode = row.ls != null ? row.ls.LivestockId : null,
            LivestockDisplayId = row.ls != null ? row.ls.LivestockId : null,
            LivestockType = row.ls != null ? row.ls.LivestockTypeId : 0,
            LossNumber = row.loss.LossNumber,
            LossType = row.loss.LossType,
            LossDate = row.loss.LossDate,
            Currency = row.loss.Currency,
            BookValue = row.loss.BookValue,
            SalvageValue = row.loss.SalvageValue,
            LossAmount = row.loss.LossAmount,
            Reason = row.loss.Reason,
            Notes = row.loss.Notes,
            IsReversed = row.loss.IsReversed,
            ReversalReason = row.loss.ReversalReason,
            ReversedAt = row.loss.ReversedAt,
            CreatedAt = row.loss.CreatedAt,
            ModifiedAt = row.loss.ModifiedAt
        };
    }

    public async Task<LivestockLossDetailDto> CreateAsync(LivestockLossCreateDto dto, Guid companyId, CancellationToken ct)
    {
        using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            var resolvedCompanyId = companyId == Guid.Empty ? dto.CompanyId : companyId;
            if (resolvedCompanyId == Guid.Empty)
                throw new ArgumentException("CompanyId is required.", nameof(dto));
            if (dto.LivestockId == Guid.Empty)
                throw new ArgumentException("LivestockId is required.", nameof(dto));
            if (dto.LossType == DischargeCondition.Sold)
                throw new ArgumentException("LossType cannot be Sold. Use Sales module for sales.", nameof(dto));

            var livestock = await _db.Livestock
                .FirstOrDefaultAsync(l => l.Id == dto.LivestockId && l.CompanyId == resolvedCompanyId, ct);
            if (livestock == null)
                throw new DomainException($"Livestock {dto.LivestockId} not found.");
            if (livestock.Status != LivestockStatus.Active)
                throw new DomainException($"Livestock {livestock.LivestockId} is not Active (current: {livestock.Status}). Cannot record loss.");

            var lossDate = dto.LossDate == default ? _dateTime.Now : dto.LossDate;
            var year = lossDate.Year;
            var scopedKey = $"LOSS:{year}";
            var rawNumber = await _sequenceGenerator.GenerateDocumentNumberAsync(resolvedCompanyId, scopedKey, ct);
            var numericSuffix = ExtractNumericSuffix(rawNumber, scopedKey);
            var lossNumber = $"LOSS-{year}-{numericSuffix:D5}";

            var currency = dto.Currency ?? Currency.USD;
            var bookValue = dto.BookValue > 0 ? dto.BookValue : livestock.PurchaseAmount;
            var salvageValue = dto.SalvageValue;

            var loss = new LivestockLoss(
                resolvedCompanyId,
                livestock.Id,
                lossNumber,
                dto.LossType,
                lossDate,
                bookValue,
                salvageValue,
                dto.FarmId ?? livestock.FarmId)
            {
                Currency = currency,
                Reason = dto.Reason,
                Notes = dto.Notes
            };

            _db.LivestockLosses.Add(loss);

            livestock.Discharge(dto.LossType, lossDate, null, dto.Reason);
            livestock.ModifiedAt = _dateTime.Now;

            var activity = new LivestockActivity(livestock.Id, LivestockActivityType.Discharge, lossDate,
                $"Loss recorded: {dto.LossType}")
            {
                Metadata = $"LossId:{loss.Id};LossNumber:{lossNumber};BookValue:{bookValue};SalvageValue:{salvageValue};LossAmount:{loss.LossAmount}"
            };
            _db.LivestockActivities.Add(activity);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return await GetByIdAsync(loss.Id, resolvedCompanyId, ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<LivestockLossDetailDto> ReverseAsync(Guid id, string reason, Guid companyId, CancellationToken ct)
    {
        using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reversal reason is required.", nameof(reason));

            var loss = await _db.LivestockLosses
                .FirstOrDefaultAsync(l => l.Id == id && l.CompanyId == companyId, ct);
            if (loss == null)
                throw new DomainException($"Livestock loss {id} not found.");
            if (loss.IsReversed)
                throw new DomainException("Loss record has already been reversed.");

            loss.Reverse(reason);
            loss.ModifiedAt = _dateTime.Now;

            var livestock = await _db.Livestock
                .FirstOrDefaultAsync(l => l.Id == loss.LivestockId && l.CompanyId == companyId, ct);
            if (livestock != null && livestock.Status != LivestockStatus.Active)
            {
                livestock.Status = LivestockStatus.Active;
                livestock.DischargeDate = null;
                livestock.DischargeCondition = null;
                livestock.DischargeDetails = null;
                livestock.SoldAmount = null;
                livestock.SaleItemId = null;
                livestock.ModifiedAt = _dateTime.Now;

                var revertActivity = new LivestockActivity(livestock.Id, LivestockActivityType.Note, _dateTime.Now,
                    $"Loss reversed: {loss.LossNumber}")
                {
                    Metadata = $"LossId:{loss.Id};Reason:{reason}"
                };
                _db.LivestockActivities.Add(revertActivity);
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return await GetByIdAsync(id, companyId, ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
