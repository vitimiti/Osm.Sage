using System.Buffers.Binary;
using JetBrains.Annotations;
using Osm.Sage.Gimex;

namespace Osm.Sage.Compression.Eac.Codex;

/// <summary>
/// Implements the Refpack compression codec, a lossless compression algorithm used by EA games.
/// Supports both encoding and decoding of data using the Refpack format with various packing types.
/// </summary>
[PublicAPI]
public partial class RefpackCodex : ICodex
{
    /// <summary>
    /// Gets information about this Refpack codec, including its signature, capabilities, version, and type descriptions.
    /// </summary>
    public CodexInformation About =>
        new()
        {
            Signature = new Signature("REF"),
            Capabilities = new CodexCapabilities
            {
                CanDecode = true,
                CanEncode = true,
                Supports32BitFields = true,
            },
            Version = new Version(1, 1),
            ShortType = "ref",
            LongType = "Refpack",
        };

    /// <summary>
    /// Gets or sets a value indicating whether to use quick encoding mode.
    /// Quick encoding is faster but may produce slightly larger compressed data.
    /// </summary>
    /// <value><c>true</c> to use quick encoding; <c>false</c> for slower but more thorough encoding.</value>
    public bool QuickEncoding { get; set; }

    /// <summary>
    /// Validates whether the provided data is valid Refpack compressed data by checking the header signature.
    /// </summary>
    /// <param name="compressedData">The compressed data to validate.</param>
    /// <returns><c>true</c> if the data has a valid Refpack signature; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// Valid Refpack signatures are: 0x10FB, 0x11FB, 0x90FB, or 0x91FB.
    /// </remarks>
    public bool IsValid(ReadOnlySpan<byte> compressedData) =>
        compressedData.Length switch
        {
            < 2 => false,
            _ => BinaryPrimitives.ReadUInt16BigEndian(compressedData)
                is 0x10FB
                    or 0x11FB
                    or 0x90FB
                    or 0x91FB,
        };

    /// <summary>
    /// Extracts the size of the uncompressed data from the Refpack compressed data header.
    /// </summary>
    /// <param name="compressedData">The compressed data from which to extract the uncompressed size.</param>
    /// <returns>The size in bytes of the uncompressed data.</returns>
    /// <exception cref="ArgumentException">Thrown when the compressed data is not valid Refpack data.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the compressed data is too short to contain a valid header.</exception>
    /// <remarks>
    /// The size extraction depends on the pack type flags in the header:
    /// - Bit 15 (0x8000) indicates whether the size field is 3 or 4 bytes
    /// - Bit 8 (0x0100) indicates whether there's an additional offset in the header
    /// </remarks>
    public int ExtractSize(ReadOnlySpan<byte> compressedData)
    {
        if (!IsValid(compressedData))
        {
            throw new ArgumentException(
                $"The data is not a valid {nameof(RefpackCodex)}",
                nameof(compressedData)
            );
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(compressedData.Length, 2);

        var packType = BinaryPrimitives.ReadUInt16BigEndian(compressedData);
        var byteCount = (packType & 0x8000) != 0 ? 4 : 3;
        var offset = (packType & 0x0100) != 0 ? 2 + byteCount : 2;

        ArgumentOutOfRangeException.ThrowIfLessThan(compressedData.Length, offset + byteCount);

        // 3 or 4 bytes
        return byteCount switch
        {
            4 => (int)BinaryPrimitives.ReadUInt32BigEndian(compressedData[offset..]),
            _ => compressedData[offset] << 16
                | BinaryPrimitives.ReadUInt16BigEndian(compressedData[(offset + 1)..]),
        };
    }

    /// <summary>
    /// Encodes (compresses) the provided uncompressed data using the Refpack compression algorithm.
    /// </summary>
    /// <param name="uncompressedData">The uncompressed data to encode.</param>
    /// <returns>A collection of bytes representing the Refpack compressed data.</returns>
    /// <remarks>
    /// The encoding process uses a hash table and sliding window approach to find repeated data patterns.
    /// The compression efficiency can be controlled using the <see cref="QuickEncoding"/> property.
    /// </remarks>
    public ICollection<byte> Encode(ReadOnlySpan<byte> uncompressedData)
    {
        EncodingContext context = new()
        {
            Source = uncompressedData.IsEmpty ? [] : uncompressedData.ToArray(),
        };

        PopulateSize(ref context);

        context.LoopLength = (uncompressedData.IsEmpty ? 0 : context.Source.Length) - 4;
        context.CurrentIndex = 0;
        context.ReferenceIndex = 0;
        context.Run = 0;

        Array.Fill(context.HashTable, -1);

        TraverseFile(ref context);

        return context.Destination;
    }

    /// <summary>
    /// Decodes (decompresses) the provided Refpack compressed data.
    /// </summary>
    /// <param name="compressedData">The Refpack compressed data to decode.</param>
    /// <returns>A collection of bytes representing the uncompressed data.</returns>
    /// <exception cref="ArgumentException">Thrown when the compressed data is not valid Refpack data.</exception>
    /// <remarks>
    /// The decoding process interprets various command forms in the compressed data:
    /// short form, int form, very int form, literals, and end-of-file markers.
    /// </remarks>
    public ICollection<byte> Decode(ReadOnlySpan<byte> compressedData)
    {
        if (!IsValid(compressedData))
        {
            throw new ArgumentException(
                $"The data is not a valid {nameof(RefpackCodex)}",
                nameof(compressedData)
            );
        }

        DecodingContext context = new()
        {
            Source = compressedData.IsEmpty ? [] : compressedData.ToArray(),
        };

        PopulateDestinationSize(ref context);
        TraverseFile(ref context);

        return context.Destination;
    }
}
