using JetBrains.Annotations;

namespace Osm.Sage.Compression.Eac;

[PublicAPI]
public interface ICodex
{
    CodexInformation About { get; }

    bool IsValid(ReadOnlySpan<byte> compressedData);
    int ExtractSize(ReadOnlySpan<byte> compressedData);
    ICollection<byte> Encode(ReadOnlyMemory<byte> uncompressedData);
    ICollection<byte> Decode(ReadOnlyMemory<byte> compressedData);
}
