using System.Globalization;
using System.Text;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.ValueObjects;

namespace LivestockManager.Infrastructure.Services.Pdf;

public class FormattedPdfWriter : IPdfGenerator
{
    private const float PageWidth = 612f;
    private const float PageHeight = 792f;
    private const float MarginLeft = 36f;
    private const float MarginRight = 36f;
    private const float MarginTop = 36f;
    private const float MarginBottom = 36f;
    private const float ContentWidth = PageWidth - MarginLeft - MarginRight;
    private const float HeaderReserved = 140f;
    private const float FooterReserved = 120f;
    private const float BodyHeight = PageHeight - HeaderReserved - FooterReserved;
    private const float BodyYTop = PageHeight - HeaderReserved;

    private static readonly CultureInfo EnUs = new("en-US");

    private static readonly object FontLock = new();
    private static byte[]? _cachedRegularTtf;
    private static byte[]? _cachedBoldTtf;
    private static bool? _fontsAvailable;
    private static bool _fontsInvalidPlaceholderDetected;
    private static bool _placeholderRejectWarningLogged;

    internal static bool UnicodeFontsAvailable
    {
        get
        {
            if (ForceFallbackModeForTesting) return false;
            EnsureFontsLoaded();
            return _fontsAvailable == true;
        }
    }

    internal static bool InvalidPlaceholderDetected => _fontsInvalidPlaceholderDetected;

    internal static void ResetFontCacheForTesting()
    {
        lock (FontLock)
        {
            _cachedRegularTtf = null;
            _cachedBoldTtf = null;
            _fontsAvailable = null;
            _forceFallbackMode = false;
            _fontsInvalidPlaceholderDetected = false;
            _placeholderRejectWarningLogged = false;
        }
    }

    private static bool _forceFallbackMode;

    internal static bool ForceFallbackModeForTesting
    {
        get => _forceFallbackMode;
        set
        {
            lock (FontLock)
            {
                _forceFallbackMode = value;
            }
        }
    }

