# .agent/STATE.md — Livestock Manager

## Phase / Feature
Feature: REPORTS_TAB_USER_REVIEW_FIXES (Spec sections 1-23 from C:\Users\Administrator\Desktop\livestock.txt)
Phase: Phase 18 Reports Tab Corrections

## Current Task (on resume start here)
All RFC-1..RFC-6 tasks completed this session. Final user-review status delivered: **REPORTS TAB CORRECTIONS READY FOR USER REVIEW**.
If resuming to amend/re-patch: start at RFC-6 for docs tweaks; if new functional gap start at RFC-2 backend.

## Last Valid Commit
Branch: `feature/stock-addition-desktop-mobile`
Base HEAD before work: `bb4f81f` (LTC: Livestock Tab user review corrections)
Latest valid commit SHA for this phase: `_________________` (to be filled after next `git add -A && git commit -m "<expected message in audit/REPORTS_TAB_USER_REVIEW_REPORT.md Section Final SHA block>"`)
Git status: dirty (files staged per git status; no commit performed this session per user consistent prior skip pattern)

## Completed Milestones
- RFC-1 Discovery + root cause ActiveLivestock farm filter confirmed
- RFC-2 Backend 4 new DTOs, 3 new service methods, 3 new DTO fields added to LivestockProfitabilityReportRowDto, ReportsController validated farmIds, from/to dates added LivestockProfitability + SalesByPeriod
- RFC-3 Desktop 3 reports rewrites: ActiveLivestock responsive + correct cast FarmSummaryDto; SalesByPeriod rewrite (# Sold→Number Sold, Farm filter, Farm summary table, Sale details table, Date Basis banner); LivestockProfitability rewrite (From/To dates, Summary 6 cards, Details 14 cols, Date Basis banner)
- RFC-4 Mobile 3 pages created: MobileActiveLivestock, MobileSalesByPeriod, MobileLivestockProfitability (cards, 44px mob-field, collapsible details, no wide tables)
- RFC-5 Build Release 0W/0E; Arch 60/60; Integration 15/15; Unit 322/324 (†2 intentional pre-existing); VS Code GetDiagnostics empty; Server running port 5100; HTTP smoke Login 200; 9 Reports routes 302 auth-redirect (3 new Mobile reports now OK 302 not 404 after ReportsController mobile actions added)
- RFC-6 State files created (this). BUILD_STATUS.md + CHANGELOG.md Phase 18 + audit/REPORTS_TAB_USER_REVIEW_REPORT.md Sections 1-23.

## Known Blockers / Next
- No functional blockers at this time.
- Known auditor attention list: (†2 intentionally failing PurchaseService.PostPurchase_* tests; MARS warning; no new migration this round — no new indexes required after inspect).
- Next exact task if continuing: optional git commit + back-patch SHA placeholders in BUILD_STATUS.md tracker row RFC-6 / CHANGELOG Phase 18 header / audit report Final Commit SHA block.

## Authorization & Security
All reports keep existing company + role isolation. New mobile routes protect via same policies: Operational reports → CanViewOperationalData; Financial reports → CanViewFinancialData. Farm filter validation server-side; unauthorized/manipulated FarmId → ignored → empty safe result. Export CSV receives same filters. Profit & Loss left completely untouched per spec mandate.

## Last Server PID
Running server terminal id = 5 (command id 160cc768-ba83-4780-8e00-b197b7240a86). Port 5100 listener active. If stale: kill via netstat findstr :5100 → Stop-Process -Id $ppid -Force → dotnet run --no-build -c Release --urls http://localhost:5100.
