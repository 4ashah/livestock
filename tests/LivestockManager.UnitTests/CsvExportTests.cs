using System.Text;
using LivestockManager.Application.Common;

namespace LivestockManager.UnitTests;

public class CsvExportTests
{
    public class SimpleRecord
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class TwoPropObject
    {
        public int Id { get; set; }
        public string? Label { get; set; }
    }

    [Fact]
    public void Escape_Commas_GetQuoted()
    {
        var rows = new[]
        {
            new SimpleRecord { Name = "Smith, John", Description = "Normal" }
        };

        var bytes = CsvExporter.Write(rows);
        var content = Encoding.UTF8.GetString(bytes.Skip(3).ToArray());
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        var dataLine = lines.Skip(1).First(l => l.Length > 0);
        Assert.Contains("\"Smith, John\"", dataLine);
    }

    [Fact]
    public void Escape_Quotes_GetDoubledAndQuoted()
    {
        var rows = new[]
        {
            new SimpleRecord { Name = "He said \"Hello\"", Description = "Normal" }
        };

        var bytes = CsvExporter.Write(rows);
        var content = Encoding.UTF8.GetString(bytes.Skip(3).ToArray());
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        var dataLine = lines.Skip(1).First(l => l.Length > 0);
        Assert.Contains("\"He said \"\"Hello\"\"\"", dataLine);
    }

    [Fact]
    public void Escape_CommasAndQuotes_BothHandled()
    {
        var rows = new[]
        {
            new SimpleRecord { Name = "Doe, Jane \"Boss\"", Description = "Test" }
        };

        var bytes = CsvExporter.Write(rows);
        var content = Encoding.UTF8.GetString(bytes.Skip(3).ToArray());
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        var dataLine = lines.Skip(1).First(l => l.Length > 0);
        Assert.StartsWith("\"", dataLine.Split(',')[0]);
        Assert.Contains("\"\"", dataLine);
    }

    [Fact]
    public void HeaderRow_UsesPropertyNames_TwoPropObject()
    {
        var rows = new[]
        {
            new TwoPropObject { Id = 1, Label = "A" }
        };

        var bytes = CsvExporter.Write(rows);
        var content = Encoding.UTF8.GetString(bytes.Skip(3).ToArray());
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var header = lines[0];

        Assert.Equal("Id,Label", header);
    }

    [Fact]
    public void HeaderRow_UsesPropertyNames_SimpleRecord()
    {
        var rows = new[]
        {
            new SimpleRecord { Name = "X", Description = "Y" }
        };

        var bytes = CsvExporter.Write(rows);
        var content = Encoding.UTF8.GetString(bytes.Skip(3).ToArray());
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var header = lines[0];

        Assert.Equal("Name,Description", header);
    }

    [Fact]
    public void UTF8BOM_Prefix_FirstThreeBytes()
    {
        var rows = new[] { new TwoPropObject { Id = 1, Label = "Test" } };
        var bytes = CsvExporter.Write(rows);

        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
    }

    [Fact]
    public void UTF8BOM_Prefix_EmptyRowsAlsoHasBOM()
    {
        var rows = Enumerable.Empty<TwoPropObject>();
        var bytes = CsvExporter.Write(rows);

        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
    }
}
