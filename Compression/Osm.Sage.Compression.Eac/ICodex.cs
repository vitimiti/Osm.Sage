using JetBrains.Annotations;

namespace Osm.Sage.Compression.Eac;

[PublicAPI]
public interface ICodex
{
    CodexInformation About { get; }
    bool IsValid { get; }

    int ExtractSize(ReadOnlySpan<byte> compressedData);
    int Encode(ReadOnlyMemory<byte> uncompressedData, Span<byte> compressedData);
    int Decode(ReadOnlyMemory<byte> compressedData, Span<byte> uncompressedData);
}
