using System.Buffers.Binary;
using JetBrains.Annotations;

namespace Osm.Sage.Compression.Eac.Codex;

/// <summary>
/// Provides RefPack compression/decompression support for the EA Maxis RefPack format.
/// </summary>
/// <remarks>
/// <para>
/// RefPack is a general-purpose lossless compression algorithm originally developed by
/// EA Maxis and used in various EA games and formats. This codex handles the standard
/// RefPack format variants with signatures 0x10FB, 0x11FB, 0x90FB, and 0x91FB.
/// </para>
/// <para>
/// The format structure varies based on the signature's flag bits:
/// <list type="bullet">
/// <item>Bit 0x8000: Determines field width (4 bytes if set, 3 bytes otherwise)</item>
/// <item>Bit 0x0100: Indicates presence of additional metadata block</item>
/// </list>
/// </para>
/// <para>
/// This implementation is distributed across multiple partial class files:
/// <list type="bullet">
/// <item>Core metadata and validation (RefPackCodexData.cs)</item>
/// <item>Decode operations (RefPackCodexDecode.cs)</item>
/// <item>Encode operations (RefPackCodexEncode.cs)</item>
/// </list>
/// </para>
/// </remarks>
[PublicAPI]
public partial class RefPackCodex : ICodex
{
    /// <summary>
    /// Gets descriptive metadata about this RefPack codex implementation.
    /// </summary>
    /// <value>
    /// Returns codex information with signature "REF", full encode/decode capabilities,
    /// 32-bit field support, version 1.1, and human-readable type names.
    /// </value>
    public CodexInformation About =>
        new(
            Signature: new CodexSignature("REF"),
            Capabilities: new CodexCapabilities(
                CanDecode: true,
                CanEncode: true,
                Supports32BitFields: true
            ),
            Version: new Version(1, 1),
            ShortType: "ref",
            LongType: "Refpack"
        );

    /// <summary>
    /// Determines whether the provided buffer appears to contain RefPack-compressed data
    /// by examining the format signature.
    /// </summary>
    /// <param name="compressedData">The candidate compressed data buffer.</param>
    /// <returns>
    /// <c>true</c> if the buffer starts with a recognized RefPack signature; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Recognizes the following 32-bit big-endian signatures:
    /// <list type="bullet">
    /// <item>0x10FB - Standard RefPack format</item>
    /// <item>0x11FB - RefPack with extended metadata</item>
    /// <item>0x90FB - RefPack variant (alternate compression)</item>
    /// <item>0x91FB - RefPack variant with extended metadata</item>
    /// </list>
    /// Safe to call on partial buffers; returns <c>false</c> for insufficient data.
    /// </remarks>
    public bool IsValid(ReadOnlySpan<byte> compressedData)
    {
        if (compressedData.Length < 2)
        {
            return false;
        }

        var packType = BinaryPrimitives.ReadInt32BigEndian(compressedData);
        return packType is 0x10FB or 0x11FB or 0x90FB or 0x91FB;
    }

    /// <summary>
    /// Extracts the expected decompressed size from RefPack header information.
    /// </summary>
    /// <param name="compressedData">The RefPack-compressed data buffer.</param>
    /// <returns>The expected size in bytes of the decompressed data.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown if this codex instance does not support decoding operations.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the data is invalid, unrecognized, or insufficient for size determination.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown if the computed size exceeds <see cref="int.MaxValue"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The size field location and width depend on format flags:
    /// <list type="bullet">
    /// <item>Field width: 4 bytes if bit 0x8000 is set in the signature, otherwise 3 bytes</item>
    /// <item>Location: After signature (offset 2) or after signature + metadata block (offset 2 + field width)</item>
    /// <item>Additional metadata: Present if bit 0x0100 is set in the signature</item>
    /// </list>
    /// </para>
    /// <para>
    /// All multibyte values are stored in big-endian byte order.
    /// </para>
    /// </remarks>
    public int GetSize(ReadOnlySpan<byte> compressedData)
    {
        if (!About.Capabilities.CanDecode)
        {
            throw new NotSupportedException(
                $"The codex {nameof(RefPackCodex)} does not support decoding."
            );
        }

        if (!IsValid(compressedData))
        {
            throw new ArgumentException("Invalid compressed data", nameof(compressedData));
        }

        if (compressedData.Length < 2)
        {
            throw new ArgumentException(
                "Compressed data is too small to read the header.",
                nameof(compressedData)
            );
        }

        var packType = BinaryPrimitives.ReadInt32BigEndian(compressedData);
        var bytesToRead = (packType & 0x8000) != 0 ? 4 : 3;
        var offset = 2 + (((packType & 0x0100) != 0) ? bytesToRead : 0);

        if (compressedData.Length < offset + bytesToRead)
        {
            throw new ArgumentException(
                "Compressed data is too small to read the uncompressed size.",
                nameof(compressedData)
            );
        }

        // Avoid overflows and throw
        return checked(
            bytesToRead == 4
                ? BinaryPrimitives.ReadInt32BigEndian(compressedData[offset..])
                : compressedData[offset] << 16
                    | compressedData[offset + 1] << 8
                    | compressedData[offset + 2]
        );
    }
}
