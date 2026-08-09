# Third-Party Notices

This file contains attributions for third-party software, fonts, and resources distributed with or used by this project.

---

## Noto Sans (Regular & Bold)

- **Name**: Noto Sans
- **Version**: v2.000 (subsetted for Latin Extended + Currency symbols)
- **License**: SIL Open Font License 1.1 (OFL 1.1)
- **Source**: https://fonts.google.com/noto/specimen/Noto+Sans
- **Attribution**: Designed by Google Inc. Noto is a trademark of Google Inc.
- **Included as**: Embedded resource for PDF Unicode rendering in invoices/receipts.

### ⚠️ CURRENT FONT STATUS (ROUND2 2026-08-09)

> **Validated genuine Noto Sans fonts were not available in this build.**
>
> Invalid 169-byte placeholder assets (that only mimicked genuine Noto-Sans file names but contained **zero usable TrueType data** and could not be parsed by a TrueType reader) were **REMOVED** from `src/LivestockManager.Infrastructure/Resources/Fonts/`. They were rejected by the new `TrueTypeFontValidator` structural C# class (which enforces ≥ 50 KB, 0x00010000 signature, 8–40 tables, and the presence of all 9 required TrueType tables: head, hhea, maxp, hmtx, cmap, name, OS/2, glyf, loca).
>
> - **Unicode PDF rendering:** **BLOCKED_EXTERNAL**
> - **Safe basic WinAnsi Helvetica fallback:** ACTIVE for all PDF generation (invoice/receipt rendering) until a validated genuine font is embedded and `TrueTypeFontValidator` passes.
> - **Basic ASCII-only customer names / symbols / currency codes:** Supported via WinAnsi Helvetica fallback.
> - **International names with diacritics / non-Latin scripts / extended currency glyphs (₹ € Ł etc.):** Not rendered correctly until genuine subsetted Noto Sans (Regular + Bold) is obtained, placed under the Fonts/ directory, and passes `TrueTypeFontValidator.Validate()`.
> - **Action for future maintainers:** Obtain genuine NotoSans-Regular.ttf + NotoSans-Bold.ttf (≥ 50 KB each, genuine Google Noto release), drop them into `src/LivestockManager.Infrastructure/Resources/Fonts/`, ensure OFL.txt is present, then re-run `TrueTypeFontValidatorTests` and `PdfUnicodeTests`.

### SIL Open Font License 1.1 Summary

The OFL allows the licensed fonts to be used, studied, modified and redistributed freely as long as they are not sold by themselves. The fonts, including any derivative works, can be bundled, embedded, redistributed and/or sold with any software provided that any reserved names are not used by derivative works. The fonts and derivatives, however, cannot be released under any other type of license. The requirement for fonts to remain under this license does not apply to any document created using the fonts or their derivatives.

Full license text: https://openfontlicense.org/open-font-license-official-text/

---

## Bootstrap v5.3.x

- **Name**: Bootstrap
- **Version**: v5.3.x
- **License**: MIT License
- **Copyright**: Copyright (c) 2011-2024 Twitter, Inc. / The Bootstrap Authors
- **Source**: https://github.com/twbs/bootstrap
- **License File**: wwwroot/lib/bootstrap/LICENSE
- **Included as**: Front-end CSS/JS framework for responsive UI and layout.

### MIT License Summary (Bootstrap)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND. Full license text in wwwroot/lib/bootstrap/LICENSE.

---

## jQuery v3.7.x

- **Name**: jQuery
- **Version**: v3.7.x
- **License**: MIT License
- **Copyright**: Copyright JS Foundation and other contributors
- **Source**: https://jquery.com/
- **License File**: wwwroot/lib/jquery/LICENSE.txt
- **Included as**: DOM manipulation and AJAX helper library for client-side interactivity.

---

## jQuery Validation

- **Name**: jQuery Validation Plugin
- **Version**: v1.x (shipped with ASP.NET Core Identity UI default template)
- **License**: MIT License
- **Copyright**: Copyright (c) 2006-2014 Jörn Zaefferer
- **Source**: https://github.com/jquery-validation/jquery-validation
- **License File**: wwwroot/lib/jquery-validation/LICENSE.md
- **Included as**: Client-side input validation for forms (used with jQuery Validation Unobtrusive).

---

## jQuery Validation Unobtrusive

