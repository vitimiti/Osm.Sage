using System.Buffers.Binary;
using JetBrains.Annotations;
using Osm.Sage.Gimex;

namespace Osm.Sage.Compression.Eac.Codex;

[PublicAPI]
public partial class HuffmanWithRunlengthCodex : ICodex
{
    private int _deltaRuns;

    public CodexInformation About =>
        new()
        {
            Signature = new Signature("HUFF"),
            Capabilities = new CodexCapabilities
            {
                CanDecode = true,
                CanEncode = true,
                Supports32BitFields = false,
            },
            Version = new Version(1, 4),
            ShortType = "huff",
            LongType = "Huffman",
        };

    public int DeltaRuns
    {
        get => _deltaRuns;
        set => _deltaRuns = int.Clamp(value, 0, 2);
    }

    public bool IsValid(ReadOnlySpan<byte> compressedData)
    {
        if (compressedData.Length < 2)
        {
            return false;
        }

        return BinaryPrimitives.ReadUInt16BigEndian(compressedData)
            is 0x30FB
                or 0x31FB
                or 0x32FB
                or 0x33FB
                or 0x34FB
                or 0x35FB
                or 0xB0FB
                or 0xB1FB
                or 0xB2FB
                or 0xB3FB
                or 0xB4FB
                or 0xB5FB;
    }

    public int ExtractSize(ReadOnlySpan<byte> compressedData)
    {
        if (!IsValid(compressedData))
        {
            throw new ArgumentException(
                $"The data is not a valid {nameof(RefpackCodex)}",
                nameof(compressedData)
            );
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(compressedData.Length, 2);

        var packType = BinaryPrimitives.ReadUInt16BigEndian(compressedData);
        var byteCount = (packType & 0x8000) != 0 ? 4 : 3;
        var offset = (packType & 0x0100) != 0 ? 2 + byteCount : 2;

        ArgumentOutOfRangeException.ThrowIfLessThan(compressedData.Length, offset + byteCount);

        // 3 or 4 bytes
        return byteCount switch
        {
            // 31FB 33FB 35FB
            4 => (int)BinaryPrimitives.ReadUInt32BigEndian(compressedData[offset..]),
            // 30FB 32FB 34FB
            _ => compressedData[offset] << 16
                | BinaryPrimitives.ReadUInt16BigEndian(compressedData[(offset + 1)..]),
        };
    }

    public byte[] Encode(ReadOnlySpan<byte> uncompressedData)
    {
        EncodingContext context = new();
        var src = uncompressedData.ToArray();
        switch (DeltaRuns)
        {
            case 1:
                src = DeltaOnce(src);
                break;
            case 2:
                src = DeltaOnce(src);
                src = DeltaOnce(src);
                break;
        }

        context.Buffer = src;
        context.FLength = src.Length;
        context.ULength = (uint)src.Length;

        MemStruct outFile = new();
        PackFile(ref context, src, ref outFile, context.FLength, DeltaRuns);

        return outFile.Buffer.ToArray();
    }

    public byte[] Decode(ReadOnlySpan<byte> compressedData)
    {
        if (!IsValid(compressedData))
        {
            throw new ArgumentException(
                $"The data is not a valid {nameof(HuffmanWithRunlengthCodex)}",
                nameof(compressedData)
            );
        }

        throw new NotImplementedException();
    }
}
