using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LivestockManager.Infrastructure.Services.Pdf;

public static class TrueTypeFontValidator
{
    private const long MinimumFileSize = 50000L;
    private const uint TrueTypeSignature = 0x00010000u;
    private const ushort MinTables = 8;
    private const ushort MaxTables = 40;

    private static readonly HashSet<string> RequiredTables = new(new[]
    {
        "head", "hhea", "maxp", "hmtx", "cmap", "name", "OS/2", "glyf", "loca"
    });

    public static bool Validate(Stream fontStream, out IReadOnlyList<string> errors)
    {
        var errorList = new List<string>();
        errors = errorList;

        if (fontStream == null)
        {
            errorList.Add("fontStream is null.");
            return false;
        }
        if (!fontStream.CanRead)
        {
            errorList.Add("fontStream is not readable.");
            return false;
        }

        long length;
        try
        {
            length = fontStream.Length;
        }
        catch (NotSupportedException)
        {
            errorList.Add("Cannot determine stream length.");
            return false;
        }

        if (length < MinimumFileSize)
        {
            errorList.Add($"File too small. Length=" + length + " bytes; minimum required=" + MinimumFileSize + " bytes (real TrueType fonts are typically several hundred KB).");
            return false;
        }

        long originalPos = fontStream.Position;
        try
        {
            fontStream.Position = 0;

            using var reader = new BinaryReader(fontStream, Encoding.ASCII, leaveOpen: true);

            uint sfntVersion = ReadUInt32BigEndian(reader);
            if (sfntVersion != TrueTypeSignature)
            {
                errorList.Add($"Invalid TrueType signature (sfntVersion/scalarType). Expected=0x" + TrueTypeSignature.ToString("X8") + "; actual=0x" + sfntVersion.ToString("X8"));
                return false;
            }

            ushort numTables = ReadUInt16BigEndian(reader);
            if (numTables < MinTables || numTables > MaxTables)
            {
                errorList.Add($"numTables out of range. Expected between " + MinTables + "-" + MaxTables + "; actual=" + numTables);
                return false;
            }

            ushort searchRange = ReadUInt16BigEndian(reader);
            ushort entrySelector = ReadUInt16BigEndian(reader);
            ushort rangeShift = ReadUInt16BigEndian(reader);

            int expectedSelector = 0;
            int pow2 = 1;
            while (pow2 * 2 <= numTables)
            {
                pow2 *= 2;
                expectedSelector++;
            }
            int expectedSearchRange = pow2 * 16;
            int expectedRangeShift = numTables * 16 - expectedSearchRange;
            if (searchRange != expectedSearchRange || entrySelector != expectedSelector || rangeShift != expectedRangeShift)
            {
                errorList.Add($"Inconsistent table directory header searchRange/entrySelector/rangeShift. numTables=" + numTables + "; expected searchRange=" + expectedSearchRange + " actual=" + searchRange + "; expected entrySelector=" + expectedSelector + " actual=" + entrySelector + "; expected rangeShift=" + expectedRangeShift + " actual=" + rangeShift);
                return false;
            }

            var presentTables = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < numTables; i++)
            {
                long dirStart = 12 + i * 16;
                fontStream.Position = dirStart;
                string tag = ReadTag(reader);
                uint checksum = ReadUInt32BigEndian(reader);
                uint offset = ReadUInt32BigEndian(reader);
                uint tableLength = ReadUInt32BigEndian(reader);

                _ = checksum;

                if (offset + tableLength > length)
                {
                    errorList.Add($"Table '{tag}'s offset+length exceeds stream bounds. offset=" + offset + " length=" + tableLength + " streamLen=" + length);
                    return false;
                }
                if (offset < 12 + numTables * 16u)
                {
                    errorList.Add($"Table '{tag}' offset points inside directory header. offset=" + offset);
                    return false;
                }
                if (tableLength == 0 && tag != "head")
                {
                    errorList.Add($"Table '{tag}' has zero length (only head may be empty).");
                    return false;
                }
                presentTables.Add(tag);
            }

            var missing = new List<string>();
            foreach (var req in RequiredTables)
                if (!presentTables.Contains(req))
                    missing.Add(req);
            if (missing.Count > 0)
            {
                errorList.Add("Missing required tables: " + string.Join(", ", missing));
                return false;
            }

            return true;
        }
        finally
        {
            try { fontStream.Position = originalPos; } catch { }
        }
    }

    private static string ReadTag(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        if (bytes.Length != 4) return "";
        return Encoding.ASCII.GetString(bytes);
    }

    private static ushort ReadUInt16BigEndian(BinaryReader reader)
    {
        byte[] b = reader.ReadBytes(2);
        if (b.Length != 2) return 0;
        return (ushort)((b[0] << 8) | b[1]);
    }

    private static uint ReadUInt32BigEndian(BinaryReader reader)
    {
        byte[] b = reader.ReadBytes(4);
        if (b.Length != 4) return 0;
        return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | (uint)b[3];
    }
}
