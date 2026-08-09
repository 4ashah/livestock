# FINAL_PDF_RESULTS — Phase 15 PDF Generation Gate (G7)

**Date:** 2026-08-09
**Tool:** `FormattedPdfWriter` (LivestockManager.Infrastructure) via throwaway reflection console host
**Assembly used:** `LivestockManager.Infrastructure/Services/Pdf/FormattedPdfWriter.cs` (1079 lines, Pass1 layout + multi-page MeasureRow engine)
**Output directory:** `artifacts/pdf-samples/`

## Sample Files Generated

| # | File | Size (bytes) | PDF Header (/%PDF/) | Pages (/Type /Page) | Multi-page required | Unicode chars |
|:-:|:-----|-------------:|:-------------------:|--------------------:|:-------------------:|:-------------:|
| 1 | `invoice-01-item.pdf`    | **9,057**  | ✅ PASS | **1** | n/a | ❌ (Latin only) |
| 2 | `invoice-18-items.pdf`   | **26,265** | ✅ PASS | **2** | n/a | ❌ (Latin only) |
| 3 | `invoice-25-items.pdf`   | **35,452** | ✅ PASS | **3** | ✅ >1 required | ❌ (Latin only) |
| 4 | `invoice-50-items.pdf`   | **63,669** | ✅ PASS | **5** | ✅ >1 required | ❌ (Latin only) |
| 5 | `invoice-unicode.pdf`    | **17,242** | ✅ PASS | **1** | n/a | ✅ (see below) |
| 6 | `receipt.pdf`            | **6,324**  | ✅ PASS | **1** | n/a | ❌ (Latin only) |

**All size validations PASS:** every PDF ≥ 1000 bytes minimum.

## Unicode Verification — `invoice-unicode.pdf`

**Names asserted present (international + diacritics):**

| Token | Person / Meaning | Language |
|:------|:-----------------|:---------|
| `José María Gómez` | Customer billing name | Spanish (ES) |
| `François Müller`  | Street address line 1 + Terms paragraph | French (FR), German (DE) |
| `Łukasz`           | Street address line 2 + Terms (Polish L-stroke) | Polish (PL) |
| `Sørensen`         | Seller company name + City field (Danish slashed O) | Danish (DK) |

**Currency symbols asserted in Notes / Terms / totals:**

| Symbol | Currency | Unicode code point |
|:------:|:---------|:-------------------|
| ₹      | Indian Rupee  | U+20B9 |
| €      | Euro          | U+20AC |
| £      | British Pound | U+00A3 |
| R      | South African Rand | U+0052 (Latin) |
| $      | US Dollar     | U+0024 (Latin) |

**Encoding engine:** Type0 CIDFont + CIDFontType2 TrueType subset + Identity-H horizontal, using embedded Noto Sans Regular + Bold TTFs (Embedded Resources `LivestockManager.Infrastructure/Resources/Fonts/`). Unicode fallback mode = WinAnsi Helvetica (not triggered; `ForceFallbackModeForTesting = false` in production path).

## Download Content-Type assertion

Controller endpoint `InvoicesController.DownloadPdf(int id)` returns:
```
Content-Type: application/pdf
Content-Disposition: attachment; filename="INV-XXXX.pdf" (RFC 5987 encoded when non-ASCII)
```

Verified in Unit test `LivestockManager.UnitTests.Final.PdfUnicodeTests` (10/10 PASS) via `FileResult.ContentType == "application/pdf"` assertion.

## Multi-page engine confirmation

Engine pass:
1. **Pass 1 layout:** Measure each InvoiceItem row height (with wrapped description via Glyph widths) → sum BodyHeight until content exceeds 532pt (Letter 792pt − 140pt Header − 120pt Footer)
2. **Page break insertion:** emit one page, continue to next with continued Items
3. **Footer:** Page N of M on every page (final M is known only after Pass 1 completes)

Verified by G7 regex `/Type /Page[^s]` count:
- 25-item → 3 pages ✅
- 50-item → 5 pages ✅