    private static void EnsureFontsLoaded()
    {
        if (_fontsAvailable.HasValue) return;
        lock (FontLock)
        {
            if (_fontsAvailable.HasValue) return;
            try
            {
                var asm = typeof(FormattedPdfWriter).Assembly;
                var prefix = asm.GetName().Name + ".Resources.Fonts.";
                var regName = prefix + "NotoSans-Regular.ttf";
                var boldName = prefix + "NotoSans-Bold.ttf";
                using var regStream = asm.GetManifestResourceStream(regName);
                using var boldStream = asm.GetManifestResourceStream(boldName);
                if (regStream != null && boldStream != null)
                {
                    using var regMs = new MemoryStream();
                    using var boldMs = new MemoryStream();
                    regStream.CopyTo(regMs);
                    boldStream.CopyTo(boldMs);
                    byte[] regBytes = regMs.ToArray();
                    byte[] boldBytes = boldMs.ToArray();

                    bool regValid;
                    using (var regValStream = new MemoryStream(regBytes))
                        regValid = TrueTypeFontValidator.Validate(regValStream, out _);
                    bool boldValid;
                    using (var boldValStream = new MemoryStream(boldBytes))
                        boldValid = TrueTypeFontValidator.Validate(boldValStream, out _);

                    if (regValid && boldValid)
                    {
                        _cachedRegularTtf = regBytes;
                        _cachedBoldTtf = boldBytes;
                        _fontsAvailable = true;
                    }
                    else
                    {
                        _cachedRegularTtf = null;
                        _cachedBoldTtf = null;
                        _fontsAvailable = false;
                        _fontsInvalidPlaceholderDetected = true;
                        if (!_placeholderRejectWarningLogged)
                        {
                            _placeholderRejectWarningLogged = true;
                            try
                            {
                                Console.Error.WriteLine(
                                    "[WARN][PDF] Rejected invalid embedded TrueType font assets " +
                                    "(placeholders detected; TrueTypeFontValidator failed). " +
                                    "Unicode PDF rendering disabled; using safe basic WinAnsi Helvetica fallback. " +
                                    "Replace with genuine validated Noto Sans (Regular + Bold) to re-enable Unicode PDF output."
                                );
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                else
                {
                    _fontsAvailable = false;
                }
            }
            catch
            {
                _fontsAvailable = false;
            }
        }
    }

    internal static byte[]? RegularTtf
    {
        get
        {
            EnsureFontsLoaded();
            return _cachedRegularTtf;
        }
    }

    internal static byte[]? BoldTtf
    {
        get
        {
            EnsureFontsLoaded();
            return _cachedBoldTtf;
        }
    }

    public Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice, Company company)
    {
        var customerName = string.Empty;
        var customerAddress = string.Empty;
        var customerTaxNumber = string.Empty;

        if (invoice.Customer != null)
        {
            customerName = invoice.Customer.Name;
            customerAddress = FormatAddress(invoice.Customer.BillingAddress);
            customerTaxNumber = invoice.Customer.TaxNumber ?? string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(invoice.CustomerSnapshot))
        {
            var parts = invoice.CustomerSnapshot.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 1 && string.IsNullOrWhiteSpace(customerName)) customerName = parts[0].Trim();
            if (parts.Length >= 3 && string.IsNullOrWhiteSpace(customerAddress))
            {
                var sb = new List<string>();
                for (int i = 2; i < Math.Min(parts.Length, 8); i++)
                    if (!string.IsNullOrWhiteSpace(parts[i])) sb.Add(parts[i].Trim());
                customerAddress = string.Join("\n", sb);
            }
            if (parts.Length >= 2 && string.IsNullOrWhiteSpace(customerTaxNumber)) customerTaxNumber = parts[1].Trim();
        }

        var companyAddress = FormatAddress(company.Address);
        var currencySymbol = GetCurrencySymbol(invoice.Currency);

        return Task.FromResult(BuildMultiPageInvoicePdf(
            invoice, company, customerName, customerAddress, customerTaxNumber,
            companyAddress, currencySymbol));
    }

    #region ReceiptHelper

    public async Task<byte[]> GenerateReceiptPdfAsync(
        Payment payment, Receipt receipt, Company company, Customer customer)
    {
        var companyAddress = FormatAddress(company.Address);
        var customerAddress = FormatAddress(customer.BillingAddress);
        var currencySymbol = GetCurrencySymbol(receipt.Currency);

        return await Task.FromResult(BuildReceiptPdf(
            payment, receipt, company, customer, companyAddress, customerAddress, currencySymbol));
    }

    #endregion

    #region Multi-Page Invoice Engine

    private sealed class PageLayout
    {
        public List<int> ItemIndices { get; } = new List<int>();
    }

    private static byte[] BuildMultiPageInvoicePdf(
        Invoice invoice, Company company,
        string customerName, string customerAddress, string customerTaxNumber,
        string companyAddress, string currencySymbol)
    {
        var items = invoice.Items ?? new List<InvoiceItem>();

        var pageLayouts = Pass1Layout(items);
        int totalPages = Math.Max(1, pageLayouts.Count);

        bool useUnicode = UnicodeFontsAvailable;
        var pageContentStreams = new List<byte[]>();
        for (int p = 0; p < totalPages; p++)
        {
            bool isLastPage = (p == totalPages - 1);
            var layout = pageLayouts[p];
            var sb = new StringBuilder();
            RenderInvoicePage(sb, p + 1, totalPages, isLastPage, layout, invoice, company,
                customerName, customerAddress, customerTaxNumber, companyAddress, currencySymbol, useUnicode);
            if (useUnicode)
                pageContentStreams.Add(Encoding.ASCII.GetBytes(sb.ToString()));
            else
                pageContentStreams.Add(Encoding.ASCII.GetBytes(sb.ToString()));
        }

        return AssembleMultiPagePdf(pageContentStreams, useUnicode);
    }

    private static List<PageLayout> Pass1Layout(List<InvoiceItem> items)
    {
        var pages = new List<PageLayout>();
        var current = new PageLayout();
        float used = 0;

        for (int i = 0; i < items.Count; i++)
        {
            float rowH = MeasureRow(items[i]);
            if (current.ItemIndices.Count > 0 && used + rowH > BodyHeight)
            {
                pages.Add(current);
                current = new PageLayout();
                used = 0;
            }
            current.ItemIndices.Add(i);
            used += rowH;
        }

        if (current.ItemIndices.Count > 0 || pages.Count == 0)
            pages.Add(current);

        return pages;
    }

    private static float MeasureRow(InvoiceItem item)
    {
        const int descMaxChars = 67;
        const int priceColChars = 18;
        const float baseHeight = 22f;
        const float perLineExtra = 24f;

        string desc = item.Description ?? string.Empty;
        string qty = item.Quantity.ToString("N2", EnUs);
        string price = $"{item.UnitPrice:N2}";
        string disc = $"{item.DiscountPercent * 100:N1}%";
        string tax = $"{item.TaxPercent * 100:N1}%";
        string total = $"{item.LineTotal:N2}";

        int descLines = CountWrappedLines(desc, descMaxChars);
        int qtyLines = CountWrappedLines(qty, priceColChars);
        int priceLines = CountWrappedLines(price, priceColChars);
        int discLines = CountWrappedLines(disc, priceColChars);
        int taxLines = CountWrappedLines(tax, priceColChars);
        int totalLines = CountWrappedLines(total, priceColChars);

        int maxLines = new[] { descLines, qtyLines, priceLines, discLines, taxLines, totalLines }.Max();
        if (maxLines < 1) maxLines = 1;

        return baseHeight + perLineExtra * (maxLines - 1);
    }

    private static int CountWrappedLines(string text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text)) return 1;
        int count = 0;
        foreach (var hardLine in text.Split('\n'))
        {
            var line = hardLine.TrimEnd('\r');
            if (line.Length == 0) { count++; continue; }
            count += (line.Length + maxChars - 1) / maxChars;
        }
        return Math.Max(1, count);
    }