- **Name**: jQuery Validation Unobtrusive
- **Version**: v4.x (shipped with ASP.NET Core Identity UI default template)
- **License**: MIT License
- **Copyright**: Copyright (c) .NET Foundation and Contributors
- **Source**: https://github.com/aspnet/jquery-validation-unobtrusive
- **License File**: wwwroot/lib/jquery-validation-unobtrusive/LICENSE.txt
- **Included as**: Data-val attribute binding between ASP.NET Core server-side Model validation and jQuery Validation client plugin.

---

## Microsoft.EntityFrameworkCore.* / Microsoft.AspNetCore.*

- **Name**: Microsoft.EntityFrameworkCore (SqlServer, Design, InMemory, Tools), Microsoft.AspNetCore.* (Identity.EntityFrameworkCore, Identity.UI, Mvc, Diagnostics.EntityFrameworkCore, HealthChecks.EntityFrameworkCore, etc.)
- **Version**: 8.0.x (various patch versions)
- **License**: MIT License
- **Copyright**: Copyright (c) .NET Foundation and Contributors
- **Source**: https://github.com/dotnet/efcore and https://github.com/dotnet/aspnetcore
- **Included as**: Primary ORM (EF Core), web framework (ASP.NET Core MVC/Razor), Identity (user/role auth), and health checks.

### MIT License Summary (.NET Foundation packages)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND. Full license text: https://licenses.nuget.org/MIT

---

## xUnit

- **Name**: xunit + xunit.runner.visualstudio
- **Version**: 2.9.2 (xunit), 2.8.2 (runner.visualstudio)
- **License**: Apache License 2.0
- **Copyright**: Copyright (c) .NET Foundation and Contributors, James Newkirk, Brad Wilson
- **Source**: https://github.com/xunit/xunit
- **Included as**: Primary unit / integration / architecture / end-to-end test framework.

### Apache 2.0 Summary (xUnit)

You may reproduce and distribute copies of the Work or Derivative Works thereof in any medium, with or without modifications, and in Source or Object form, provided that You meet the following conditions:
- You must give any other recipients of the Work or Derivative Works a copy of this License; and
- You must cause any modified files to carry prominent notices stating that You changed the dates; and
- You must retain, in the Source form of any Derivative Works that You distribute, all copyright, patent, trademark, and attribution notices from the Source form of the Work.

Full license text: https://licenses.nuget.org/Apache-2.0

---

## NetArchTest.Rules

- **Name**: NetArchTest.Rules
- **Version**: 1.3.2
- **License**: MIT License (from NuGet package metadata)
- **Copyright**: Copyright (c) NetArchTest Contributors / Ben Driver
- **Source / NuGet**: https://www.nuget.org/packages/NetArchTest.Rules
- **Included as**: Architecture tests — layer dependency enforcement (Domain -> Application -> Infrastructure -> Web) plus controller inheritance checks in tests/LivestockManager.ArchitectureTests.

---

## FluentValidation

- **Name**: FluentValidation + FluentValidation.DependencyInjectionExtensions
- **Version**: 11.9.0 (both packages)
- **License**: Apache License 2.0
- **Copyright**: Copyright (c) Jeremy Skinner and Contributors
- **Source**: https://github.com/FluentValidation/FluentValidation
- **Included as**: Direct PackageReference in LivestockManager.Application (currently potential unused — see audit/FINAL_DEPENDENCY_SCAN.md section 13c for review status).

### Apache 2.0 Summary (FluentValidation)

Same terms as Apache 2.0 above. Attribution: Jeremey Skinner. Full license: https://licenses.nuget.org/Apache-2.0

---

## Microsoft.Playwright + Microsoft.Playwright.NUnit

- **Name**: Microsoft.Playwright + Microsoft.Playwright.NUnit
- **Version**: 1.52.0 (both packages)
- **License**: MIT License
- **Copyright**: Copyright (c) Microsoft Corporation
- **Source**: https://github.com/microsoft/playwright-dotnet
- **Included as**: End-to-end browser automation framework used by tests/LivestockManager.EndToEndTests (Playwright Chromium driver, page object APIs, and NUnit fixtures).

### MIT License Summary (Playwright)

Same MIT terms as above. Copyright Microsoft Corporation. Full license: https://licenses.nuget.org/MIT
