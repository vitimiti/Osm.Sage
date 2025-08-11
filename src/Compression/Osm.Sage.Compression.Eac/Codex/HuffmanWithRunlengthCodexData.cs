using System.Buffers.Binary;
using JetBrains.Annotations;

namespace Osm.Sage.Compression.Eac.Codex;

/// <summary>
/// Provides Huffman with Run-length Encoding compression/decompression support for EA game formats.
/// </summary>
/// <remarks>
/// <para>
/// The Huffman with Run-length Encoding codec combines Huffman coding with run-length encoding
/// to achieve efficient compression for data with repetitive patterns. This format is commonly
/// used in various EA games for compressing assets and game data.
/// </para>
/// <para>
/// The format supports multiple signature variants that indicate different compression modes
/// and field width configurations:
/// <list type="bullet">
/// <item>0x30FB through 0x35FB - Standard Huffman+RLE variants</item>
/// <item>0xB0FB through 0xB5FB - Extended Huffman+RLE variants with 32-bit fields</item>
/// </list>
/// </para>
/// <para>
/// The format structure is determined by analyzing the signature's flag bits:
/// <list type="bullet">
/// <item>Bit 0x8000: Determines field width (4 bytes if set, 3 bytes otherwise)</item>
/// <item>Bit 0x0100: Indicates presence of additional metadata block before size field</item>
/// <item>Low nibble (0x000F): Specifies compression variant/algorithm parameters</item>
/// </list>
/// </para>
/// <para>
/// This implementation is distributed across multiple partial class files:
/// <list type="bullet">
/// <item>Core metadata and validation (HuffmanWithRunlengthCodexData.cs)</item>
/// <item>Decode operations (HuffmanWithRunlengthCodexDecode.cs)</item>
/// <item>Encode operations (HuffmanWithRunlengthCodexEncode.cs)</item>
/// </list>
/// </para>
/// </remarks>
[PublicAPI]
public partial class HuffmanWithRunlengthCodex : ICodex
{
    /// <summary>
    /// Gets descriptive metadata about this Huffman with Run-length Encoding codex implementation.
    /// </summary>
    /// <value>
    /// Returns codex information with signature "HUFF", full encode/decode capabilities,
    /// no 32-bit field support (uses 3-byte fields by default), version 1.4, and human-readable type names.
    /// </value>
    /// <remarks>
    /// While this codec can handle formats with 32-bit fields (0xB0FB-0xB5FB variants),
    /// the <see cref="CodexCapabilities.Supports32BitFields"/> is set to <c>false</c>
    /// to indicate that the default behavior uses 3-byte size fields for compatibility.
    /// </remarks>
    public CodexInformation About =>
        new(
            Signature: new CodexSignature("HUFF"),
            Capabilities: new CodexCapabilities(
                CanDecode: true,
                CanEncode: true,
                Supports32BitFields: false
            ),
            Version: new Version(1, 4),
            ShortType: "huff",
            LongType: "Huffman"
        );

    /// <summary>
    /// Determines whether the provided buffer appears to contain Huffman+RLE compressed data
    /// by examining the format signature.
    /// </summary>
    /// <param name="compressedData">The candidate compressed data buffer to validate.</param>
    /// <returns>
    /// <c>true</c> if the buffer starts with a recognized Huffman+RLE signature; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Recognizes the following 16-bit big-endian signatures:
    /// </para>
    /// <para>
    /// <b>Standard variants (3-byte fields):</b>
    /// <list type="bullet">
    /// <item>0x30FB - Basic Huffman+RLE</item>
    /// <item>0x31FB - Huffman+RLE with metadata</item>
    /// <item>0x32FB - Huffman+RLE variant 2</item>
    /// <item>0x33FB - Huffman+RLE variant 3</item>
    /// <item>0x34FB - Huffman+RLE variant 4</item>
    /// <item>0x35FB - Huffman+RLE variant 5</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Extended variants (4-byte fields):</b>
    /// <list type="bullet">
    /// <item>0xB0FB - Extended Huffman+RLE</item>
    /// <item>0xB1FB - Extended Huffman+RLE with metadata</item>
    /// <item>0xB2FB - Extended Huffman+RLE variant 2</item>
    /// <item>0xB3FB - Extended Huffman+RLE variant 3</item>
    /// <item>0xB4FB - Extended Huffman+RLE variant 4</item>
    /// <item>0xB5FB - Extended Huffman+RLE variant 5</item>
    /// </list>
    /// </para>
    /// <para>
    /// Safe to call on partial buffers; returns <c>false</c> for insufficient data
    /// rather than throwing exceptions. This makes it suitable for format detection
    /// scenarios where the complete data may not be available.
    /// </para>
    /// </remarks>
    public bool IsValid(ReadOnlySpan<byte> compressedData)
    {
        if (compressedData.Length < 2)
        {
            return false;
        }

        var packType = BinaryPrimitives.ReadUInt16BigEndian(compressedData);
        return packType
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

    /// <summary>
    /// Extracts the expected decompressed size from the Huffman+RLE header information.
    /// </summary>
    /// <param name="compressedData">The Huffman+RLE compressed data buffer.</param>
    /// <returns>The expected size in bytes of the decompressed data.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the data is invalid, has an unrecognized signature, or is insufficient
    /// for size determination.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when the computed size exceeds <see cref="int.MaxValue"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The size field location and width are determined by analyzing the format signature:
    /// </para>
    /// <para>
    /// <b>Field Width Determination:</b>
    /// <list type="bullet">
    /// <item>4 bytes if bit 0x8000 is set in the signature (0xB0FB-0xB5FB variants)</item>
    /// <item>3 bytes otherwise (0x30FB-0x35FB variants)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Field Location:</b>
    /// <list type="bullet">
    /// <item>Directly after signature (offset 2) for basic variants</item>
    /// <item>After signature + metadata block for variants with bit 0x0100 set</item>
    /// <item>Metadata block size equals the field width (3 or 4 bytes)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Header Structure Examples:</b>
    /// <list type="bullet">
    /// <item><b>0x30FB:</b> [2-byte signature][3-byte size][compressed data]</item>
    /// <item><b>0x31FB:</b> [2-byte signature][3-byte metadata][3-byte size][compressed data]</item>
    /// <item><b>0xB0FB:</b> [2-byte signature][4-byte size][compressed data]</item>
    /// <item><b>0xB1FB:</b> [2-byte signature][4-byte metadata][4-byte size][compressed data]</item>
    /// </list>
    /// </para>
    /// <para>
    /// All multibyte integer values are stored in big-endian byte order as per EA format conventions.
    /// The method uses checked arithmetic to prevent integer overflow when reading size values.
    /// </para>
    /// </remarks>
    public int GetSize(ReadOnlySpan<byte> compressedData)
    {
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

        var packType = BinaryPrimitives.ReadUInt16BigEndian(compressedData);
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
