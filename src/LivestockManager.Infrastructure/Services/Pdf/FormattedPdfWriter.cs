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

    private static readonly CultureInfo EnUs = new("en-US");

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
            if (parts.Length >= 2 && string.IsNullOrWhiteSpace(customerAddress)) customerAddress = parts[1].Trim();
            if (parts.Length >= 3 && string.IsNullOrWhiteSpace(customerTaxNumber)) customerTaxNumber = parts[2].Trim();
        }

        var companyAddress = FormatAddress(company.Address);
        var currencySymbol = GetCurrencySymbol(invoice.Currency);

        return Task.FromResult(BuildInvoicePdf(
            invoice, company, customerName, customerAddress, customerTaxNumber,
            companyAddress, currencySymbol));
    }

    #region ReceiptHelper (Internal for future module)

    internal async Task<byte[]> GenerateReceiptPdfAsync(
        Payment payment, Receipt receipt, Company company, Customer customer)
    {
        var companyAddress = FormatAddress(company.Address);
        var customerAddress = FormatAddress(customer.BillingAddress);
        var currencySymbol = GetCurrencySymbol(receipt.Currency);

        return await Task.FromResult(BuildReceiptPdf(
            payment, receipt, company, customer, companyAddress, customerAddress, currencySymbol));
    }

    #endregion

    #region PDF Core Infrastructure

    private static byte[] BuildInvoicePdf(
        Invoice invoice, Company company,
        string customerName, string customerAddress, string customerTaxNumber,
        string companyAddress, string currencySymbol)
    {
        var content = new StringBuilder();
        float y = PageHeight - MarginTop;

        content.AppendLine("q");

        y -= 4;
        DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
        y -= 10;

        content.AppendLine("BT");
        content.AppendLine($"/F2 22 Tf");
        content.AppendLine($"1 0 0 1 {MarginLeft.ToString(EnUs)} {(y - 20).ToString(EnUs)} Tm");
        content.AppendLine("(INVOICE) Tj");
        content.AppendLine("ET");
        y -= 30;

        content.AppendLine("BT");
        content.AppendLine("/F1 9 Tf");
        float labelX = PageWidth - MarginRight - 190;
        float valX = labelX + 75;

        DrawTextRight(content, labelX, y, "Invoice #:", "/F1", 9);
        DrawTextLeft(content, valX, y, invoice.InvoiceNumber ?? "-", "/F2", 9);
        y -= 13;
        DrawTextRight(content, labelX, y, "Date:", "/F1", 9);
        DrawTextLeft(content, valX, y, invoice.InvoiceDate.ToString("yyyy-MM-dd", EnUs), "/F1", 9);
        y -= 13;
        DrawTextRight(content, labelX, y, "Due Date:", "/F1", 9);
        DrawTextLeft(content, valX, y, invoice.DueDate.ToString("yyyy-MM-dd", EnUs), "/F1", 9);
        y -= 13;
        DrawTextRight(content, labelX, y, "Status:", "/F1", 9);
        DrawTextLeft(content, valX, y, invoice.Status.ToString(), "/F2", 9);
        content.AppendLine("ET");
        y -= 18;

        DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
        y -= 14;

        float fromX = MarginLeft;
        float billX = MarginLeft + ContentWidth * 0.52f;
        float sectionY = y;
        float billSectionY = y;

        content.AppendLine("BT");
        DrawTextLeft(content, fromX, sectionY, "FROM", "/F2", 9);
        sectionY -= 13;
        DrawTextLeft(content, fromX, sectionY, company.Name, "/F2", 10);
        sectionY -= 12;
        DrawMultiLineLeft(content, fromX, ref sectionY, companyAddress, "/F1", 9, 11);
        if (!string.IsNullOrWhiteSpace(company.TaxNumber))
        {
            DrawTextLeft(content, fromX, sectionY, $"Tax: {company.TaxNumber}", "/F1", 9);
            sectionY -= 11;
        }
        if (!string.IsNullOrWhiteSpace(company.Phone))
        {
            DrawTextLeft(content, fromX, sectionY, $"Tel: {company.Phone}", "/F1", 9);
            sectionY -= 11;
        }
        if (!string.IsNullOrWhiteSpace(company.Email))
        {
            DrawTextLeft(content, fromX, sectionY, $"Email: {company.Email}", "/F1", 9);
            sectionY -= 11;
        }

        DrawTextLeft(content, billX, billSectionY, "BILL TO", "/F2", 9);
        billSectionY -= 13;
        DrawTextLeft(content, billX, billSectionY, customerName, "/F2", 10);
        billSectionY -= 12;
        DrawMultiLineLeft(content, billX, ref billSectionY, customerAddress, "/F1", 9, 11);
        if (!string.IsNullOrWhiteSpace(customerTaxNumber))
        {
            DrawTextLeft(content, billX, billSectionY, $"Tax: {customerTaxNumber}", "/F1", 9);
            billSectionY -= 11;
        }
        content.AppendLine("ET");

        y = Math.Min(sectionY, billSectionY) - 18;
        DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
        y -= 14;

        DrawInvoiceItemsTable(content, ref y, invoice.Items, currencySymbol);
        y -= 10;

        y -= 2;

        DrawInvoiceTotals(content, ref y, invoice, currencySymbol);
        y -= 14;

        DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
        y -= 14;

        DrawNotesAndTerms(content, ref y, invoice.Notes, invoice.Terms);

        content.AppendLine("Q");

        var contentBytes = Encoding.ASCII.GetBytes(content.ToString());

        return AssemblePdf(contentBytes);
    }

    private static byte[] BuildReceiptPdf(
        Payment payment, Receipt receipt, Company company, Customer customer,
        string companyAddress, string customerAddress, string currencySymbol)
    {
        var content = new StringBuilder();
        float y = PageHeight - MarginTop;

        content.AppendLine("q");

        y -= 4;
        DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
        y -= 10;

        content.AppendLine("BT");
        content.AppendLine("/F2 22 Tf");
        content.AppendLine($"1 0 0 1 {MarginLeft.ToString(EnUs)} {(y - 20).ToString(EnUs)} Tm");
        content.AppendLine("(RECEIPT) Tj");
        content.AppendLine("ET");
        y -= 30;

        content.AppendLine("BT");
        content.AppendLine("/F1 9 Tf");
        float labelX = PageWidth - MarginRight - 200;
        float valX = labelX + 85;

        DrawTextRight(content, labelX, y, "Receipt #:", "/F1", 9);
        DrawTextLeft(content, valX, y, receipt.ReceiptNumber ?? "-", "/F2", 9);
        y -= 13;
        DrawTextRight(content, labelX, y, "Receipt Date:", "/F1", 9);
        DrawTextLeft(content, valX, y, receipt.ReceiptDate.ToString("yyyy-MM-dd", EnUs), "/F1", 9);
        y -= 13;
        DrawTextRight(content, labelX, y, "Payment Date:", "/F1", 9);
        DrawTextLeft(content, valX, y, payment.PaymentDate.ToString("yyyy-MM-dd", EnUs), "/F1", 9);
        y -= 13;
        if (payment.InvoiceId.HasValue)
        {
            DrawTextRight(content, labelX, y, "Invoice Ref:", "/F1", 9);
            var invRef = payment.Invoice?.InvoiceNumber ?? payment.InvoiceId.Value.ToString("N")[..8];
            DrawTextLeft(content, valX, y, invRef, "/F1", 9);
            y -= 13;
        }
        DrawTextRight(content, labelX, y, "Method:", "/F1", 9);
        DrawTextLeft(content, valX, y, payment.Method.ToString(), "/F1", 9);
        content.AppendLine("ET");
        y -= 18;

        DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
        y -= 14;

        float fromX = MarginLeft;
        float custX = MarginLeft + ContentWidth * 0.52f;
        float sectionY = y;
        float custSectionY = y;

        content.AppendLine("BT");
        DrawTextLeft(content, fromX, sectionY, "ISSUED BY", "/F2", 9);
        sectionY -= 13;
        DrawTextLeft(content, fromX, sectionY, company.Name, "/F2", 10);
        sectionY -= 12;
        DrawMultiLineLeft(content, fromX, ref sectionY, companyAddress, "/F1", 9, 11);
        if (!string.IsNullOrWhiteSpace(company.TaxNumber))
        {
            DrawTextLeft(content, fromX, sectionY, $"Tax: {company.TaxNumber}", "/F1", 9);
            sectionY -= 11;
        }

        DrawTextLeft(content, custX, custSectionY, "RECEIVED FROM", "/F2", 9);
        custSectionY -= 13;
        DrawTextLeft(content, custX, custSectionY, customer.Name, "/F2", 10);
        custSectionY -= 12;
        DrawMultiLineLeft(content, custX, ref custSectionY, customerAddress, "/F1", 9, 11);
        if (!string.IsNullOrWhiteSpace(customer.TaxNumber))
        {
            DrawTextLeft(content, custX, custSectionY, $"Tax: {customer.TaxNumber}", "/F1", 9);
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
        DrawTextRight(content, totalsBoxX + totalsBoxWidth - 150, ty, "AMOUNT RECEIVED:", "/F2", 9);
        DrawTextRight(content, totalsBoxX + totalsBoxWidth - 12, ty,
            $"{currencySymbol}{receipt.AmountReceived:N2}", "/F2", 9);
        ty -= 16;
        DrawLine(content, totalsBoxX + 10, ty + 6, totalsBoxX + totalsBoxWidth - 10, ty + 6);
        ty -= 2;
        DrawTextRight(content, totalsBoxX + totalsBoxWidth - 150, ty, "Running Balance:", "/F1", 9);
        DrawTextRight(content, totalsBoxX + totalsBoxWidth - 12, ty,
            $"{currencySymbol}{receipt.RunningInvoiceBalance:N2}", "/F1", 9);
        content.AppendLine("ET");

        y = boxBottom - 16;

        DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
        y -= 14;

        DrawNotesAndTerms(content, ref y, receipt.Notes, null);

        content.AppendLine("Q");

        var contentBytes = Encoding.ASCII.GetBytes(content.ToString());

        return AssemblePdf(contentBytes);
    }

    private static byte[] AssemblePdf(byte[] contentBytes)
    {
        var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);

        writer.Write(Encoding.ASCII.GetBytes("%PDF-1.4\n"));
        writer.Write(new byte[] { 0xE2, 0xE3, 0xCF, 0xD3, 0x0A });

        var objects = new List<(int Number, string Output)>
        {
            (1, "<< /Type /Catalog /Pages 2 0 R /OpenAction [3 0 R /XYZ null null null] >>"),
            (2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            (3, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth.ToString(EnUs)} {PageHeight.ToString(EnUs)}] /Contents 4 0 R /Resources << /Font << /F1 5 0 R /F2 6 0 R >> >> >>"),
            (4, $"<< /Length {contentBytes.Length} >>\nstream\n" + Encoding.ASCII.GetString(contentBytes) + "endstream"),
            (5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"),
            (6, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>")
        };

        var xref = new StringBuilder();
        xref.AppendLine("xref");
        xref.AppendLine("0 " + (objects.Count + 1));
        xref.AppendLine("0000000000 65535 f ");

        var offsets = new List<int>();
        writer.Flush();
        var runningOffset = (int)ms.Length;

        foreach (var obj in objects)
        {
            offsets.Add(runningOffset);
            var objString = $"{obj.Number} 0 obj\n{obj.Output}\nendobj\n";
            var objBytes = Encoding.ASCII.GetBytes(objString);
            writer.Write(objBytes);
            runningOffset += objBytes.Length;
        }

        foreach (var offset in offsets)
        {
            xref.AppendLine($"{offset:0000000000} 00000 n ");
        }

        var xrefStart = (int)ms.Length;
        writer.Write(Encoding.ASCII.GetBytes(xref.ToString()));
        writer.Write(Encoding.ASCII.GetBytes(
            $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefStart}\n%%EOF\n"));
        writer.Flush();

        return ms.ToArray();
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

    private static void DrawTextLeft(StringBuilder content, float x, float y, string text, string font, int size)
    {
        var safe = EscapePdfString(text);
        content.AppendLine($"/{font.Replace("/","")} {size} Tf");
        content.AppendLine($"1 0 0 1 {x.ToString(EnUs)} {y.ToString(EnUs)} Tm");
        content.AppendLine($"({safe}) Tj");
    }

    private static void DrawTextRight(StringBuilder content, float x, float y, string text, string font, int size)
    {
        var safe = EscapePdfString(text);
        content.AppendLine($"/{font.Replace("/","")} {size} Tf");
        content.AppendLine($"1 0 0 1 {x.ToString(EnUs)} {y.ToString(EnUs)} Tm");
        content.AppendLine($"({safe}) Tj");
    }

    private static void DrawMultiLineLeft(StringBuilder content, float x, ref float y,
        string text, string font, int size, int lineGap)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            DrawTextLeft(content, x, y, line, font, size);
            y -= lineGap;
        }
    }

    private static void DrawInvoiceItemsTable(StringBuilder content, ref float y,
        List<InvoiceItem> items, string currencySymbol)
    {
        float col1X = MarginLeft;
        float col2X = MarginLeft + 240;
        float col3X = col2X + 55;
        float col4X = col3X + 65;
        float col5X = col4X + 55;
        float col7X = PageWidth - MarginRight;

        content.AppendLine("BT");
        DrawTextLeft(content, col1X, y, "Description", "/F2", 9);
        DrawTextRight(content, col2X - 6, y, "Qty", "/F2", 9);
        DrawTextRight(content, col3X - 6, y, "Unit Price", "/F2", 9);
        DrawTextRight(content, col4X - 6, y, "Disc %", "/F2", 9);
        DrawTextRight(content, col5X - 6, y, "Tax %", "/F2", 9);
        DrawTextRight(content, col7X - 6, y, "Line Total", "/F2", 9);
        content.AppendLine("ET");
        y -= 4;

        DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
        y -= 10;

        if (items.Count == 0)
        {
            content.AppendLine("BT");
            DrawTextLeft(content, col1X, y, "No items.", "/F1", 9);
            content.AppendLine("ET");
            return;
        }

        content.AppendLine("BT");
        foreach (var item in items)
        {
            var desc = (item.Description ?? "").Length > 45
                ? item.Description![..42] + "..."
                : item.Description ?? "";
            DrawTextLeft(content, col1X, y, desc, "/F1", 9);
            DrawTextRight(content, col2X - 6, y, item.Quantity.ToString("N2", EnUs), "/F1", 9);
            DrawTextRight(content, col3X - 6, y, $"{currencySymbol}{item.UnitPrice:N2}", "/F1", 9);
            DrawTextRight(content, col4X - 6, y, $"{item.DiscountPercent * 100:N1}%", "/F1", 9);
            DrawTextRight(content, col5X - 6, y, $"{item.TaxPercent * 100:N1}%", "/F1", 9);
            DrawTextRight(content, col7X - 6, y, $"{currencySymbol}{item.LineTotal:N2}", "/F1", 9);
            y -= 13;
        }
        content.AppendLine("ET");

        y -= 2;
        DrawLine(content, MarginLeft, y, PageWidth - MarginRight, y);
        y -= 8;
    }

    private static void DrawInvoiceTotals(StringBuilder content, ref float y, Invoice invoice, string currencySymbol)
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
        DrawTextRight(content, boxX + boxWidth - 150, ty, "Subtotal:", "/F1", 9);
        DrawTextRight(content, boxX + boxWidth - 12, ty, $"{currencySymbol}{invoice.Subtotal:N2}", "/F1", 9);
        ty -= rowHeight;
        DrawTextRight(content, boxX + boxWidth - 150, ty, "Discount Total:", "/F1", 9);
        DrawTextRight(content, boxX + boxWidth - 12, ty, $"{currencySymbol}{invoice.DiscountTotal:N2}", "/F1", 9);
        ty -= rowHeight;
        DrawTextRight(content, boxX + boxWidth - 150, ty, "Tax Total:", "/F1", 9);
        DrawTextRight(content, boxX + boxWidth - 12, ty, $"{currencySymbol}{invoice.TaxTotal:N2}", "/F1", 9);
        ty -= rowHeight;
        DrawTextRight(content, boxX + boxWidth - 150, ty, "Charge Total:", "/F1", 9);
        DrawTextRight(content, boxX + boxWidth - 12, ty, $"{currencySymbol}{invoice.ChargeTotal:N2}", "/F1", 9);
        ty -= rowHeight;
        DrawLine(content, boxX + 10, ty + 8, boxX + boxWidth - 10, ty + 8);
        ty -= 2;
        DrawTextRight(content, boxX + boxWidth - 150, ty, "GRAND TOTAL:", "/F2", 9);
        DrawTextRight(content, boxX + boxWidth - 12, ty, $"{currencySymbol}{invoice.GrandTotal:N2}", "/F2", 9);
        ty -= rowHeight;
        DrawTextRight(content, boxX + boxWidth - 150, ty, "Paid Amount:", "/F1", 9);
        DrawTextRight(content, boxX + boxWidth - 12, ty, $"{currencySymbol}{invoice.PaidAmount:N2}", "/F1", 9);
        ty -= rowHeight;
        DrawTextRight(content, boxX + boxWidth - 150, ty, "OUTSTANDING:", "/F2", 9);
        DrawTextRight(content, boxX + boxWidth - 12, ty, $"{currencySymbol}{invoice.OutstandingAmount:N2}", "/F2", 9);
        content.AppendLine("ET");

        y = boxBottom;
    }

    private static void DrawNotesAndTerms(StringBuilder content, ref float y, string? notes, string? terms)
    {
        content.AppendLine("BT");
        if (!string.IsNullOrWhiteSpace(notes))
        {
            DrawTextLeft(content, MarginLeft, y, "NOTES:", "/F2", 9);
            y -= 13;
            var wrapped = WrapText(notes!, 95);
            foreach (var line in wrapped)
            {
                if (y < MarginBottom + 30) break;
                DrawTextLeft(content, MarginLeft, y, line, "/F1", 8);
                y -= 11;
            }
            y -= 6;
        }

        if (!string.IsNullOrWhiteSpace(terms) && y > MarginBottom + 30)
        {
            DrawTextLeft(content, MarginLeft, y, "TERMS & CONDITIONS:", "/F2", 9);
            y -= 13;
            var wrapped = WrapText(terms!, 95);
            foreach (var line in wrapped)
            {
                if (y < MarginBottom + 12) break;
                DrawTextLeft(content, MarginLeft, y, line, "/F1", 8);
                y -= 11;
            }
        }
        content.AppendLine("ET");
    }

    #endregion

    #region Utility Helpers

    private static string EscapePdfString(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
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
                    if (c < 32 || c > 126)
                        sb.AppendFormat("\\{0}", Convert.ToString((int)c, 8).PadLeft(3, '0'));
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

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
            Currency.EUR => "EUR ",
            Currency.GBP => "\\243",
            Currency.ZAR => "R",
            Currency.AUD => "AUD ",
            Currency.CAD => "CAD ",
            Currency.NZD => "NZD ",
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
