using JetBrains.Annotations;

namespace Osm.Sage.Compression.Eac;

/// <summary>
/// Represents a compression codec that can encode and decode data using a specific compression algorithm.
/// </summary>
[PublicAPI]
public interface ICodex
{
    /// <summary>
    /// Gets information about this codec, including its signature, capabilities, version, and type descriptions.
    /// </summary>
    CodexInformation About { get; }

    /// <summary>
    /// Validates whether the provided data is valid compressed data that this codec can process.
    /// </summary>
    /// <param name="compressedData">The compressed data to validate.</param>
    /// <returns><c>true</c> if the data is valid and can be processed by this codec; otherwise, <c>false</c>.</returns>
    bool IsValid(ReadOnlySpan<byte> compressedData);

    /// <summary>
    /// Extracts the size of the uncompressed data from the compressed data header or metadata.
    /// </summary>
    /// <param name="compressedData">The compressed data from which to extract the uncompressed size.</param>
    /// <returns>The size in bytes of the uncompressed data.</returns>
    /// <exception cref="ArgumentException">Thrown when the compressed data is invalid or corrupted.</exception>
    int ExtractSize(ReadOnlySpan<byte> compressedData);

    /// <summary>
    /// Encodes (compresses) the provided uncompressed data using this codec's compression algorithm.
    /// </summary>
    /// <param name="uncompressedData">The uncompressed data to encode.</param>
    /// <returns>An array of bytes representing the compressed data.</returns>
    /// <exception cref="NotSupportedException">Thrown when this codec does not support encoding operations.</exception>
    byte[] Encode(ReadOnlySpan<byte> uncompressedData);

    /// <summary>
    /// Decodes (decompresses) the provided compressed data using this codec's decompression algorithm.
    /// </summary>
    /// <param name="compressedData">The compressed data to decode.</param>
    /// <returns>An array of bytes representing the uncompressed data.</returns>
    /// <exception cref="ArgumentException">Thrown when the compressed data is invalid or corrupted.</exception>
    /// <exception cref="NotSupportedException">Thrown when this codec does not support decoding operations.</exception>
    byte[] Decode(ReadOnlySpan<byte> compressedData);
}