    private static void RenderInvoicePage(
        StringBuilder content, int pageNum, int totalPages, bool isLastPage,
        PageLayout layout, Invoice invoice, Company company,
        string customerName, string customerAddress, string customerTaxNumber,
        string companyAddress, string currencySymbol, bool useUnicode)
    {
        content.AppendLine("q");
        RenderInvoiceHeader(content, invoice, company, customerName, customerAddress, customerTaxNumber, companyAddress, useUnicode);
        float y = BodyYTop;

        float col1X = MarginLeft;
        float col2X = MarginLeft + 240;
        float col3X = col2X + 55;
        float col4X = col3X + 65;
        float col5X = col4X + 55;
        float col7X = PageWidth - MarginRight;

        content.AppendLine("BT");
        DrawTextLeft(content, col1X, y, "Description", BoldFontAlias(useUnicode), 9, useUnicode);
        DrawTextRight(content, col2X - 6, y, "Qty", BoldFontAlias(useUnicode), 9, useUnicode);
        DrawTextRight(content, col3X - 6, y, "Unit Price", BoldFontAlias(useUnicode), 9, useUnicode);
        DrawTextRight(content, col4X - 6, y, "Disc %", BoldFontAlias(useUnicode), 9, useUnicode);
        DrawTextRight(content, col5X - 6, y, "Tax %", BoldFontAlias(useUnicode), 9, useUnicode);
        DrawTextRight(content, col7X - 6, y, "Line Total", BoldFontAlias(useUnicode), 9, useUnicode);
        content.AppendLine("ET");
        y -= 4;

        DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
        y -= 10;

        if (layout.ItemIndices.Count == 0 && isLastPage)
        {
            content.AppendLine("BT");
            DrawTextLeft(content, col1X, y, "No items.", RegularFontAlias(useUnicode), 9, useUnicode);
            content.AppendLine("ET");
            y -= 20;
        }
        else
        {
            content.AppendLine("BT");
            foreach (var idx in layout.ItemIndices)
            {
                var item = invoice.Items[idx];
                float rowH = MeasureRow(item);
                float rowTop = y;
                float rowBottom = y - rowH + 10;

                string desc = item.Description ?? string.Empty;
                var descLines = WrapText(desc, 67).ToList();
                float textY = rowTop - 12;
                for (int l = 0; l < descLines.Count; l++)
                {
                    DrawTextLeft(content, col1X, textY, descLines[l], RegularFontAlias(useUnicode), 9, useUnicode);
                    textY -= 12;
                }

                float valY = rowTop - 12;
                DrawTextRight(content, col2X - 6, valY, item.Quantity.ToString("N2", EnUs), RegularFontAlias(useUnicode), 9, useUnicode);
                DrawTextRight(content, col3X - 6, valY, $"{currencySymbol}{item.UnitPrice:N2}", RegularFontAlias(useUnicode), 9, useUnicode);
                DrawTextRight(content, col4X - 6, valY, $"{item.DiscountPercent * 100:N1}%", RegularFontAlias(useUnicode), 9, useUnicode);
                DrawTextRight(content, col5X - 6, valY, $"{item.TaxPercent * 100:N1}%", RegularFontAlias(useUnicode), 9, useUnicode);
                DrawTextRight(content, col7X - 6, valY, $"{currencySymbol}{item.LineTotal:N2}", RegularFontAlias(useUnicode), 9, useUnicode);

                y = rowBottom - 2;
                DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
                y -= 6;
            }
            content.AppendLine("ET");
            y -= 6;
        }

        if (isLastPage)
        {
            y -= 4;
            RenderInvoiceTotals(content, ref y, invoice, currencySymbol, useUnicode);
            y -= 14;
            DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
            y -= 14;
            RenderNotesAndTerms(content, ref y, invoice.Notes, invoice.Terms, useUnicode);
        }

        RenderInvoiceFooter(content, pageNum, totalPages, useUnicode);
        content.AppendLine("Q");
    }

    private static string RegularFontAlias(bool useUnicode) => useUnicode ? "/F3" : "/F1";
    private static string BoldFontAlias(bool useUnicode) => useUnicode ? "/F4" : "/F2";

    private static void RenderInvoiceHeader(
        StringBuilder content, Invoice invoice, Company company,
        string customerName, string customerAddress, string customerTaxNumber,
        string companyAddress, bool useUnicode)
    {
        float y = PageHeight - MarginTop;
        y -= 4;
        DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
        y -= 10;

        content.AppendLine("BT");
        content.AppendLine($"{BoldFontAlias(useUnicode)} 22 Tf");
        content.AppendLine($"1 0 0 1 {MarginLeft.ToString(EnUs)} {(y - 20).ToString(EnUs)} Tm");
        WriteText(content, "INVOICE", useUnicode);
        content.AppendLine("ET");
        y -= 30;

        content.AppendLine("BT");
        content.AppendLine($"{RegularFontAlias(useUnicode)} 9 Tf");
        float labelX = PageWidth - MarginRight - 190;
        float valX = labelX + 75;

        DrawTextRight(content, labelX, y, "Invoice #:", RegularFontAlias(useUnicode), 9, useUnicode);
        DrawTextLeft(content, valX, y, invoice.InvoiceNumber ?? "-", BoldFontAlias(useUnicode), 9, useUnicode);
        y -= 13;
        DrawTextRight(content, labelX, y, "Date:", RegularFontAlias(useUnicode), 9, useUnicode);
        DrawTextLeft(content, valX, y, invoice.InvoiceDate.ToString("yyyy-MM-dd", EnUs), RegularFontAlias(useUnicode), 9, useUnicode);
        y -= 13;
        DrawTextRight(content, labelX, y, "Due Date:", RegularFontAlias(useUnicode), 9, useUnicode);
        DrawTextLeft(content, valX, y, invoice.DueDate.ToString("yyyy-MM-dd", EnUs), RegularFontAlias(useUnicode), 9, useUnicode);
        y -= 13;
        DrawTextRight(content, labelX, y, "Status:", RegularFontAlias(useUnicode), 9, useUnicode);
        DrawTextLeft(content, valX, y, invoice.Status.ToString(), BoldFontAlias(useUnicode), 9, useUnicode);
        content.AppendLine("ET");
        y -= 18;

        DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
        y -= 14;

        float fromX = MarginLeft;
        float billX = MarginLeft + ContentWidth * 0.52f;
        float sectionY = y;
        float billSectionY = y;

        content.AppendLine("BT");
        DrawTextLeft(content, fromX, sectionY, "FROM", BoldFontAlias(useUnicode), 9, useUnicode);
        sectionY -= 13;
        DrawTextLeft(content, fromX, sectionY, company.Name, BoldFontAlias(useUnicode), 10, useUnicode);
        sectionY -= 12;
        DrawMultiLineLeft(content, fromX, ref sectionY, companyAddress, RegularFontAlias(useUnicode), 9, 11, useUnicode);
        if (!string.IsNullOrWhiteSpace(company.TaxNumber))
        {
            DrawTextLeft(content, fromX, sectionY, $"Tax: {company.TaxNumber}", RegularFontAlias(useUnicode), 9, useUnicode);
            sectionY -= 11;
        }
        if (!string.IsNullOrWhiteSpace(company.Phone))
        {
            DrawTextLeft(content, fromX, sectionY, $"Tel: {company.Phone}", RegularFontAlias(useUnicode), 9, useUnicode);
            sectionY -= 11;
        }
        if (!string.IsNullOrWhiteSpace(company.Email))
        {
            DrawTextLeft(content, fromX, sectionY, $"Email: {company.Email}", RegularFontAlias(useUnicode), 9, useUnicode);
            sectionY -= 11;
        }

        DrawTextLeft(content, billX, billSectionY, "BILL TO", BoldFontAlias(useUnicode), 9, useUnicode);
        billSectionY -= 13;
        DrawTextLeft(content, billX, billSectionY, customerName, BoldFontAlias(useUnicode), 10, useUnicode);
        billSectionY -= 12;
        DrawMultiLineLeft(content, billX, ref billSectionY, customerAddress, RegularFontAlias(useUnicode), 9, 11, useUnicode);
        if (!string.IsNullOrWhiteSpace(customerTaxNumber))
        {
            DrawTextLeft(content, billX, billSectionY, $"Tax: {customerTaxNumber}", RegularFontAlias(useUnicode), 9, useUnicode);
            billSectionY -= 11;
        }
        content.AppendLine("ET");

        y = Math.Min(sectionY, billSectionY) - 14;
        DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
    }

