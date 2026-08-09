# ROUND2 CLOCK CHECK — FINAL AUDIT STABILIZATION

## CLOCK SYNC STATUS

CLOCK_SYNC_REQUIRED = 1

Sandbox OS clock (and SQL SYSDATETIMEOFFSET) currently report 2026-08-09.
Authoritative audit baseline date is 2026-08-06, which is 3 days behind sandbox wall clock.
No issued records are altered or back-dated; document sequence year fields reflect CURRENT clock values.

## Raw Clock Readings (2026-08-09T20:52:04 IST UTC+05:30)

### Get-Date -Format o
```
2026-08-09T20:52:04.0288689+05:30
```

### TimeZone
```
Id            : India Standard Time
DisplayName   : (UTC+05:30) Chennai, Kolkata, Mumbai, New Delhi
BaseUtcOffset : 05:30:00
```

### w32tm /query /status
```
Leap Indicator: 0(no warning)
Stratum: 5 (secondary reference - syncd by (S)NTP)
Precision: -23 (119.209ns per tick)
Root Delay: 0.0161414s
Root Dispersion: 0.0335584s
ReferenceId: 0x28515E41 (source IP:  40.81.94.65)
Last Successful Sync Time: 8/9/2026 8:47:02 PM
Source: time.windows.com,0x8
Poll Interval: 10 (1024s)
```

### SQL SYSDATETIMEOFFSET / SYSUTCDATETIME
```
2026-08-09 20:52:04.1570156 +05:30  |  2026-08-09 15:22:04.1570156
```

---

## DOCUMENT SEQUENCE NUMBER FORMATS (ENFORCED, NOT ALTERED)

The following sequence number formats are produced by `EfSequenceGenerator` (see unit tests in `SequenceGeneratorTests.cs`):

| Document Type | Prefix | Canonical Format | Example |
|---|---|---|---|
| Invoice | `INV` | `INV-YYYY-NNNNN` | INV-2026-00001 |
| Receipt | `RCP` | `RCP-YYYY-NNNNN` | RCP-2026-00001 |
| Payment | `PAY` | `PAY-YYYY-NNNNN` | PAY-2026-00001 |
| Purchase Order | `PUR` | `PUR-YYYY-NNNNN` | PUR-2026-00001 |

Format rules (enforced by unit tests in `SequenceGeneratorTests.cs`):
- `YYYY` = 4-digit calendar year (UTC), derived from the counter key partition `prefix:year`.
- `NNNNN` = 5-digit zero-padded integer, >= 00001, produced by `lastValue.ToString("D5")`.
- Hyphens are literal separators; no concatenated forms are ever emitted.

Test coverage preserved:
- `SequenceGeneratorTests.GenerateInvoiceNumber_IncludesCurrentYearAndHyphens` → asserts format matches regex `^INV-\d{4}-\d{5}$`.
- `SequenceGeneratorTests.GenerateReceiptNumber_UsesRcpPrefix_NotLegacyRct` → asserts regex `^RCP-\d{4}-\d{5}$` (verifies legacy typo `RCT` is never used).
- `SequenceGeneratorTests.PurchaseAndPaymentFormat_YearWidth5_ZeroPadded` → asserts both `PUR-YYYY-NNNNN` and `PAY-YYYY-NNNNN` regex formats are produced.
- 3 real-SQL concurrent sequence tests verify 20 parallel invoice numbers are contiguous distinct `00001..00020` with correct hyphen+year.

Livestock tag ID format (`Ah00001`, `Su00001`, `Sa00001`, `Ad00001`, `Sd00001`) explicitly does NOT include a year component (historical compatibility; no test requires a year in livestock tag IDs).
