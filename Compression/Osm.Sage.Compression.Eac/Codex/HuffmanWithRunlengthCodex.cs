using System.Buffers.Binary;
using Osm.Sage.Gimex;

namespace Osm.Sage.Compression.Eac.Codex;

public partial class HuffmanWithRunlengthCodex : ICodex
{
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

    public bool IsValid(ReadOnlySpan<byte> compressedData) =>
        BinaryPrimitives.ReadUInt16BigEndian(compressedData)
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

    public ICollection<byte> Encode(ReadOnlySpan<byte> uncompressedData)
    {
        throw new NotImplementedException();
    }

    public ICollection<byte> Decode(ReadOnlySpan<byte> compressedData)
    {
        throw new NotImplementedException();
    }
}