    private static void RenderInvoiceFooter(StringBuilder content, int pageNum, int totalPages, bool useUnicode)
    {
        float y = MarginBottom + 12;
        content.AppendLine("BT");
        DrawTextRight(content, PageWidth - MarginRight, y, $"Page {pageNum} of {totalPages}", RegularFontAlias(useUnicode), 8, useUnicode);
        content.AppendLine("ET");
    }

    private static void RenderInvoiceTotals(StringBuilder content, ref float y, Invoice invoice, string currencySymbol, bool useUnicode)
    {
        float boxX = PageWidth - MarginRight - 260;
        float boxWidth = 260;
        float boxY = y;
        int rows = 7;
        float rowHeight = 15;
        float boxBottom = boxY - (rows * rowHeight) - 10;

        DrawRect(content, boxX, boxBottom, boxWidth, boxY - boxBottom);

        content.AppendLine("BT");
        float ty = boxY - 13;
        DrawTextRight(content, boxX + boxWidth - 150, ty, "Subtotal:", RegularFontAlias(useUnicode), 9, useUnicode);
        DrawTextRight(content, boxX + boxWidth - 12, ty, $"{currencySymbol}{invoice.Subtotal:N2}", RegularFontAlias(useUnicode), 9, useUnicode);
        ty -= rowHeight;
        DrawTextRight(content, boxX + boxWidth - 150, ty, "Discount Total:", RegularFontAlias(useUnicode), 9, useUnicode);
        DrawTextRight(content, boxX + boxWidth - 12, ty, $"{currencySymbol}{invoice.DiscountTotal:N2}", RegularFontAlias(useUnicode), 9, useUnicode);
        ty -= rowHeight;
        DrawTextRight(content, boxX + boxWidth - 150, ty, "Tax Total:", RegularFontAlias(useUnicode), 9, useUnicode);
        DrawTextRight(content, boxX + boxWidth - 12, ty, $"{currencySymbol}{invoice.TaxTotal:N2}", RegularFontAlias(useUnicode), 9, useUnicode);
        ty -= rowHeight;
        DrawTextRight(content, boxX + boxWidth - 150, ty, "Additional:", RegularFontAlias(useUnicode), 9, useUnicode);
        DrawTextRight(content, boxX + boxWidth - 12, ty, $"{currencySymbol}{invoice.ChargeTotal:N2}", RegularFontAlias(useUnicode), 9, useUnicode);
        ty -= rowHeight;
        DrawLine(content, boxX + 10, ty + 8, boxX + boxWidth - 10, ty + 8);
        ty -= 2;
        DrawTextRight(content, boxX + boxWidth - 150, ty, "GRAND TOTAL:", BoldFontAlias(useUnicode), 9, useUnicode);
        DrawTextRight(content, boxX + boxWidth - 12, ty, $"{currencySymbol}{invoice.GrandTotal:N2}", BoldFontAlias(useUnicode), 9, useUnicode);
        ty -= rowHeight;
        DrawTextRight(content, boxX + boxWidth - 150, ty, "Paid Amount:", RegularFontAlias(useUnicode), 9, useUnicode);
        DrawTextRight(content, boxX + boxWidth - 12, ty, $"{currencySymbol}{invoice.PaidAmount:N2}", RegularFontAlias(useUnicode), 9, useUnicode);
        ty -= rowHeight;
        DrawTextRight(content, boxX + boxWidth - 150, ty, "Outstanding:", BoldFontAlias(useUnicode), 9, useUnicode);
        DrawTextRight(content, boxX + boxWidth - 12, ty, $"{currencySymbol}{invoice.OutstandingAmount:N2}", BoldFontAlias(useUnicode), 9, useUnicode);
        content.AppendLine("ET");

        y = boxBottom;
    }

