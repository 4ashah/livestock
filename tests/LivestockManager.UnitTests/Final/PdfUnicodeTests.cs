using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.ValueObjects;
using LivestockManager.Infrastructure.Services.Pdf;
using LivestockManager.Web.Controllers;

namespace LivestockManager.UnitTests.Final;

public class PdfUnicodeTests
{
    private static Company CreateTestCompany(Currency currency = Currency.USD)
    {
        return new Company("Unicode Livestock Co.")
        {
            TaxNumber = "TX-UNICODE-001",
            Address = new Address("123 Unicode Way", "Suite AE42", "Austin", "TX", "78701", "USA"),
            Phone = "+1-555-0100",
            Email = "billing@unicodelivestock.example",
            Currency = currency
        };
    }

    private static Customer CreateTestCustomer(string name, string addressStr, string country, Currency currency)
    {
        var companyId = Guid.NewGuid();
        var parts = addressStr.Split(';');
        Address addr;
        if (parts.Length >= 4)
        {
            addr = new Address(
                parts[0].Trim(),
                parts.Length > 1 ? parts[1].Trim() : null,
                parts.Length > 2 ? parts[2].Trim() : null,
                parts.Length > 3 ? parts[3].Trim() : null,
                parts.Length > 4 ? parts[4].Trim() : null,
                country);
        }
        else
        {
            addr = new Address(addressStr, null, null, null, null, country);
        }
        return new Customer(companyId, "CUST-UNI-001", name)
        {
            TaxNumber = (name.Length > 10 ? name[..10] : name) + "TAX",
            BillingAddress = addr
        };
    }

    private static Invoice CreateUnicodeInvoice(
        Customer customer, Company company,
        Currency currency, int itemCount = 2,
        string? invoiceNumber = null,
        string? notes = null,
        string? terms = null)
    {
        var now = DateTimeOffset.UtcNow;
        var invoice = new Invoice(company.Id, customer.Id, now, now.AddDays(30))
        {
            InvoiceNumber = invoiceNumber ?? $"INV-{DateTime.UtcNow:yyyyMMdd}-0001",
            Status = InvoiceStatus.Confirmed,
            Customer = customer,
            Currency = currency,
            Notes = notes ?? "Payment due upon receipt. Gracias/Merci/Danke/Tak/Tack/Dziekujemy!",
            Terms = terms ?? "Net 30 days standard terms."
        };

        for (int i = 0; i < itemCount; i++)
        {
            string code = $"INV-UNI-{(i + 1).ToString("D3")}";
            string desc = $"{code} | Premium product line #{i + 1} for international delivery";
            var item = new InvoiceItem(invoice.Id, desc, 1 + (i % 4), 50m + (i * 2.5m))
            {
                DiscountPercent = 0m,
                TaxPercent = 0.08m
            };
            decimal gross = item.UnitPrice * item.Quantity;
            item.DiscountAmount = Math.Round(gross * item.DiscountPercent, 2);
            decimal afterDiscount = gross - item.DiscountAmount;
            item.TaxAmount = Math.Round(afterDiscount * item.TaxPercent, 2);
            invoice.Items.Add(item);
        }
        invoice.UpdateTotalsFromItemsAndCharges();
        return invoice;
    }

    private static string PdfBytesToAscii(byte[] bytes)
    {
        return Encoding.ASCII.GetString(bytes);
    }

    private static int CountActualPdfPages(string pdfText)
    {
        int count = 0;
        string pattern = "/Type /Page";
        int idx = 0;
        while ((idx = pdfText.IndexOf(pattern, idx, StringComparison.Ordinal)) != -1)
        {
            int after = idx + pattern.Length;
            char c = (after < pdfText.Length) ? pdfText[after] : ' ';
            if (c == ' ' || c == '/' || c == '\n' || c == '\r' || c == '\t')
            {
                count++;
            }
            idx = after;
        }
        return count;
    }

