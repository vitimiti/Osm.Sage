using System.Buffers.Binary;
using JetBrains.Annotations;
using Osm.Sage.Gimex;

namespace Osm.Sage.Compression.Eac.Codex;

/// <summary>
/// Implements binary tree compression using the BTREE algorithm for compressing data.
/// This class provides methods to compress and decompress data using a binary tree approach.
/// </summary>
[PublicAPI]
public partial class BinaryTreeCodex : ICodex
{
    /// <summary>
    /// Gets information about this codex including its name, capabilities, and version.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the compression ratio used during encoding.
    /// Higher values may result in better compression at the cost of performance.
    /// </summary>
    public uint Ratio { get; set; } = 2;

    /// <summary>
    /// Gets or sets a value indicating whether to suppress zero bytes during compression.
    /// When enabled, sequences of zero bytes are handled more efficiently.
    /// </summary>
    public bool ZeroSuppress { get; set; }

    /// <summary>
    /// Validates whether the provided data is a valid binary tree compressed format.
    /// </summary>
    /// <param name="compressedData">The compressed data to validate.</param>
    /// <returns>True if the data is a valid binary tree compressed format; otherwise, false.</returns>
    public bool IsValid(ReadOnlySpan<byte> compressedData)
    {
        if (compressedData.Length < 2)
        {
            return false;
        }

        return BinaryPrimitives.ReadUInt16BigEndian(compressedData) is 0x46FB or 0x47FB;
    }

    /// <summary>
    /// Extracts the uncompressed size from the compressed data header.
    /// </summary>
    /// <param name="compressedData">The compressed data containing the size information.</param>
    /// <returns>The size of the original uncompressed data in bytes.</returns>
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
        return compressedData[offset] << 16
            | BinaryPrimitives.ReadUInt16BigEndian(compressedData[(offset + 1)..]);
    }

    /// <summary>
    /// Compresses the provided uncompressed data using the binary tree algorithm.
    /// </summary>
    /// <param name="uncompressedData">The data to compress.</param>
    /// <returns>A collection containing the compressed data.</returns>
    public ICollection<byte> Encode(ReadOnlySpan<byte> uncompressedData)
    {
        var context = new BTreeEncodeContext
        {
            Source = uncompressedData.ToArray(),
            SourceLength = uncompressedData.Length,
            Destination = [],
            PackBits = 0,
            WorkPattern = 0,
            PLen = 0,
        };

        InitializeMasks(ref context);
        return CompressFile(ref context);
    }

    /// <summary>
    /// Decompresses the provided compressed data encoded using the binary tree algorithm.
    /// </summary>
    /// <param name="compressedData">The compressed data to decompress.</param>
    /// <returns>A collection containing the original uncompressed data.</returns>
    public ICollection<byte> Decode(ReadOnlySpan<byte> compressedData)
    {
        if (!IsValid(compressedData))
        {
            throw new ArgumentException(
                $"The data is not a valid {nameof(BinaryTreeCodex)}",
                nameof(compressedData)
            );
        }

        DecodingContext context = new() { Source = compressedData.ToArray() };

        PopulateSize(ref context);
        InitializeClueTable(ref context);
        ProcessNodes(ref context);
        TraverseFile(ref context);

        return context.Destination;
    }
}