    private static void RenderNotesAndTerms(StringBuilder content, ref float y, string? notes, string? terms, bool useUnicode)
    {
        content.AppendLine("BT");
        if (!string.IsNullOrWhiteSpace(notes))
        {
            DrawTextLeft(content, MarginLeft, y, "NOTES:", BoldFontAlias(useUnicode), 9, useUnicode);
            y -= 13;
            var wrapped = WrapText(notes!, 95);
            foreach (var line in wrapped)
            {
                if (y < MarginBottom + 30) break;
                DrawTextLeft(content, MarginLeft, y, line, RegularFontAlias(useUnicode), 8, useUnicode);
                y -= 11;
            }
            y -= 6;
        }

        if (!string.IsNullOrWhiteSpace(terms) && y > MarginBottom + 30)
        {
            DrawTextLeft(content, MarginLeft, y, "TERMS & CONDITIONS:", BoldFontAlias(useUnicode), 9, useUnicode);
            y -= 13;
            var wrapped = WrapText(terms!, 95);
            foreach (var line in wrapped)
            {
                if (y < MarginBottom + 12) break;
                DrawTextLeft(content, MarginLeft, y, line, RegularFontAlias(useUnicode), 8, useUnicode);
                y -= 11;
            }
        }
        content.AppendLine("ET");
    }

    #endregion

    #region PDF Assembly with Unicode Type0 Font Support