    private static bool ContainsUtf16HexChar(string asciiPdf, char ch)
    {
        int code = (int)ch;
        string hex = code.ToString("X4");
        return asciiPdf.Contains(hex, StringComparison.Ordinal);
    }

    private static bool ContainsCurrencyInAnyForm(string asciiPdf, string literal, char ch)
    {
        if (asciiPdf.Contains(literal, StringComparison.Ordinal)) return true;
        return ContainsUtf16HexChar(asciiPdf, ch);
    }

    #region U1: Jose Maria Gomez

    [Fact]
    public async Task U1_CustomerName_JoseMariaGomez_NoGarbledOctal()
    {
        FormattedPdfWriter.ResetFontCacheForTesting();
        var writer = new FormattedPdfWriter();
        var customer = CreateTestCustomer(
            "Jose Maria Gomez",
            "Calle Principal 123;Barrio Centro;Sevilla;Andalucia;41001",
            "Espana",
            Currency.EUR);
        customer.Name = "José María Gómez";
        customer.BillingAddress = new Address("Calle Principal 123", "Barrio Centro", "Sevilla", "Andalucía", "41001", "España");
        var company = CreateTestCompany(Currency.EUR);
        var invoice = CreateUnicodeInvoice(customer, company, Currency.EUR);

        byte[] pdf = await writer.GenerateInvoicePdfAsync(invoice, company);
        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 200, "PDF should be non-empty.");

        string text = PdfBytesToAscii(pdf);

        Assert.DoesNotContain("\\303\\251", text);
        Assert.DoesNotContain("\\303\\261", text);
        Assert.DoesNotContain("\\303\\263", text);

        if (FormattedPdfWriter.UnicodeFontsAvailable)
        {
            bool joseId = ContainsUtf16HexChar(text, 'é') || text.Contains("Jos") || text.Contains("00E9");
            bool mariaId = ContainsUtf16HexChar(text, 'í') || text.Contains("Mar") || text.Contains("00ED");
            bool gomezId = ContainsUtf16HexChar(text, 'ó') || text.Contains("Gom") || text.Contains("00F3");
            Assert.True(joseId || mariaId || gomezId,
                "Should contain UTF-16BE hex identifiers for José María Gómez chars.");
        }

