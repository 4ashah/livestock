# Mobile Device UAT Checklist

This manual checklist complements the automated Playwright viewport matrix (360x800 → 1920x1080). Run these steps on physical devices before production go-live or after layout-affecting changes.

---

## 1. Android Chrome (Pixel / Samsung)

| # | Step | Pass Criteria | Notes |
|---|---|---|---|
| A1 | Open **Login** page at 360x800 equivalent (Portrait) | Page fits width; no page-level horizontal scroll bar; `Sign In` button above the on-screen keyboard when fields active. | Confirm `inputmode="tel"` on Settings Phone triggers number pad; decimal rate inputs trigger decimal keypad. |
| A2 | Device Pixel Ratio (DPR) 2.625+ text crispness | No blurry borders or rasterized edges on nav badges, buttons, kpi cards. | Compare text sharpness to native apps. |
| A3 | Rotate device to Landscape (width > height) | Sidebar toggles at <992px breakpoint on tablets; hamburger still present on narrow phones in landscape. | Confirm body `overflow-x:hidden` — no page drift left/right after rotate. |
| A4 | **Drawer / Hamburger** open/close | Tap `☰` → sidebar slides in from left with backdrop. Tap **backdrop** → sidebar closes. | Swipe-to-dismiss NOT required. Escape key optional on touch. |
| A5 | Open **Livestock Index** | Table inside `.table-responsive` wrapper scrolls independently (2-finger drag inside the table card). Body stays fixed horizontally. | Confirm table internal scroll with sticky header CSS if present; horizontal scroll bar visible only inside card not page. |
| A6 | **Data Entry** — Livestock/Register form | All fields reachable without zoom; date picker native widget opens; InitialWeight numeric input triggers decimal keyboard. | Avoid double-tap zoom on inputs (meta viewport `user-scalable=no` OK in _Layout). |

---

## 2. iPhone Safari (iOS 16+)

| # | Step | Pass Criteria | Notes |
|---|---|---|---|
| I1 | Open app on iPhone 14 (390x844, Notch / Dynamic Island) | `padding-top: calc(56px + 1.5rem)` on `.main-content` plus safe-area insets account for notch on fixed-top bar. Brand `🌾 Livestock Manager` not clipped. | Test on iOS 16.4+ — earlier versions may have 100vh URL-bar overlap. Use `100dvh` if available, or current `min-height: 100vh` fallback. |
| I2 | Notch **safe-area** check: rotate to Landscape | Sidebar toggler ☰ remains reachable; nav brand and avatar not clipped under horizontal notch. Footer copyright stays within safe area. | CSS `env(safe-area-inset-*)` optional for 2024 devices — verify with and without. |
| I3 | **Drawer open/close**: Sidebar backdrop + hamburger tap. | Tap `☰` → drawer slides in. Tap dark **backdrop** → drawer closes. | Confirm backdrop z-index 1015, sidebar 1020, topbar 1030 (higher than both). Avoid backdrop click-through to form buttons. |
| I4 | **Escape / hardware keyboard**: Close drawer with Esc key if BT/KB connected. | Sidebar closes and backdrop hides. | Optional for mobile-only. Mark N/A if no hardware keyboard available. |
| I5 | **1Password / iCloud Keychain** autofill on Login page | Credential picker sheet opens; after fill → Submit still visible above keyboard (no need to scroll). | Test with actual saved password not copy-paste. |
| I6 | Pull-to-refresh / double-tap on Livestock and Invoices tables. | No accidental full-page zoom; no double-tap zoom on cards. | Viewport meta `initial-scale=1.0` only. |

---

## 3. Tablets — iPad (1024 × 768 and vice versa)

| # | Step | Pass Criteria | Notes |
|---|---|---|---|
| T1 | **Portrait (768 x 1024)** | Check that sidebar uses `@media (max-width: 991.98px) → transform: translateX(-100%)`. Nav renders as mobile drawer + hamburger, NOT permanent sidebar. | Critical for 7.9" iPad Mini which falls just below 992px logical px breakpoint when in portrait. |
| T2 | **Landscape (1024 × 768)** | Sidebar becomes permanent (left 250px pinned), main content margin-left 250px. Hamburger disappears. | Match `.sidebar-layout` min-width: 992px rules. |
| T3 | **Reports / PDF Downloads** | Run the three main reports: ActiveLivestock (CSV), SalesByPeriod (CSV), Invoice PDF Download. Files download via Safari viewer; share sheet opens. | On iPad Safari, PDF opens inline by default — confirm share button offers "Save to Files". |
| T4 | **Sale → Invoice workflow end-to-end** | Create a Sale, add items, Confirm → generate Invoice #. Download invoice PDF. No horizontal scroll on any step. | Use **Accounts** user role (`accounts@livestock.dev` pw `Dev@123456`). |
| T5 | **Split view / Slide Over** (iPadOS) | Resize app to 1/3, 1/2, 2/3 widths. At <992px → hamburger. At ≥992px → sidebar. | Ensure no infinite re-layout loops during resize. |

---

## 4. Sign-off

| Device / Browser | Tester Name | Date | Result |
|---|---|---|---|
| Android Chrome (Pixel 7 or equiv.) | __________________ | ____-__-__ | ☐ PASS ☐ FAIL (attach screenshots) |
| iPhone Safari iOS 16+ (iPhone 14 / 15) | __________________ | ____-__-__ | ☐ PASS ☐ FAIL (attach screenshots) |
| iPad (iPad Mini 6 / iPad Air) | __________________ | ____-__-__ | ☐ PASS ☐ FAIL (attach screenshots) |
| Playwright E2E viewport matrix 7 sizes | Automated (scripts/Run-E2ETests.ps1) | ____-__-__ | ☐ PASS 7/7 ☐ FAIL: ____/7 |

**Notes / Defects logged:**
```
ID: _________ Severity: [Low/Med/High] Area: ___________ Description: ___________________________
```