    private static string ToUtf16BeHex(string s)
    {
        if (string.IsNullOrEmpty(s)) return "<>";
        var bytes = Encoding.BigEndianUnicode.GetBytes(s);
        var sb = new StringBuilder(bytes.Length * 2 + 2);
        sb.Append('<');
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("X2"));
        }
        sb.Append('>');
        return sb.ToString();
    }

    private static void WriteText(StringBuilder content, string text, bool useUnicode)
    {
        if (useUnicode)
        {
            content.AppendLine(ToUtf16BeHex(text) + " Tj");
        }
        else
        {
            string safe;
            var sb = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '(': sb.Append("\\("); break;
                    case ')': sb.Append("\\)"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c <= 126 && c >= 32) sb.Append(c);
                        else sb.Append('?');
                        break;
                }
            }
            safe = sb.ToString();
            content.AppendLine($"({safe}) Tj");
        }
    }

    private static byte[] AssembleMultiPagePdf(List<byte[]> pageContentStreams, bool useUnicode)
    {
        int numPages = pageContentStreams.Count;

        var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);

        writer.Write(Encoding.ASCII.GetBytes("%PDF-1.4\n"));
        writer.Write(new byte[] { 0xE2, 0xE3, 0xCF, 0xD3, 0x0A });

        int nextObjNum = 1;
        int catalogObj = nextObjNum++;
        int pagesObj = nextObjNum++;

        var pageObjNums = new List<int>();
        var contentObjNums = new List<int>();
        for (int i = 0; i < numPages; i++)
        {
            pageObjNums.Add(nextObjNum++);
            contentObjNums.Add(nextObjNum++);
        }

        int f1Obj, f2Obj, f3Type0Obj = 0, f4Type0Obj = 0;
        int regCidObj = 0, regDescObj = 0, regFile2Obj = 0, regToUnicodeObj = 0;
        int boldCidObj = 0, boldDescObj = 0, boldFile2Obj = 0, boldToUnicodeObj = 0;

        if (useUnicode)
        {
            f3Type0Obj = nextObjNum++;
            regCidObj = nextObjNum++;
            regDescObj = nextObjNum++;
            regFile2Obj = nextObjNum++;
            regToUnicodeObj = nextObjNum++;

            f4Type0Obj = nextObjNum++;
            boldCidObj = nextObjNum++;
            boldDescObj = nextObjNum++;
            boldFile2Obj = nextObjNum++;
            boldToUnicodeObj = nextObjNum++;

            f1Obj = f3Type0Obj;
            f2Obj = f4Type0Obj;
        }
        else
        {
            f1Obj = nextObjNum++;
            f2Obj = nextObjNum++;
        }

        var objectBytes = new List<byte[]>();
        var objectNumbers = new List<int>();

        void AddDictObj(int num, string dict)
        {
            objectNumbers.Add(num);
            var data = Encoding.ASCII.GetBytes($"{num} 0 obj\n{dict}\nendobj\n");
            objectBytes.Add(data);
        }

        void AddStreamObj(int num, string headerDict, byte[] streamData, bool isBinary, string afterStream = "")
        {
            objectNumbers.Add(num);
            var msObj = new MemoryStream();
            var bwObj = new BinaryWriter(msObj);
            bwObj.Write(Encoding.ASCII.GetBytes($"{num} 0 obj\n{headerDict}\nstream\n"));
            bwObj.Write(streamData);
            if (!isBinary || streamData.Length == 0 || streamData[streamData.Length - 1] != (byte)'\n')
            {
                bwObj.Write(Encoding.ASCII.GetBytes("\n"));
            }
            bwObj.Write(Encoding.ASCII.GetBytes($"endstream\nendobj\n"));
            if (!string.IsNullOrEmpty(afterStream))
            {
                bwObj.Write(Encoding.ASCII.GetBytes(afterStream));
            }
            objectBytes.Add(msObj.ToArray());
        }

        AddDictObj(catalogObj, $"<< /Type /Catalog /Pages {pagesObj} 0 R /OpenAction [{pageObjNums[0]} 0 R /XYZ null null null] >>");

        var kidsSb = new StringBuilder();
        kidsSb.Append("<< /Type /Pages /Kids [");
        for (int i = 0; i < numPages; i++)
        {
            if (i > 0) kidsSb.Append(' ');
            kidsSb.Append($"{pageObjNums[i]} 0 R");
        }
        kidsSb.Append($"] /Count {numPages} >>");
        AddDictObj(pagesObj, kidsSb.ToString());

        for (int i = 0; i < numPages; i++)
        {
            string fontRes;
            if (useUnicode)
            {
                fontRes = $"/Font << /F1 {f1Obj} 0 R /F2 {f2Obj} 0 R /F3 {f3Type0Obj} 0 R /F4 {f4Type0Obj} 0 R >>";
            }
            else
            {
                fontRes = $"/Font << /F1 {f1Obj} 0 R /F2 {f2Obj} 0 R >>";
            }
            AddDictObj(pageObjNums[i],
                $"<< /Type /Page /Parent {pagesObj} 0 R /MediaBox [0 0 {PageWidth.ToString(EnUs)} {PageHeight.ToString(EnUs)}] /Contents {contentObjNums[i]} 0 R /Resources << {fontRes} >> >>");

            var cb = pageContentStreams[i];
            AddStreamObj(contentObjNums[i], $"<< /Length {cb.Length} >>", cb, false);
        }

        if (useUnicode)
        {
            var regTtf = RegularTtf!;
            var boldTtf = BoldTtf!;

            BuildUnicodeFontObjects(
                AddDictObj, AddStreamObj,
                f3Type0Obj, regCidObj, regDescObj, regFile2Obj, regToUnicodeObj,
                regTtf, "NotoSans", false);

            BuildUnicodeFontObjects(
                AddDictObj, AddStreamObj,
                f4Type0Obj, boldCidObj, boldDescObj, boldFile2Obj, boldToUnicodeObj,
                boldTtf, "NotoSansBold", true);
        }
        else
        {
            AddDictObj(f1Obj, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
            AddDictObj(f2Obj, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");
        }

        var ordered = new List<(int Num, byte[] Bytes)>(objectNumbers.Count);
        for (int i = 0; i < objectNumbers.Count; i++)
        {
            ordered.Add((objectNumbers[i], objectBytes[i]));
        }
        ordered.Sort((a, b) => a.Num.CompareTo(b.Num));

        var xref = new StringBuilder();
        xref.AppendLine("xref");
        int xrefSize = ordered.Count + 1;
        xref.AppendLine("0 " + xrefSize);
        xref.AppendLine("0000000000 65535 f ");

        var offsets = new List<int>();
        writer.Flush();
        var runningOffset = (int)ms.Length;

        foreach (var obj in ordered)
        {
            offsets.Add(runningOffset);
            writer.Write(obj.Bytes);
            runningOffset += obj.Bytes.Length;
        }

        foreach (var offset in offsets)
        {
            xref.AppendLine($"{offset:0000000000} 00000 n ");
        }

        var xrefStart = (int)ms.Length;
        writer.Write(Encoding.ASCII.GetBytes(xref.ToString()));
        writer.Write(Encoding.ASCII.GetBytes(
            $"trailer\n<< /Size {xrefSize} /Root {catalogObj} 0 R >>\nstartxref\n{xrefStart}\n%%EOF\n"));
        writer.Flush();

        return ms.ToArray();
    }

    private static void BuildUnicodeFontObjects(
        Action<int, string> addDict,
        Action<int, string, byte[], bool, string> addStream,
        int type0Obj, int cidObj, int descObj, int file2Obj, int toUnicodeObj,
        byte[] ttfBytes, string baseFontName, bool isBold)
    {
        int length1 = ttfBytes.Length;
        int flags = isBold ? 4 + 32 : 4;
        int stemV = isBold ? 140 : 80;
        int capHeight = 700;
        int ascent = 800;
        int descent = -200;

        addStream(file2Obj,
            $"<< /Length {ttfBytes.Length} /Length1 {length1} >>",
            ttfBytes, true, "");

        addDict(descObj,
            $"<< /Type /FontDescriptor /FontName /{baseFontName} /Flags {flags} " +
            $"/FontBBox [-100 -200 1100 850] /ItalicAngle 0 /Ascent {ascent} /Descent {descent} " +
            $"/CapHeight {capHeight} /StemV {stemV} /FontFile2 {file2Obj} 0 R >>");

        addDict(cidObj,
            $"<< /Type /Font /Subtype /CIDFontType2 /BaseFont /{baseFontName} " +
            $"/CIDSystemInfo << /Registry (Adobe) /Ordering (Identity) /Supplement 0 >> " +
            $"/FontDescriptor {descObj} 0 R /DW 500 /W [0 [500]] >>");

        string cmapDataStr =
            "/CIDInit /ProcSet findresource begin\n" +
            "12 dict begin\n" +
            "begincmap\n" +
            "/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def\n" +
            "/CMapName /Adobe-Identity-UCS def\n" +
            "/CMapType 2 def\n" +
            "1 begincodespacerange\n" +
            "<0000> <FFFF>\n" +
            "endcodespacerange\n" +
            "1 beginbfrange\n" +
            "<0000> <FFFF> <0000>\n" +
            "endbfrange\n" +
            "endcmap\n" +
            "CMapName currentdict /CMap defineresource pop\n" +
            "end\n" +
            "end";
        var cmapBytes = Encoding.ASCII.GetBytes(cmapDataStr);
        addStream(toUnicodeObj,
            $"<< /Length {cmapBytes.Length} >>",
            cmapBytes, false, "");

        addDict(type0Obj,
            $"<< /Type /Font /Subtype /Type0 /BaseFont /{baseFontName} " +
            $"/DescendantFonts [{cidObj} 0 R] /Encoding /Identity-H /ToUnicode {toUnicodeObj} 0 R >>");
    }

    #endregion

    #region Single-Page Receipt Builder

    private static byte[] BuildReceiptPdf(
        Payment payment, Receipt receipt, Company company, Customer customer,
        string companyAddress, string customerAddress, string currencySymbol)
    {
        bool useUnicode = UnicodeFontsAvailable;
        var content = new StringBuilder();
        float y = PageHeight - MarginTop;

        content.AppendLine("q");

        y -= 4;
        DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
        y -= 10;

        content.AppendLine("BT");
        content.AppendLine($"{BoldFontAlias(useUnicode)} 22 Tf");
        content.AppendLine($"1 0 0 1 {MarginLeft.ToString(EnUs)} {(y - 20).ToString(EnUs)} Tm");
        WriteText(content, "RECEIPT", useUnicode);
        content.AppendLine("ET");
        y -= 30;

        content.AppendLine("BT");
        content.AppendLine($"{RegularFontAlias(useUnicode)} 9 Tf");
        float labelX = PageWidth - MarginRight - 200;
        float valX = labelX + 85;

        DrawTextRight(content, labelX, y, "Receipt #:", RegularFontAlias(useUnicode), 9, useUnicode);
        DrawTextLeft(content, valX, y, receipt.ReceiptNumber ?? "-", BoldFontAlias(useUnicode), 9, useUnicode);
        y -= 13;
        DrawTextRight(content, labelX, y, "Receipt Date:", RegularFontAlias(useUnicode), 9, useUnicode);
        DrawTextLeft(content, valX, y, receipt.ReceiptDate.ToString("yyyy-MM-dd", EnUs), RegularFontAlias(useUnicode), 9, useUnicode);
        y -= 13;
        DrawTextRight(content, labelX, y, "Payment Date:", RegularFontAlias(useUnicode), 9, useUnicode);
        DrawTextLeft(content, valX, y, payment.PaymentDate.ToString("yyyy-MM-dd", EnUs), RegularFontAlias(useUnicode), 9, useUnicode);
        y -= 13;
        if (payment.InvoiceId.HasValue)
        {
            DrawTextRight(content, labelX, y, "Invoice Ref:", RegularFontAlias(useUnicode), 9, useUnicode);
            var invRef = payment.Invoice?.InvoiceNumber ?? payment.InvoiceId.Value.ToString("N")[..8];
            DrawTextLeft(content, valX, y, invRef, RegularFontAlias(useUnicode), 9, useUnicode);
            y -= 13;
        }
        DrawTextRight(content, labelX, y, "Method:", RegularFontAlias(useUnicode), 9, useUnicode);
        DrawTextLeft(content, valX, y, payment.Method.ToString(), RegularFontAlias(useUnicode), 9, useUnicode);
        content.AppendLine("ET");
        y -= 18;

        DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
        y -= 14;

        float fromX = MarginLeft;
        float custX = MarginLeft + ContentWidth * 0.52f;
        float sectionY = y;
        float custSectionY = y;

        content.AppendLine("BT");
        DrawTextLeft(content, fromX, sectionY, "ISSUED BY", BoldFontAlias(useUnicode), 9, useUnicode);
        sectionY -= 13;
        DrawTextLeft(content, fromX, sectionY, company.Name, BoldFontAlias(useUnicode), 10, useUnicode);
        sectionY -= 12;
        DrawMultiLineLeft(content, fromX, ref sectionY, companyAddress, RegularFontAlias(useUnicode), 9, 11, useUnicode);
        if (!string.IsNullOrWhiteSpace(company.TaxNumber))
        {
            DrawTextLeft(content, fromX, sectionY, $"Tax: {company.TaxNumber}", RegularFontAlias(useUnicode), 9, useUnicode);
            sectionY -= 11;
        }

        DrawTextLeft(content, custX, custSectionY, "RECEIVED FROM", BoldFontAlias(useUnicode), 9, useUnicode);
        custSectionY -= 13;
        DrawTextLeft(content, custX, custSectionY, customer.Name, BoldFontAlias(useUnicode), 10, useUnicode);
        custSectionY -= 12;
        DrawMultiLineLeft(content, custX, ref custSectionY, customerAddress, RegularFontAlias(useUnicode), 9, 11, useUnicode);
        if (!string.IsNullOrWhiteSpace(customer.TaxNumber))
        {
            DrawTextLeft(content, custX, custSectionY, $"Tax: {customer.TaxNumber}", RegularFontAlias(useUnicode), 9, useUnicode);
            custSectionY -= 11;
        }
        content.AppendLine("ET");

        y = Math.Min(sectionY, custSectionY) - 18;
        DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
        y -= 20;

        float totalsBoxX = PageWidth - MarginRight - 260;
        float totalsBoxWidth = 260;
        float boxY = y - 10;
        float boxBottom = boxY - 90;

        DrawRect(content, totalsBoxX, boxBottom, totalsBoxWidth, boxY - boxBottom);

        content.AppendLine("BT");
        float ty = boxY - 16;
        DrawTextRight(content, totalsBoxX + totalsBoxWidth - 150, ty, "AMOUNT RECEIVED:", BoldFontAlias(useUnicode), 9, useUnicode);
        DrawTextRight(content, totalsBoxX + totalsBoxWidth - 12, ty,
            $"{currencySymbol}{receipt.AmountReceived:N2}", BoldFontAlias(useUnicode), 9, useUnicode);
        ty -= 16;
        DrawLine(content, totalsBoxX + 10, ty + 6, totalsBoxX + totalsBoxWidth - 10, ty + 6);
        ty -= 2;
        DrawTextRight(content, totalsBoxX + totalsBoxWidth - 150, ty, "Running Balance:", RegularFontAlias(useUnicode), 9, useUnicode);
        DrawTextRight(content, totalsBoxX + totalsBoxWidth - 12, ty,
            $"{currencySymbol}{receipt.RunningInvoiceBalance:N2}", RegularFontAlias(useUnicode), 9, useUnicode);
        content.AppendLine("ET");

        y = boxBottom - 16;

        DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
        y -= 14;

        RenderNotesAndTerms(content, ref y, receipt.Notes, null, useUnicode);

        RenderInvoiceFooter(content, 1, 1, useUnicode);

        content.AppendLine("Q");

        var contentBytes = Encoding.ASCII.GetBytes(content.ToString());

        return AssembleMultiPagePdf(new List<byte[]> { contentBytes }, useUnicode);
    }

    #endregion

    #region Drawing Helpers (Graphics)

    private static void DrawLine(StringBuilder content, float x1, float y1, float x2, float y2)
    {
        content.AppendLine("0.5 w");
        content.AppendLine("0 0 0 RG");
        content.AppendLine($"{x1.ToString(EnUs)} {y1.ToString(EnUs)} m");
        content.AppendLine($"{x2.ToString(EnUs)} {y2.ToString(EnUs)} l");
        content.AppendLine("S");
    }

    private static void DrawRect(StringBuilder content, float x, float y, float w, float h)
    {
        content.AppendLine("0.5 w");
        content.AppendLine("0 0 0 RG");
        content.AppendLine($"{x.ToString(EnUs)} {y.ToString(EnUs)} {w.ToString(EnUs)} {h.ToString(EnUs)} re");
        content.AppendLine("S");
    }

    #endregion

    #region Drawing Helpers (Text - all use absolute Tm positioning)

    private static void DrawTextLeft(StringBuilder content, float x, float y, string text, string font, int size, bool useUnicode)
    {
        var fontName = font.Replace("/", "");
        content.AppendLine($"/{fontName} {size} Tf");
        content.AppendLine($"1 0 0 1 {x.ToString(EnUs)} {y.ToString(EnUs)} Tm");
        WriteText(content, text, useUnicode);
    }

    private static void DrawTextRight(StringBuilder content, float x, float y, string text, string font, int size, bool useUnicode)
    {
        var fontName = font.Replace("/", "");
        content.AppendLine($"/{fontName} {size} Tf");
        content.AppendLine($"1 0 0 1 {x.ToString(EnUs)} {y.ToString(EnUs)} Tm");
        WriteText(content, text, useUnicode);
    }

    private static void DrawMultiLineLeft(StringBuilder content, float x, ref float y,
        string text, string font, int size, int lineGap, bool useUnicode)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            DrawTextLeft(content, x, y, line, font, size, useUnicode);
            y -= lineGap;
        }
    }

    #endregion

    #region Utility Helpers

    private static string FormatAddress(Address? address)
    {
        if (address == null) return string.Empty;
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(address.Street1)) parts.Add(address.Street1);
        if (!string.IsNullOrWhiteSpace(address.Street2)) parts.Add(address.Street2);
        var cityLine = new List<string>();
        if (!string.IsNullOrWhiteSpace(address.City)) cityLine.Add(address.City!);
        if (!string.IsNullOrWhiteSpace(address.State)) cityLine.Add(address.State!);
        if (!string.IsNullOrWhiteSpace(address.PostalCode)) cityLine.Add(address.PostalCode!);
        if (cityLine.Count > 0) parts.Add(string.Join(", ", cityLine));
        if (!string.IsNullOrWhiteSpace(address.Country)) parts.Add(address.Country);
        return string.Join("\n", parts);
    }

    private static string GetCurrencySymbol(Currency currency)
    {
        return currency switch
        {
            Currency.USD => "$",
            Currency.EUR => "€",
            Currency.GBP => "£",
            Currency.ZAR => "R",
            Currency.AUD => "A$",
            Currency.CAD => "C$",
            Currency.NZD => "NZ$",
            Currency.Other => "₹",
            _ => ""
        };
    }

    private static IEnumerable<string> WrapText(string text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var currentLine = new StringBuilder();
        foreach (var word in words)
        {
            if (currentLine.Length + word.Length + 1 > maxChars)
            {
                if (currentLine.Length > 0)
                {
                    yield return currentLine.ToString();
                    currentLine.Clear();
                }
                if (word.Length > maxChars)
                {
                    for (int i = 0; i < word.Length; i += maxChars)
                    {
                        int len = Math.Min(maxChars, word.Length - i);
                        yield return word.Substring(i, len);
                    }
                }
                else
                {
                    currentLine.Append(word);
                }
            }
            else
            {
                if (currentLine.Length > 0) currentLine.Append(' ');
                currentLine.Append(word);
            }
        }
        if (currentLine.Length > 0) yield return currentLine.ToString();
    }

    #endregion
}
