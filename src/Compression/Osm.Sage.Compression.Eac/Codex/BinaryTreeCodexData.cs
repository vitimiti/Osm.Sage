using System.Buffers.Binary;
using JetBrains.Annotations;

namespace Osm.Sage.Compression.Eac.Codex;

/// <summary>
/// Provides Binary Tree compression/decompression support for EA game formats.
/// </summary>
/// <remarks>
/// <para>
/// The Binary Tree codec is a compression algorithm used in various EA games to compress
/// data using a tree-based encoding scheme. This codex handles decompression of data
/// compressed with the Binary Tree format, which uses specific signature patterns to
/// identify and decode the compressed content.
/// </para>
/// <para>
/// The format supports two main variants identified by their 16-bit big-endian signatures:
/// <list type="bullet">
/// <item>0x46FB - Standard Binary Tree format</item>
/// <item>0x47FB - Binary Tree format variant</item>
/// </list>
/// </para>
/// <para>
/// The format structure consists of:
/// <list type="number">
/// <item>2-byte signature (0x46FB or 0x47FB)</item>
/// <item>Optional 3-byte metadata block (for 0x47FB variant)</item>
/// <item>3-byte uncompressed size field (big-endian)</item>
/// <item>Compressed data payload</item>
/// </list>
/// </para>
/// <para>
/// This implementation is distributed across multiple partial class files:
/// <list type="bullet">
/// <item>Core metadata and validation (BinaryTreeCodexData.cs)</item>
/// <item>Decode operations (BinaryTreeCodexDecode.cs)</item>
/// <item>Encode operations (BinaryTreeCodexEncode.cs)</item>
/// </list>
/// </para>
/// </remarks>
[PublicAPI]
public partial class BinaryTreeCodex : ICodex
{
    /// <summary>
    /// Gets descriptive metadata about this Binary Tree codex implementation.
    /// </summary>
    /// <value>
    /// Returns codex information with signature "BTRE", decode and encode capabilities,
    /// without 32-bit field support, version 1.2, and human-readable type names.
    /// </value>
    /// <remarks>
    /// The format uses only 3-byte size fields, so 32-bit field support is not required
    /// and <see cref="CodexCapabilities.Supports32BitFields"/> is <c>false</c>.
    /// </remarks>
    public CodexInformation About =>
        new(
            Signature: new CodexSignature("BTRE"),
            Capabilities: new CodexCapabilities(
                CanDecode: true,
                CanEncode: true,
                Supports32BitFields: false
            ),
            Version: new Version(1, 2),
            ShortType: "btr",
            LongType: "BTree"
        );

    /// <summary>
    /// Determines whether the provided buffer appears to contain Binary Tree compressed data
    /// by examining the format signature.
    /// </summary>
    /// <param name="compressedData">The candidate compressed data buffer to validate.</param>
    /// <returns>
    /// <c>true</c> if the buffer starts with a recognized Binary Tree signature; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs signature validation by reading the first 2 bytes as a
    /// 16-bit big-endian value and comparing against known Binary Tree format signatures:
    /// <list type="bullet">
    /// <item>0x46FB - Standard Binary Tree format</item>
    /// <item>0x47FB - Binary Tree format with additional metadata</item>
    /// </list>
    /// </para>
    /// <para>
    /// The method is safe to call on partial buffers and will return <c>false</c> for
    /// insufficient data rather than throwing exceptions. This makes it suitable for
    /// format detection scenarios where the complete data may not be available.
    /// </para>
    /// </remarks>
    public bool IsValid(ReadOnlySpan<byte> compressedData)
    {
        if (compressedData.Length < 2)
        {
            return false;
        }

        var header = BinaryPrimitives.ReadUInt16BigEndian(compressedData);
        return header is 0x46FB or 0x47FB;
    }

    /// <summary>
    /// Extracts the expected decompressed size from the Binary Tree header information.
    /// </summary>
    /// <param name="compressedData">The Binary Tree compressed data buffer.</param>
    /// <returns>The expected size in bytes of the decompressed data.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the data is invalid, has an unrecognized signature, or is not enough
    /// for size determination.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The size field location depends on the format variant identified by the signature:
    /// <list type="bullet">
    /// <item><b>0x46FB (Standard):</b> Size field at offset 2 (3 bytes big-endian)</item>
    /// <item><b>0x47FB (With metadata):</b> Size field at offset 5 (3 bytes big-endian)</item>
    /// </list>
    /// </para>
    /// <para>
    /// The 0x47FB variant includes a 3-byte metadata block between the signature and the
    /// size field, hence the different offset. The size field is always encoded as a
    /// 3-byte big-endian integer, limiting the maximum uncompressed size to 16MB.
    /// </para>
    /// <para>
    /// The method performs comprehensive validation including:
    /// <list type="number">
    /// <item>Format signature validation via <see cref="IsValid"/></item>
    /// <item>Minimum buffer size verification</item>
    /// <item>Sufficient data availability for size field reading</item>
    /// </list>
    /// </para>
    /// </remarks>
    public int GetSize(ReadOnlySpan<byte> compressedData)
    {
        if (!IsValid(compressedData))
        {
            throw new ArgumentException("Invalid compressed data", nameof(compressedData));
        }

        ReadOnlySpan<byte> slice;
        if (BinaryPrimitives.ReadUInt16BigEndian(compressedData) != 0x46FB)
        {
            if (compressedData.Length < 2)
            {
                throw new ArgumentException(
                    "The header is too small to read the size.",
                    nameof(compressedData)
                );
            }

            slice = compressedData[2..];
            return (slice[0] << 16) | (slice[1] << 8) | slice[2];
        }

        if (compressedData.Length < 5)
        {
            throw new ArgumentException(
                "The header is too small to read the size.",
                nameof(compressedData)
            );
        }

        slice = compressedData[(2 + 3)..];
        return (slice[0] << 16) | (slice[1] << 8) | slice[2];
    }
}
