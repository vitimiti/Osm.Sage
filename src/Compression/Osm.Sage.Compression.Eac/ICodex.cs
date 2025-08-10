using JetBrains.Annotations;

namespace Osm.Sage.Compression.Eac;

/// <summary>
/// Defines the contract for an EAC codex implementation, including metadata exposure,
/// validation, sizing, and zero-allocation encode/decode operations over spans.
/// </summary>
/// <remarks>
/// Typical usage:
/// <list type="number">
/// <item>Inspect <see cref="About"/> to determine capabilities and identity.</item>
/// <item>Call <see cref="IsValid"/> to quickly check whether a buffer appears to be handled by this codex.</item>
/// <item>For decoding, ensure <see cref="CodexInformation.Capabilities"/> permits decoding, call <see cref="GetSize"/> to determine the required destination size, then <see cref="Decode"/>.</item>
/// <item>For encoding, ensure <see cref="CodexInformation.Capabilities"/> permits encoding, then call <see cref="Encode"/> with a sized enough destination.</item>
/// </list>
/// Implementations should avoid throwing from <see cref="IsValid"/> and prefer returning <c>false</c> for unrecognized or insufficient data.
/// </remarks>
[PublicAPI]
public interface ICodex
{
    /// <summary>
    /// Gets descriptive metadata about this codex, including signature, capabilities, version,
    /// and human-readable type names.
    /// </summary>
    CodexInformation About { get; }

    /// <summary>
    /// Performs a lightweight check to determine whether the provided buffer appears to be
    /// a payload supported by this codex (e.g., via header/signature inspection).
    /// </summary>
    /// <param name="compressedData">The candidate compressed data.</param>
    /// <returns>
    /// <c>true</c> if the buffer plausibly matches this codex; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Should be safe to call on partial buffers and should not throw for non-matching input.
    /// </remarks>
    bool IsValid(ReadOnlySpan<byte> compressedData);

    /// <summary>
    /// Returns the expected size, in bytes, of the decompressed data represented by the given buffer.
    /// </summary>
    /// <param name="compressedData">The compressed data.</param>
    /// <returns>The decompressed size in bytes.</returns>
    /// <exception cref="ArgumentException">Thrown if the buffer is invalid or insufficient for determining size.</exception>
    /// <exception cref="NotSupportedException">Thrown if this codex does not support decoding.</exception>
    int GetSize(ReadOnlySpan<byte> compressedData);

    /// <summary>
    /// Decompresses the provided buffer into the destination span.
    /// </summary>
    /// <param name="compressedData">The source compressed data.</param>
    /// <param name="decompressedData">
    /// The destination buffer for the decompressed bytes. Its length must be at least the value returned by <see cref="GetSize"/>.
    /// </param>
    /// <returns>The number of bytes written to <paramref name="decompressedData"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if the destination is too small or the source data is invalid.</exception>
    /// <exception cref="NotSupportedException">Thrown if this codex does not support decoding.</exception>
    int Decode(ReadOnlySpan<byte> compressedData, Span<byte> decompressedData);

    /// <summary>
    /// Compresses the provided buffer into the destination span.
    /// </summary>
    /// <param name="decompressedData">The source uncompressed data.</param>
    /// <param name="compressedData">The destination buffer for the compressed bytes.</param>
    /// <returns>The number of bytes written to <paramref name="compressedData"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if the destination is too small or the source data is invalid.</exception>
    /// <exception cref="NotSupportedException">Thrown if this codex does not support encoding.</exception>
    int Encode(ReadOnlySpan<byte> decompressedData, Span<byte> compressedData);
}
