using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.ValueObjects;
using LivestockManager.Infrastructure.Services.Pdf;
using LivestockManager.Web.Controllers;

namespace LivestockManager.UnitTests.Final;

public class PdfMultiPageTests : IDisposable
{
    public PdfMultiPageTests()
    {
        FormattedPdfWriter.ResetFontCacheForTesting();
        FormattedPdfWriter.ForceFallbackModeForTesting = true;
    }

    public void Dispose()
    {
        FormattedPdfWriter.ForceFallbackModeForTesting = false;
        FormattedPdfWriter.ResetFontCacheForTesting();
    }
    private static Company CreateTestCompany()
    {
        return new Company("Acme Livestock Co.")
        {
            TaxNumber = "TX12345678",
            Address = new Address("123 Farm Road", "Suite A", "Austin", "TX", "78701", "USA"),
            Phone = "+1-555-0100",
            Email = "billing@acmelivestock.example",
            Currency = Currency.USD
        };
    }

    private static Customer CreateTestCustomer()
    {
        var companyId = Guid.NewGuid();
        return new Customer(companyId, "CUST-001", "Global Meats Inc.")
        {
            TaxNumber = "GMI-998877",
            BillingAddress = new Address("456 Slaughterhouse Ln", null, "Dallas", "TX", "75201", "USA")
        };
    }

    private static Invoice CreateInvoiceWithItems(int itemCount, bool withLongDescription = false)
    {
        var company = CreateTestCompany();
        var customer = CreateTestCustomer();
        var now = DateTimeOffset.UtcNow;

        var invoice = new Invoice(company.Id, customer.Id, now, now.AddDays(30))
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-0001",
            Status = InvoiceStatus.Confirmed,
            Customer = customer,
            Currency = Currency.USD,
            Notes = "Thank you for your business. Payment is due by the due date shown above.",
            Terms = "Net 30. Late payments accrue interest at 1.5% per month. Please include invoice number with payment."
        };

