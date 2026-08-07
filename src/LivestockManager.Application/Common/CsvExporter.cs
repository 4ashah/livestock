using System.Text;

namespace LivestockManager.Application.Common;

public static class CsvExporter
{
    public static byte[] Write<T>(IEnumerable<T> rows, IEnumerable<string>? columnNames = null)
    {
        var sb = new StringBuilder();
        var properties = typeof(T).GetProperties(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        var cols = (columnNames ?? properties.Select(p => p.Name)).ToList();
        sb.AppendLine(string.Join(",", cols.Select(Escape)));

        var propNames = columnNames != null
            ? cols.Select(c => (object?)properties.FirstOrDefault(p =>
                string.Equals(p.Name, c, StringComparison.OrdinalIgnoreCase))).ToList()
            : properties.Cast<object?>().ToList();

        foreach (var row in rows)
        {
            var values = new List<string>();
            foreach (var p in propNames)
            {
                if (p is System.Reflection.PropertyInfo pi)
                {
                    var val = pi.GetValue(row);
                    var str = val?.ToString() ?? string.Empty;
                    values.Add(Escape(str));
                }
                else
                {
                    values.Add(string.Empty);
                }
            }
            sb.AppendLine(string.Join(",", values));
        }

        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[bom.Length + contentBytes.Length];
        Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
        Buffer.BlockCopy(contentBytes, 0, result, bom.Length, contentBytes.Length);
        return result;
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}
