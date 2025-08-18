using JetBrains.Annotations;

namespace Osm.Sage.Compression.LightZhl.Exceptions;

/// <summary>
/// Represents the state data for a decoding exception, providing detailed
/// information about the error context during the decoding process.
/// </summary>
[PublicAPI]
public record DecodingExceptionData()
{
    /// <summary>
    /// Gets or initializes the zero-based index of the source data being decoded
    /// at the time the exception occurred. This property identifies the position
    /// in the input data where the decoding issue was encountered.
    /// </summary>
    public required int SourceIndex { get; init; }

    /// <summary>
    /// Gets or initializes the number of bits currently used in the bit buffer during
    /// the decoding process. This property indicates the number of valid bits available
    /// in the buffer for decoding operations.
    /// </summary>
    public required int BitCount { get; init; }

    /// <summary>
    /// Gets or initializes the current bit buffer containing the remaining
    /// undigested bits from the source data. This property provides access
    /// to the raw binary data during the decoding process.
    /// </summary>
    public required uint BitBuffer { get; init; }

    /// <summary>
    /// Gets or initializes the zero-based position within the internal buffer
    /// at the time the decoding exception occurred. This property reflects
    /// the current pointer location in the processing buffer when the error occurred.
    /// </summary>
    public required uint BufferPosition { get; init; }

    /// <summary>
    /// Gets or initializes the index of the last group accessed or processed during
    /// the decoding operation. This property provides context about the state of
    /// the decoding process when an exception is encountered.
    /// </summary>
    public required int LastGroup { get; init; }

    /// <summary>
    /// Gets or initializes the identifier of the last successfully decoded symbol
    /// during the decompression process. This property provides context about the
    /// most recent symbol that was processed before a decoding exception occurred.
    /// </summary>
    public required int LastSymbol { get; init; }

    /// <summary>
    /// Gets or initializes the name of the decoding stage where the exception occurred.
    /// This property indicates the specific phase of the decoding process at the time
    /// the error was encountered, aiding in error diagnosis and debugging.
    /// </summary>
    public required string Stage { get; init; }
}
