using System.Buffers.Binary;
using JetBrains.Annotations;
using Osm.Sage.Gimex;

namespace Osm.Sage.Compression.Eac.Codex;

/// <summary>
/// Implements a Huffman-with-run-length compression codec.
/// Provides methods to validate data, extract the uncompressed size, and perform encoding/decoding.
/// </summary>
[PublicAPI]
public partial class HuffmanWithRunlengthCodex : ICodex
{
    private int _deltaRuns;

    /// <summary>
    /// Gets information about this codec, including signature, capabilities, version, and type descriptors.
    /// </summary>
    public CodexInformation About =>
        new()
        {
            Signature = new Signature("HUFF"),
            Capabilities = new CodexCapabilities
            {
                CanDecode = true,
                CanEncode = true,
                Supports32BitFields = false,
            },
            Version = new Version(1, 4),
            ShortType = "huff",
            LongType = "Huffman",
        };

    /// <summary>
    /// Gets or sets the number of delta pre-processing passes applied before encoding.
    /// </summary>
    /// <remarks>
    /// The value is clamped to the range [0, 2]. Delta passes are only applied during <see cref="Encode(ReadOnlySpan{byte})"/>.
    /// </remarks>
    public int DeltaRuns
    {
        get => _deltaRuns;
        set => _deltaRuns = int.Clamp(value, 0, 2);
    }

    /// <summary>
    /// Validates whether the provided data is in a supported Huffman-with-run-length compressed format.
    /// </summary>
    /// <param name="compressedData">The compressed data to validate.</param>
    /// <returns><c>true</c> if the data header matches a supported format; otherwise, <c>false</c>.</returns>
    public bool IsValid(ReadOnlySpan<byte> compressedData)
    {
        if (compressedData.Length < 2)
        {
            return false;
        }

        return BinaryPrimitives.ReadUInt16BigEndian(compressedData)
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
    /// Extracts the size of the original uncompressed data from the compressed data header.
    /// </summary>
    /// <param name="compressedData">The compressed data containing the size information.</param>
    /// <returns>The size in bytes of the uncompressed data.</returns>
    /// <exception cref="ArgumentException">Thrown when the data is not recognized as valid for this codec.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the buffer is too short to contain the required header fields.</exception>
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
            // 31FB 33FB 35FB
            4 => (int)BinaryPrimitives.ReadUInt32BigEndian(compressedData[offset..]),
            // 30FB 32FB 34FB
            _ => compressedData[offset] << 16
                | BinaryPrimitives.ReadUInt16BigEndian(compressedData[(offset + 1)..]),
        };
    }

    /// <summary>
    /// Encodes (compresses) the provided uncompressed data using the Huffman-with-run-length algorithm.
    /// </summary>
    /// <param name="uncompressedData">The uncompressed data to encode.</param>
    /// <returns>An array containing the compressed data.</returns>
    /// <remarks>
    /// Depending on <see cref="DeltaRuns"/>, 0–2 delta passes may be applied before compression to improve ratio.
    /// </remarks>
    public byte[] Encode(ReadOnlySpan<byte> uncompressedData)
    {
        EncodingContext context = new();
        var src = uncompressedData.ToArray();
        switch (DeltaRuns)
        {
            case 1:
                src = DeltaOnce(src);
                break;
            case 2:
                src = DeltaOnce(src);
                src = DeltaOnce(src);
                break;
        }

        context.Buffer = src;
        context.FLength = src.Length;
        context.ULength = (uint)src.Length;

        MemStruct outFile = new();
        PackFile(ref context, src, ref outFile, context.FLength, DeltaRuns);

        return outFile.Buffer.ToArray();
    }

    /// <summary>
    /// Decodes (decompresses) the provided Huffman-with-run-length compressed data.
    /// </summary>
    /// <param name="compressedData">The compressed data to decode.</param>
    /// <returns>An array containing the uncompressed data.</returns>
    /// <exception cref="ArgumentException">Thrown when the data is not recognized as valid for this codec.</exception>
    public byte[] Decode(ReadOnlySpan<byte> compressedData)
    {
        if (!IsValid(compressedData))
        {
            throw new ArgumentException(
                $"The data is not a valid {nameof(HuffmanWithRunlengthCodex)}",
                nameof(compressedData)
            );
        }

        return Decompress(compressedData).ToArray();
    }
}
