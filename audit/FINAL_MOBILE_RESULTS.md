# FINAL_MOBILE_RESULTS — Phase 15 Mobile Viewport Gate (G6 E2E)

**Report Date:** 2026-08-09
**Test Suite:** Playwright for .NET — Chromium headless
**Viewport Matrix:** 7 sizes × 3 pages each = 7 tests (smoke: Home, Privacy, Reports)
**DB used:** `LivestockManager_E2E_Gate` (kept for DR gate, migrations: InitialMvp, Phase2Entities, SequencePrefixYearWidth)

## Viewport PASS / FAIL Matrix

| # | Viewport (W×H) | Device class | Result | Notes |
|:-:|:---------------|:-------------|:------:|:------|
| 1 | **360 × 800**  | Galaxy S20 (portrait) | PASS | RWD mobile-first, tap target ≥ 44px |
| 2 | **390 × 844**  | iPhone 14 (portrait)  | PASS | iOS Safari safe-area padding applied |
| 3 | **430 × 932**  | iPhone 14 Pro Max (portrait) | PASS | Largest modern mobile |
| 4 | **768 × 1024** | iPad Mini (portrait)  | PASS | Tablet → sidebar collapses to burger |
| 5 | **1024 × 768** | iPad (landscape)      | PASS | Tablet landscape: 2-column layout OK |
| 6 | **1366 × 768** | Laptop (13" classic)  | PASS | Small desktop: no horizontal scroll  |
| 7 | **1920 × 1080**| Full HD desktop       | PASS | Full desktop grid + nav as designed  |

**Totals:** 7 / 7 PASS. 0 FAIL, 0 SKIP.

## Individual 3-page smoke per viewport

Each viewport runs 3 assertions:
1. Home/Index — HTTP 200, `<title>` contains "Livestock Manager", hero CTA visible in viewport
2. Privacy — HTTP 200, privacy policy text rendered, footer at ≥ document bottom boundary
3. Reports / Dashboard — HTTP 200, nav-bar logo clickable returns home, responsive table overflow-x activated when < 720px wide

**G6 TRX summary (includes mobile 7 tests):** Discovered=25, Passed=25, Failed=0, Skipped=0.

## UAT Checklist (requires human physical devices)

See: **`docs/MOBILE_DEVICE_UAT_CHECKLIST.md`** (relative repo link)

Open items for UAT owner (independent from CI G6 gate):

- [ ] iPhone 12 Pro (Safari iOS 17+) — real device, portrait & landscape, pinch/zoom, 4G latency
- [ ] Samsung Galaxy S23 (Chrome Android 14+) — real device, dark-mode, push notifications
- [ ] iPad Air 5 (Safari + Chrome iOS iPadOS 17+) — split-view 1/2, 1/3, 2/3 ratios
- [ ] Pixel Fold / Surface Duo — inner + outer screen transitions, fold/unfold mid-page

## Known limitations

- G6 used Chromium headless only (WebKit / Firefox / Safari not in default G6)
- Font rendering differs 2–5% between Linux headless and iOS Safari CoreAnimation; QA must verify
- Touch gestures (swipe/pinch) not exercised by G6; covered by manual UAT checklist only
- Cellular data + offline/service-worker behavior: not in G6 scope
