# .agent/DEFECTS.md — REMOVE_VIEWER_AND_FINALIZE_SIX_ROLES

| # | Defect | Status | Notes |
|---|---|---|---|
| RVR-DFX-001 | Active role model still contained Viewer in runtime constants, demo seeds, login demo accounts, navigation, and selectors | FIXED | Final six-role model is now the active runtime model |
| RVR-DFX-002 | Controllers used legacy/broad string policies instead of centralized final `PolicyNames` constants | FIXED | Active controllers normalized onto the final policy set |
| RVR-DFX-003 | `StockAdditionController` was locked behind broad livestock-management policy and did not match purchase/newborn rights split | FIXED | Purchase endpoints use `CanRecordStockPurchase`; newborn endpoints use `CanRecordNewborn`; index/success routes use `CanViewLivestock` |
| RVR-DFX-004 | FarmManager still received financial visibility in some runtime paths | FIXED | Dashboard/nav/runtime checks tightened to final policy expectations |
| RVR-DFX-005 | Development database still had one Viewer-only user | FIXED | `viewer@livestock.dev` migrated to disabled `DataEntry`; Viewer assignment count now zero |
| RVR-DFX-006 | Viewer-retirement startup service hit a role update concurrency failure under integration startup | FIXED | Retirement role-seeding path now tolerates duplicate/concurrent role updates and integration tests pass |
| RVR-DFX-007 | Current docs/status files still described Viewer as an active role | FIXED | Active documentation now describes the six-role model and keeps Viewer only as a retired historical reference where needed |
| RVR-DFX-008 | Playwright/E2E verification for the final six-role pass was failing on stale email-only login selectors | FIXED | Updated E2E selectors to match the current `UserName` login field; targeted login slice passed `8/8` and the full suite passed `39/39` |
