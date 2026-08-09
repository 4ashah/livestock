using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LivestockManager.Infrastructure.Services.Pdf;
using Xunit;

namespace LivestockManager.UnitTests.Final;

public class TrueTypeFontValidatorTests
{
    [Fact]
    public void T1_169BytePlaceholder_FailsValidation()
    {
        var data = new byte[169];
        new Random(42).NextBytes(data);

        using var ms = new MemoryStream(data);
        var ok = TrueTypeFontValidator.Validate(ms, out var errors);

        Assert.False(ok);
        Assert.NotEmpty(errors);
        bool mentionsSize = false;
        foreach (var e in errors)
            if (e.Contains("small", StringComparison.OrdinalIgnoreCase) || e.Contains("Length", StringComparison.Ordinal))
                mentionsSize = true;
        Assert.True(mentionsSize, "Expected 169-byte placeholder to fail on minimum file size check. errors=" + string.Join(" | ", errors));
    }

    [Fact]
    public void T2_Valid50KBStreamWith9RequiredTables_PassesValidation()
    {
        using var ms = BuildMockValidTrueType();
        var ok = TrueTypeFontValidator.Validate(ms, out var errors);

        Assert.True(ok, "Expected valid 50KB+ mock TrueType stream to PASS. Errors: " + string.Join(" | ", errors));
    }

    [Fact]
    public void T3_50KBStreamWithWrongSignature_FailsValidation()
    {
        using var ms = BuildMockValidTrueType();
        ms.Position = 0;
        ms.WriteByte(0x4F);
        ms.WriteByte(0x54);
        ms.WriteByte(0x54);
        ms.WriteByte(0x4F);

        ms.Position = 0;
        var ok = TrueTypeFontValidator.Validate(ms, out var errors);

        Assert.False(ok);
        bool mentionsSig = false;
        foreach (var e in errors)
            if (e.Contains("signature", StringComparison.OrdinalIgnoreCase) || e.Contains("sfntVersion", StringComparison.Ordinal) || e.Contains("00010000", StringComparison.Ordinal))
                mentionsSig = true;
        Assert.True(mentionsSig, "Expected wrong-signature stream to fail on sfntVersion/signature check. errors=" + string.Join(" | ", errors));
    }

    [Fact]
    public void T4_50KBStreamMissingHeadTable_FailsValidation()
    {
        using var ms = BuildMockValidTrueType(omitHead: true);
        ms.Position = 0;
        var ok = TrueTypeFontValidator.Validate(ms, out var errors);

        Assert.False(ok);
        bool mentionsMissing = false;
        foreach (var e in errors)
            if (e.Contains("Missing", StringComparison.OrdinalIgnoreCase) && e.Contains("head", StringComparison.Ordinal))
                mentionsMissing = true;
        Assert.True(mentionsMissing, "Expected head-missing stream to fail with 'head' listed as missing. errors=" + string.Join(" | ", errors));
    }

    private static readonly string[] RequiredTablesForMock = new[] { "head", "hhea", "maxp", "hmtx", "cmap", "name", "OS/2", "glyf", "loca" };

    private static MemoryStream BuildMockValidTrueType(bool omitHead = false)
    {
        const long TargetSize = 52000L;
        var ms = new MemoryStream(capacity: (int)TargetSize + 256);
        using var writer = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);

        writer.Write((byte)0x00);
        writer.Write((byte)0x01);
        writer.Write((byte)0x00);
        writer.Write((byte)0x00);

        ushort numTables = 9;
        int pow2 = 8;
        int entrySelector = 3;
        int searchRange = pow2 * 16;
        int rangeShift = (numTables * 16) - searchRange;

        writer.Write((byte)((numTables >> 8) & 0xFF));
        writer.Write((byte)(numTables & 0xFF));
        writer.Write((byte)((searchRange >> 8) & 0xFF));
        writer.Write((byte)(searchRange & 0xFF));
        writer.Write((byte)((entrySelector >> 8) & 0xFF));
        writer.Write((byte)(entrySelector & 0xFF));
        writer.Write((byte)((rangeShift >> 8) & 0xFF));
        writer.Write((byte)(rangeShift & 0xFF));

        uint dirEnd = (uint)(12 + numTables * 16);
        uint nextOffset = dirEnd + 16u;

        for (int i = 0; i < numTables; i++)
        {
            string tag = RequiredTablesForMock[i];
            if (omitHead && tag == "head")
                tag = "xxxx";

            byte[] tagBytes = Encoding.ASCII.GetBytes(tag);
            if (tagBytes.Length != 4) Array.Resize(ref tagBytes, 4);
            writer.Write(tagBytes);

            uint checksum = (uint)i + 1u;
            writer.Write((byte)((checksum >> 24) & 0xFF));
            writer.Write((byte)((checksum >> 16) & 0xFF));
            writer.Write((byte)((checksum >> 8) & 0xFF));
            writer.Write((byte)(checksum & 0xFF));

            writer.Write((byte)((nextOffset >> 24) & 0xFF));
            writer.Write((byte)((nextOffset >> 16) & 0xFF));
            writer.Write((byte)((nextOffset >> 8) & 0xFF));
            writer.Write((byte)(nextOffset & 0xFF));

            uint tableLen = (uint)(tag == "glyf" ? 400 : 120);
            writer.Write((byte)((tableLen >> 24) & 0xFF));
            writer.Write((byte)((tableLen >> 16) & 0xFF));
            writer.Write((byte)((tableLen >> 8) & 0xFF));
            writer.Write((byte)(tableLen & 0xFF));

            nextOffset += tableLen;
        }

        while (ms.Position < dirEnd + 16)
            writer.Write((byte)0);

        while (ms.Length < TargetSize)
        {
            long pad = Math.Min(4096, TargetSize - ms.Length);
            writer.Write(new byte[pad]);
        }

        ms.Position = 0;
        return ms;
    }
}
