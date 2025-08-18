using JetBrains.Annotations;

namespace Osm.Sage.Compression.LightZhl.Exceptions;

[PublicAPI]
public record DecodingExceptionData()
{
    public required int SourceIndex { get; init; }
    public required int BitCount { get; init; }
    public required uint BitBuffer { get; init; }
    public required uint BufferPosition { get; init; }
    public required int LastGroup { get; init; }
    public required int LastSymbol { get; init; }
    public required string Stage { get; init; }
}
