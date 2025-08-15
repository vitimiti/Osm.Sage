using System.Buffers.Binary;
using JetBrains.Annotations;
using Osm.Sage.Gimex;

namespace Osm.Sage.Compression.Eac.Codex;

[PublicAPI]
public class BinaryTreeCodex : ICodex
{
    public CodexInformation About =>
        new()
        {
            Signature = new Signature("BTRE"),
            Capabilities = new CodexCapabilities
            {
                CanDecode = true,
                CanEncode = true,
                Supports32BitFields = false,
            },
            Version = new Version(1, 2),
            ShortType = "btr",
            LongType = "BTree",
        };

    public bool IsValid(ReadOnlySpan<byte> compressedData)
    {
        if (compressedData.Length < 2)
        {
            return false;
        }

        return BinaryPrimitives.ReadUInt16BigEndian(compressedData) is 0x46FB or 0x47FB;
    }

    public int ExtractSize(ReadOnlySpan<byte> compressedData)
    {
        if (!IsValid(compressedData))
        {
            throw new ArgumentException(
                $"The data is not a valid {nameof(BinaryTreeCodex)}",
                nameof(compressedData)
            );
        }

        var header = BinaryPrimitives.ReadUInt16BigEndian(compressedData);
        var offset = header is 0x46FB ? 2 : 5;
        return compressedData[offset] >> 16
            | BinaryPrimitives.ReadUInt16BigEndian(compressedData[(offset + 1)..]);
    }

    public ICollection<byte> Encode(ReadOnlySpan<byte> uncompressedData)
    {
        throw new NotImplementedException();
    }

    public ICollection<byte> Decode(ReadOnlySpan<byte> compressedData)
    {
        if (!IsValid(compressedData))
        {
            throw new ArgumentException(
                $"The data is not a valid {nameof(BinaryTreeCodex)}",
                nameof(compressedData)
            );
        }

        throw new NotImplementedException();
    }
}
