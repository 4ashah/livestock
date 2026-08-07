using System.Text;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;

namespace LivestockManager.Infrastructure.Services;

public class StubPdfGenerator : IPdfGenerator
{
    public Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice, Company company)
    {
        var lines = new List<string>
        {
            "INVOICE",
            $"Invoice #: {invoice.InvoiceNumber}",
            $"Date: {invoice.InvoiceDate:yyyy-MM-dd}",
            $"Due: {invoice.DueDate:yyyy-MM-dd}",
            "",
            "From:",
            company.Name,
            $"Email: {company.Email}",
            "",
            $"Customer: {invoice.CustomerId}",
            "",
            "Items:",
            $"  Subtotal: {invoice.Subtotal}",
            $"  Discount: {invoice.DiscountTotal}",
            $"  Tax:      {invoice.TaxTotal}",
            "",
            $"Grand Total: {invoice.GrandTotal}",
            $"Paid:        {invoice.PaidAmount}",
            $"Outstanding: {invoice.OutstandingAmount}",
            "",
            $"Status: {invoice.Status}"
        };

        return Task.FromResult(GenerateMinimalPdf(lines, $"Invoice {invoice.InvoiceNumber}"));
    }

    private static byte[] GenerateMinimalPdf(IEnumerable<string> textLines, string title)
    {
        var lines = textLines.ToList();
        var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);

        writer.Write(Encoding.ASCII.GetBytes("%PDF-1.4\n"));
        writer.Write(Encoding.ASCII.GetBytes("%âãÏÓ\n"));

        var contentStream = new StringBuilder();
        contentStream.AppendLine("BT");
        contentStream.AppendLine("/F1 12 Tf");
        float y = 770;
        foreach (var line in lines)
        {
            var safeLine = line.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            contentStream.AppendLine($"14 {y} Td");
            contentStream.AppendLine($"({safeLine}) Tj");
            y -= 18;
        }
        contentStream.AppendLine("ET");
        var contentBytes = Encoding.ASCII.GetBytes(contentStream.ToString());

        var objects = new List<(int Number, string Output)>();

        objects.Add((1, "<< /Type /Catalog /Pages 2 0 R /OpenAction [3 0 R /XYZ null null null] >>"));
        objects.Add((2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>"));
        objects.Add((3, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>"));
        objects.Add((4, $"<< /Length {contentBytes.Length} >>\nstream\n" +
                      Encoding.ASCII.GetString(contentBytes) + "endstream"));
        objects.Add((5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"));

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
        writer.Write(Encoding.ASCII.GetBytes($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefStart}\n%%EOF\n"));
        writer.Flush();

        return ms.ToArray();
    }
}