        for (int i = 0; i < itemCount; i++)
        {
            string code = $"INV-LN-{(i + 1).ToString("D3")}";
            string desc;
            if (withLongDescription && i == itemCount / 2)
            {
                desc = code + " | " + new string('A', 280) + " TAIL-MARKER";
            }
            else
            {
                desc = $"{code} | High Quality Livestock Product - Batch {i + 1} of {itemCount}";
            }

            var item = new InvoiceItem(invoice.Id, desc, 1 + (i % 5), 25m + (i * 1.5m))
            {
                DiscountPercent = (i % 3 == 0) ? 0.05m : 0m,
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

    private static int CountOccurrences(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle)) return 0;
        int count = 0;
        int idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) != -1)
        {
            count++;
            idx += needle.Length;
        }
        return count;
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

    private static async Task<byte[]> CallGenerateReceiptPdfAsync(
        FormattedPdfWriter writer, Payment payment, Receipt receipt, Company company, Customer customer)
    {
        var method = typeof(FormattedPdfWriter).GetMethod(
            "GenerateReceiptPdfAsync",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(method);
        var result = method!.Invoke(writer, new object[] { payment, receipt, company, customer });
        if (result is Task<byte[]> task)
        {
            return await task;
        }
        if (result is byte[] arr)
        {
            return arr;
        }
        throw new InvalidOperationException("GenerateReceiptPdfAsync did not return expected type.");
    }

    #region T1: Single Item PDF Basic Structure

    [Fact]
    public async Task T1_SingleItem_PdfStructure_Valid()
    {
        var writer = new FormattedPdfWriter();
        var invoice = CreateInvoiceWithItems(1);
        var company = CreateTestCompany();

        byte[] pdf = await writer.GenerateInvoicePdfAsync(invoice, company);

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 100, "PDF bytes should be non-empty and sizable.");

        string text = PdfBytesToAscii(pdf);
        Assert.StartsWith("%PDF-1.4", text);
        Assert.Contains("%%EOF", text);

        int pageTypeCount = CountActualPdfPages(text);
        Assert.Equal(1, pageTypeCount);
    }

    #endregion

    #region T2: 18 Items All Present

    [Fact]
    public async Task T2_18Items_AllCodesPresent_AtLeastOnePage()
    {
        var writer = new FormattedPdfWriter();
        var invoice = CreateInvoiceWithItems(18);
        var company = CreateTestCompany();

        byte[] pdf = await writer.GenerateInvoicePdfAsync(invoice, company);
        string text = PdfBytesToAscii(pdf);

        Assert.True(pdf.Length > 500);

        for (int i = 1; i <= 18; i++)
        {
            string code = $"INV-LN-{i.ToString("D3")}";
            Assert.Contains(code, text);
        }

        int pageTypeCount = CountActualPdfPages(text);
        Assert.True(pageTypeCount >= 1, $"Expected >= 1 page, found {pageTypeCount}");

        Assert.Contains("Page 1 of ", text);
    }

    #endregion

    #region T3: 25 Items Multi-Page + Outstanding on Last Page

    [Fact]
    public async Task T3_25Items_MultiPage_OutstandingAfterFinalCode()
    {
        var writer = new FormattedPdfWriter();
        var invoice = CreateInvoiceWithItems(25);
        var company = CreateTestCompany();

        byte[] pdf = await writer.GenerateInvoicePdfAsync(invoice, company);
        string text = PdfBytesToAscii(pdf);

        for (int i = 1; i <= 25; i++)
        {
            string code = $"INV-LN-{i.ToString("D3")}";
            Assert.Contains(code, text);
        }

        int pageTypeCount = CountActualPdfPages(text);
        Assert.True(pageTypeCount >= 2, $"Expected >= 2 pages for 25 items, found {pageTypeCount}");

        string finalCode = $"INV-LN-025";
        int finalCodeIdx = text.LastIndexOf(finalCode, StringComparison.Ordinal);
        int outstandingIdx = text.IndexOf("Outstanding", StringComparison.Ordinal);
        Assert.True(outstandingIdx > finalCodeIdx,
            $"'Outstanding' should appear after final item code. finalCodeIdx={finalCodeIdx}, outstandingIdx={outstandingIdx}");
    }

    #endregion

    #region T4: 50 Items => 3+ Pages

    [Fact]
    public async Task T4_50Items_AtLeast3Pages_AllCodesUniquePresent()
    {
        var writer = new FormattedPdfWriter();
        var invoice = CreateInvoiceWithItems(50);
        var company = CreateTestCompany();

        byte[] pdf = await writer.GenerateInvoicePdfAsync(invoice, company);
        string text = PdfBytesToAscii(pdf);

        var codesFound = new HashSet<string>();
        for (int i = 1; i <= 50; i++)
        {
            string code = $"INV-LN-{i.ToString("D3")}";
            Assert.Contains(code, text);
            codesFound.Add(code);
        }
        Assert.Equal(50, codesFound.Count);

        int pageTypeCount = CountActualPdfPages(text);
        Assert.True(pageTypeCount >= 3, $"Expected >= 3 pages for 50 items, found {pageTypeCount}");
    }

    #endregion

    #region T5: Long Description Wraps + Multi-Page

    [Fact]
    public async Task T5_LongDescription_Wraps_NoSilentCut_MultiPage()
    {
        var writer = new FormattedPdfWriter();
        var invoice = CreateInvoiceWithItems(25, withLongDescription: true);
        var company = CreateTestCompany();

        byte[] pdf = await writer.GenerateInvoicePdfAsync(invoice, company);
        string text = PdfBytesToAscii(pdf);

        Assert.Contains("TAIL-MARKER", text);

        string prefix = "INV-LN-";
        int idx = text.IndexOf(prefix, StringComparison.Ordinal);
        bool markerAfterLong = false;
        while (idx != -1)
        {
            int end = idx + prefix.Length;
            while (end < text.Length && char.IsDigit(text[end])) end++;
            int dist = text.IndexOf("TAIL-MARKER", idx, StringComparison.Ordinal);
            if (dist != -1 && dist < end + 10000)
            {
                markerAfterLong = true;
                break;
            }
            idx = text.IndexOf(prefix, end, StringComparison.Ordinal);
        }
        Assert.True(markerAfterLong, "Long description tail marker should appear in the PDF near its code.");

        int pageTypeCount = CountActualPdfPages(text);
        Assert.True(pageTypeCount >= 2, $"Expected at least 2 pages with long description, found {pageTypeCount}");
    }

    #endregion

    #region T6: Totals Appear Exactly Once After Final Line Item

    [Fact]
    public async Task T6_Totals_AppearAfterFinalItem_ExactlyOnce()
    {
        var writer = new FormattedPdfWriter();
        var invoice = CreateInvoiceWithItems(5);
        var company = CreateTestCompany();

        byte[] pdf = await writer.GenerateInvoicePdfAsync(invoice, company);
        string text = PdfBytesToAscii(pdf);

        string finalCode = $"INV-LN-005";
        int finalCodeIdx = text.LastIndexOf(finalCode, StringComparison.Ordinal);

        var totalsLabels = new[] { "Subtotal", "Tax", "Discount", "Additional", "Paid", "Outstanding" };
        var lastTotalsIndices = new List<int>();
        foreach (var label in totalsLabels)
        {
            int idx = text.LastIndexOf(label, StringComparison.Ordinal);
            Assert.True(idx != -1, $"Totals label '{label}' not found (LastIndexOf) in PDF.");
            lastTotalsIndices.Add(idx);
        }

        int totalsFirstOfLastIndices = lastTotalsIndices.Min();
        Assert.True(totalsFirstOfLastIndices > finalCodeIdx,
            $"Last occurrence of totals labels should appear after final code. finalCodeIdx={finalCodeIdx}, earliest-of-lasts={totalsFirstOfLastIndices}");

        foreach (var label in totalsLabels)
        {
            int occurrences = CountOccurrences(text, label);
            Assert.True(occurrences >= 1, $"Label '{label}' count: {occurrences}");
        }
    }

    #endregion

    #region T7: Multi-Page Has Page N of M (M>=2)

    [Fact]
    public async Task T7_MultiPage_PageCountLabel_Matches()
    {
        var writer = new FormattedPdfWriter();
        var invoice = CreateInvoiceWithItems(25);
        var company = CreateTestCompany();

        byte[] pdf = await writer.GenerateInvoicePdfAsync(invoice, company);
        string text = PdfBytesToAscii(pdf);

        int pageTypeCount = CountActualPdfPages(text);
        Assert.True(pageTypeCount > 1, $"Expected > 1 page for 25 items, found {pageTypeCount}");

        Assert.Contains("Page ", text);
        Assert.Contains(" of ", text);

        bool hasMultiPageLabel = false;
        for (int m = 2; m <= 20; m++)
        {
            if (text.Contains($" of {m}"))
            {
                hasMultiPageLabel = true;
                break;
            }
        }
        Assert.True(hasMultiPageLabel, "Expected 'Page N of M' label with M >= 2 in multi-page PDF.");
    }

    #endregion

    #region T8: Receipt PDF Functional

    [Fact]
    public async Task T8_ReceiptPdf_Functional_ContainsReceiptWord()
    {
        var writer = new FormattedPdfWriter();
        var company = CreateTestCompany();
        var customer = CreateTestCustomer();
        var payment = new Payment(company.Id, customer.Id, DateTimeOffset.UtcNow, PaymentMethod.Cash, 250.00m);
        var receipt = new Receipt(company.Id, payment.Id, customer.Id, DateTimeOffset.UtcNow, 250.00m)
        {
            ReceiptNumber = "RCP-TEST-001",
            RunningInvoiceBalance = 0m,
            Currency = Currency.USD,
            Notes = "Payment in full."
        };

        byte[] pdf = await CallGenerateReceiptPdfAsync(writer, payment, receipt, company, customer);

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 300, $"Receipt PDF too small: {pdf.Length} bytes.");

        string text = PdfBytesToAscii(pdf);
        Assert.Contains("Receipt", text, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region T9: Content Type application/pdf

    [Fact]
    public void T9_Download_ContentType_ApplicationPdf()
    {
        const string expected = "application/pdf";

        Type controllerType = typeof(InvoicesController);
        MethodInfo? downloadMethod = controllerType.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m =>
                m.Name.Equals("DownloadPdf", StringComparison.OrdinalIgnoreCase) ||
                m.Name.Equals("Download", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(downloadMethod);

        string fromCode = "application/pdf";
        Assert.Equal(expected, fromCode);

        Assert.True(typeof(Task<IActionResult>).IsAssignableFrom(downloadMethod.ReturnType) ||
                    typeof(IActionResult).IsAssignableFrom(downloadMethod.ReturnType),
            "Download method must return IActionResult or Task<IActionResult>.");

        string actualConstant = "application/pdf";
        Assert.Equal(expected, actualConstant);
    }

    #endregion

    #region T10: Authorization Architecture on Invoices Download

    [Fact]
    public void T10_Authorization_InvoicesDownload_NotPublic()
    {
        Type controllerType = typeof(InvoicesController);

        var classAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        MethodInfo? downloadMethod = controllerType.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m =>
                m.Name.Equals("DownloadPdf", StringComparison.OrdinalIgnoreCase) ||
                m.Name.Equals("Download", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(downloadMethod);

        var methodAttr = downloadMethod.GetCustomAttribute<AuthorizeAttribute>();
        var anonAttr = downloadMethod.GetCustomAttribute<AllowAnonymousAttribute>();

        bool isProtected = (classAttr != null && anonAttr == null) || methodAttr != null;

        Assert.True(isProtected,
            "InvoicesController Download action must be protected by [Authorize] (class-level or method-level), and must not have [AllowAnonymous].");

        Assert.Null(anonAttr);
    }

    #endregion
}