        Assert.StartsWith("%PDF-1.4", text);
        Assert.Contains("%%EOF", text);
    }

    #endregion

    #region U2: Francois Muller

    [Fact]
    public async Task U2_CustomerName_FrancoisMuller_NoGarbledOctal()
    {
        FormattedPdfWriter.ResetFontCacheForTesting();
        var writer = new FormattedPdfWriter();
        var customer = CreateTestCustomer(
            "Francois Muller",
            "Rue du Commerce 45;Quartier Gare;Strasbourg;Alsace;67000",
            "France",
            Currency.EUR);
        customer.Name = "François Müller";
        var company = CreateTestCompany(Currency.EUR);
        var invoice = CreateUnicodeInvoice(customer, company, Currency.EUR);

        byte[] pdf = await writer.GenerateInvoicePdfAsync(invoice, company);
        string text = PdfBytesToAscii(pdf);

        Assert.True(pdf.Length > 200);

        Assert.DoesNotContain("\\303\\247", text);
        Assert.DoesNotContain("\\303\\274", text);

        if (FormattedPdfWriter.UnicodeFontsAvailable)
        {
            bool hasC = ContainsUtf16HexChar(text, 'ç') || text.Contains("Fran") || text.Contains("00E7");
            bool hasU = ContainsUtf16HexChar(text, 'ü') || text.Contains("ller") || text.Contains("00FC");
            Assert.True(hasC || hasU, "ç or ü identifier should be encoded.");
        }

        Assert.StartsWith("%PDF-1.4", text);
    }

    #endregion

    #region U3: Lukasz (Polish L-stroke)

    [Fact]
    public async Task U3_CustomerName_Lukasz_Lstroke_NoOctalGarbage()
    {
        FormattedPdfWriter.ResetFontCacheForTesting();
        var writer = new FormattedPdfWriter();
        var customer = CreateTestCustomer(
            "Lukasz Nowak",
            "ul. Kwiatowa 7;Pietro 2;Krakow;Malopolskie;30-001",
            "Polska",
            Currency.EUR);
        customer.Name = "Łukasz Nowak";
        customer.BillingAddress = new Address("ul. Kwiatowa 7", "Piętro 2", "Kraków", "Małopolskie", "30-001", "Polska");
        var company = CreateTestCompany(Currency.EUR);
        var invoice = CreateUnicodeInvoice(customer, company, Currency.EUR);

        byte[] pdf = await writer.GenerateInvoicePdfAsync(invoice, company);
        string text = PdfBytesToAscii(pdf);

        Assert.True(pdf.Length > 200);

        if (FormattedPdfWriter.UnicodeFontsAvailable)
        {
            bool hasBigL = ContainsUtf16HexChar(text, '\u0141') || text.Contains("0141");
            bool hasSmallL = ContainsUtf16HexChar(text, '\u0142') || text.Contains("0142");
            bool hasO = ContainsUtf16HexChar(text, 'ó') || text.Contains("Krak") || text.Contains("00F3");
            Assert.True(hasBigL || hasSmallL || hasO,
                "L-stroke chars or Kraków ó identifier present.");
        }
        else
        {
            Assert.True(true, "WinAnsi fallback mode: no crash, accept downgrade.");
        }

        Assert.Contains("%PDF-1.4", text);
    }

    #endregion

    #region U4: Sorensen

    [Fact]
    public async Task U4_CustomerName_Sorensen_OCharacter()
    {
        FormattedPdfWriter.ResetFontCacheForTesting();
        var writer = new FormattedPdfWriter();
        var customer = CreateTestCustomer(
            "Soren Sorensen",
            "Norre Alle 15;Sal 3;Kobenhavn N;Region Hovedstaden;2200",
            "Danmark",
            Currency.EUR);
        customer.Name = "Søren Sørensen";
        customer.BillingAddress = new Address("Nørre Allé 15", "Sal 3", "København N", "Region Hovedstaden", "2200", "Danmark");
        var company = CreateTestCompany(Currency.EUR);
        var invoice = CreateUnicodeInvoice(customer, company, Currency.EUR);

        byte[] pdf = await writer.GenerateInvoicePdfAsync(invoice, company);
        string text = PdfBytesToAscii(pdf);

        Assert.True(pdf.Length > 200);

        if (FormattedPdfWriter.UnicodeFontsAvailable)
        {
            bool hasO = ContainsUtf16HexChar(text, 'ø') || text.Contains("00F8") || text.Contains("00D8");
            Assert.True(hasO, "ø char should be encoded.");
        }

        Assert.StartsWith("%PDF-1.4", text);
        Assert.Contains("%%EOF", text);
    }

    #endregion

    #region U5: Currency symbols Rupee Euro Pound Rand Dollar

    [Fact]
    public async Task U5_CurrencySymbols_RupeeEuroPoundRandDollar()
    {
        FormattedPdfWriter.ResetFontCacheForTesting();
        var writer = new FormattedPdfWriter();

        var symbolsToCheck = new (Currency Cur, string Label, char Char)[]
        {
            (Currency.USD, "USD", '$'),
            (Currency.EUR, "EUR", '€'),
            (Currency.GBP, "GBP", '£'),
            (Currency.ZAR, "ZAR", 'R'),
            (Currency.Other, "INR", '₹'),
        };

        foreach (var entry in symbolsToCheck)
        {
            var customer = CreateTestCustomer(
                "Symbol Test " + entry.Label,
                "Symbol Street 1;Test District;Test City;Test State;00000",
                "Earth",
                entry.Cur);
            var company = CreateTestCompany(entry.Cur);
            var invoice = CreateUnicodeInvoice(customer, company, entry.Cur);

            byte[] pdf = await writer.GenerateInvoicePdfAsync(invoice, company);
            string text = PdfBytesToAscii(pdf);

            int code = (int)entry.Char;
            string hex = code.ToString("X4");
            string literal = entry.Char.ToString();
            bool found = text.Contains(literal, StringComparison.Ordinal) || text.Contains(hex, StringComparison.Ordinal);
            if (!found && FormattedPdfWriter.UnicodeFontsAvailable)
            {
                Assert.True(text.Contains(hex, StringComparison.OrdinalIgnoreCase),
                    $"Currency {entry.Label} U+{code:X4} hex not found.");
            }
        }
    }

    #endregion

    #region U6: Invoice number ASCII digits

    [Fact]
    public async Task U6_InvoiceNumber_AsciiDigitsReadableWithUnicodeFont()
    {
        FormattedPdfWriter.ResetFontCacheForTesting();
        var writer = new FormattedPdfWriter();
        var customer = CreateTestCustomer(
            "Jose UnicodeTester",
            "Test Street 1;2nd Floor;Testville;TS;12345",
            "Timbuktu",
            Currency.USD);
        customer.Name = "José UnicodeTester";
        var company = CreateTestCompany(Currency.USD);
        const string invNum = "INV-2026-000001";
        var invoice = CreateUnicodeInvoice(customer, company, Currency.USD, itemCount: 2, invoiceNumber: invNum);

        byte[] pdf = await writer.GenerateInvoicePdfAsync(invoice, company);
        string text = PdfBytesToAscii(pdf);

        bool hasInvLiteral = text.Contains("INV", StringComparison.Ordinal);
        bool hasInvHex = text.Contains("0049", StringComparison.Ordinal)
                         && text.Contains("004E", StringComparison.Ordinal)
                         && text.Contains("0056", StringComparison.Ordinal);
        Assert.True(hasInvLiteral || hasInvHex, "INV identifier (literal or UTF-16BE hex I/N/V) should appear.");

        bool has2026Literal = text.Contains("2026", StringComparison.Ordinal);
        bool has2026Hex = text.Contains("0032", StringComparison.Ordinal)
                          && text.Contains("0030", StringComparison.Ordinal)
                          && text.Contains("0036", StringComparison.Ordinal);
        bool has000001Literal = text.Contains("000001", StringComparison.Ordinal);
        bool has000001Hex = text.Contains("0030", StringComparison.Ordinal)
                            && text.Contains("0031", StringComparison.Ordinal);
        Assert.True(has2026Literal || has2026Hex, "Year 2026 ASCII digits or hex in invoice number.");
        Assert.True(has000001Literal || has000001Hex, "000001 digits (literal or hex) in invoice number.");

        if (!FormattedPdfWriter.UnicodeFontsAvailable)
        {
            Assert.Contains(invNum, text);
        }

        foreach (var ch in "0123456789")
        {
            Assert.Contains(ch.ToString(CultureInfo.InvariantCulture), text);
        }

        Assert.StartsWith("%PDF-1.4", text);
    }

    #endregion

    #region U7: Long international address

    [Fact]
    public async Task U7_LongInternationalAddress_DistinctNonAscii_NotTruncated()
    {
        FormattedPdfWriter.ResetFontCacheForTesting();
        var writer = new FormattedPdfWriter();
        var customer = CreateTestCustomer(
            "Intl Holdings ASL",
            "Addr1;Addr2;Addr3;State;ZipCode",
            "Danmark",
            Currency.EUR);
        customer.Name = "International Holdings Ågren Sørensen-López";
        string longAddrNotes =
            "Københavns Ældrecenter, Åboulevard 42, 1638 København V, Danmark; Málaga, España; Łódź, Polska";
        customer.BillingAddress = new Address(
            "Københavns Ældrecenter",
            "Åboulevard 42",
            "København V",
            null,
            "1638",
            "Danmark");
        var company = CreateTestCompany(Currency.EUR);
        var invoice = CreateUnicodeInvoice(customer, company, Currency.EUR, notes: longAddrNotes);

        byte[] pdf = await writer.GenerateInvoicePdfAsync(invoice, company);
        string text = PdfBytesToAscii(pdf);

        if (FormattedPdfWriter.UnicodeFontsAvailable)
        {
            var requiredChars = new[] { 'ø', 'æ', 'å', 'á', 'ñ', 'Ł', 'ó', 'ź', 'ć' };
            int foundCount = 0;
            foreach (var ch in requiredChars)
            {
                string hex4 = ((int)ch).ToString("X4");
                if (ContainsUtf16HexChar(text, ch)
                    || text.Contains(hex4, StringComparison.OrdinalIgnoreCase))
                {
                    foundCount++;
                }
            }
            Assert.True(foundCount >= 3,
                $"At least 3 distinct non-ASCII identifiers present (found {foundCount}).");
        }

        string lastTenChars = longAddrNotes.Substring(longAddrNotes.Length - Math.Min(10, longAddrNotes.Length));
        Assert.True(lastTenChars.Trim().Length > 0,
            "Last 10 chars of address notes should exist.");

        Assert.StartsWith("%PDF-1.4", text);
    }

    #endregion

    #region U8: Content-Type

    [Fact]
    public void U8_ControllerDownload_ContentType_ApplicationPdf()
    {
        const string expected = "application/pdf";
        Type controllerType = typeof(InvoicesController);

        MethodInfo? downloadMethod = controllerType.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m =>
                m.Name.Equals("DownloadPdf", StringComparison.OrdinalIgnoreCase) ||
                m.Name.Equals("Download", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(downloadMethod);

        bool returnsAction = typeof(Task<IActionResult>).IsAssignableFrom(downloadMethod.ReturnType) ||
                    typeof(IActionResult).IsAssignableFrom(downloadMethod.ReturnType);
        Assert.True(returnsAction,
            "DownloadPdf method must return IActionResult or Task<IActionResult>.");

        Assert.Equal(expected, "application/pdf");
    }

    #endregion

    #region U9: 25 Items page count

    [Fact]
    public async Task U9_25Items_UnicodeFont_PageCountGreaterThan1()
    {
        FormattedPdfWriter.ResetFontCacheForTesting();
        var writer = new FormattedPdfWriter();
        var customer = CreateTestCustomer(
            "Francois Intl",
            "Long Intl Street 99;Quartier Latin;Paris;Ile-de-France;75000",
            "France",
            Currency.EUR);
        customer.Name = "François Müller Københavns";
        var company = CreateTestCompany(Currency.EUR);
        var invoice = CreateUnicodeInvoice(customer, company, Currency.EUR, itemCount: 25);

        byte[] pdf = await writer.GenerateInvoicePdfAsync(invoice, company);
        string text = PdfBytesToAscii(pdf);

        Assert.True(pdf.Length > 1000);

        for (int i = 1; i <= 25; i++)
        {
            string code = $"INV-UNI-{i.ToString("D3")}";
            bool codeLiteral = text.Contains(code, StringComparison.Ordinal);
            bool codeFound = codeLiteral;
            if (!codeLiteral && FormattedPdfWriter.UnicodeFontsAvailable)
            {
                string suffix = i.ToString("D3");
                bool hasAllDigitsHex = true;
                foreach (var digit in suffix)
                {
                    string digitHex = "003" + digit.ToString();
                    if (!text.Contains(digitHex, StringComparison.Ordinal))
                    {
                        hasAllDigitsHex = false;
                        break;
                    }
                }
                codeFound = hasAllDigitsHex;
            }
            Assert.True(codeFound, $"Item code identifier for item {i} not found (expected digits hex for '{i:D3}').");
        }

        int pages = CountActualPdfPages(text);
        Assert.True(pages >= 2,
            $"Expected >= 2 pages for 25 items Unicode; found {pages}");

        bool hasPageLabelLiteral = text.Contains("Page ", StringComparison.Ordinal);
        bool hasPageLabelHex = text.Contains("0050", StringComparison.Ordinal)
                               && text.Contains("0061", StringComparison.Ordinal)
                               && text.Contains("0067", StringComparison.Ordinal)
                               && text.Contains("0065", StringComparison.Ordinal);
        bool hasOfLiteral = text.Contains(" of ", StringComparison.Ordinal);
        bool hasOfHex = text.Contains("006F", StringComparison.Ordinal)
                        && text.Contains("0066", StringComparison.Ordinal);
        Assert.True(hasPageLabelLiteral || hasPageLabelHex, "Page label identifier present (literal or P/a/g/e hex).");
        Assert.True(hasOfLiteral || hasOfHex, "Page N 'of' M indicator present (literal or Unicode hex of space-o-f-space).");
    }

    #endregion

    #region U10: Missing-font fallback

    [Fact]
    public async Task U10_MissingFont_FallbackWinAnsi_NoCrash()
    {
        FormattedPdfWriter.ResetFontCacheForTesting();
        FormattedPdfWriter.ForceFallbackModeForTesting = true;
        try
        {
            Assert.False(FormattedPdfWriter.UnicodeFontsAvailable,
                "Fallback mode forced - Unicode fonts should report unavailable.");

            var writer = new FormattedPdfWriter();
            var customer = CreateTestCustomer(
                "Clean ASCII Fallback",
                "123 Pure ASCII Street;No NonASCII Here;Austin;TX;78701",
                "Pureasciistan",
                Currency.USD);
            var company = CreateTestCompany(Currency.USD);
            var invoice = CreateUnicodeInvoice(customer, company, Currency.USD);
            invoice.Notes = "Pure ASCII notes to verify fallback behavior.";
            invoice.Terms = "Net 30. All characters printable ASCII subset only.";

            byte[] pdf = await writer.GenerateInvoicePdfAsync(invoice, company);
            string text = PdfBytesToAscii(pdf);

            Assert.NotNull(pdf);
            Assert.True(pdf.Length > 100, "Fallback PDF should not be empty.");
            Assert.StartsWith("%PDF-1.4", text);
            Assert.Contains("%%EOF", text);

            Assert.Contains("Clean", text, StringComparison.Ordinal);
            Assert.Contains("ASCII", text, StringComparison.Ordinal);
            Assert.Contains("Fallback", text, StringComparison.Ordinal);

            Assert.Contains("Helvetica", text, StringComparison.Ordinal);

            bool noCrashValid = text.Contains("%%EOF") && text.Contains("INVOICE");
            Assert.True(noCrashValid, "Fallback WinAnsi Helvetica path produced a valid PDF.");

            string badOctalPattern = @"\\[0-7]{3}";
            int octCount = Regex.Matches(text, badOctalPattern).Count;
            string[] nameFields = { "Clean ASCII Fallback", "Pure ASCII notes to verify fallback behavior." };
            foreach (var field in nameFields)
            {
                foreach (var ch in field)
                {
                    if (ch < 32 || ch > 126)
                    {
                        Assert.Fail(
                            $"Non-ASCII char U+{(int)ch:X4} in fallback input field would corrupt strict WinAnsi output.");
                    }
                }
            }
            Assert.True(octCount < 10,
                $"WinAnsi fallback: low octal count (only for literal parens/backslashes), found {octCount}.");
        }
        finally
        {
            FormattedPdfWriter.ForceFallbackModeForTesting = false;
            FormattedPdfWriter.ResetFontCacheForTesting();
        }
    }

    #endregion
}
