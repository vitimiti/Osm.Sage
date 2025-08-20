using System.Buffers.Binary;
using System.IO.Compression;
using JetBrains.Annotations;
using Osm.Sage.Compression.Eac.Codex;
using Osm.Sage.Compression.Nox;

namespace Osm.Sage.Compression;

/// <summary>
/// High-level utilities to detect, compress and decompress buffers using multiple formats.
/// </summary>
[PublicAPI]
public static class CompressionManager
{
    /// <summary>
    /// Determines whether the provided buffer appears to be compressed in a supported format.
    /// </summary>
    /// <param name="data">The buffer to inspect.</param>
    /// <returns>
    /// <see langword="true"/> if <see cref="GetCompressionType(System.ReadOnlySpan{byte})"/> detects a known
    /// compression header; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsCompressed(ReadOnlySpan<byte> data) =>
        GetCompressionType(data) is not CompressionType.None;

    /// <summary>
    /// Detects the compression type of the given buffer based on its 4-byte magic.
    /// </summary>
    /// <param name="data">The buffer to inspect. Only the first 4–8 bytes are required.</param>
    /// <returns>
    /// A <see cref="CompressionType"/> value indicating the detected format, or
    /// <see cref="CompressionType.None"/> if the buffer does not match a supported header.
    /// </returns>
    public static CompressionType GetCompressionType(ReadOnlySpan<byte> data)
    {
        if (data.Length < 8)
        {
            return CompressionType.None;
        }

        var magic = data[..4];

        // Handle ZLib levels generically: "ZL{1-9}\0"
        if (
            magic[0] == (byte)'Z'
            && magic[1] == (byte)'L'
            && magic[3] == 0
            && magic[2] >= (byte)'1'
            && magic[2] <= (byte)'9'
        )
        {
            return CompressionType.ZLib1 + (magic[2] - (byte)'1');
        }

        // Handle the remaining fixed magics
        return (magic[0], magic[1], magic[2], magic[3]) switch
        {
            ((byte)'N', (byte)'O', (byte)'X', 0) => CompressionType.NoxLzh,
            ((byte)'E', (byte)'A', (byte)'B', 0) => CompressionType.BinaryTree,
            ((byte)'E', (byte)'A', (byte)'H', 0) => CompressionType.HuffmanWithRunlength,
            ((byte)'E', (byte)'A', (byte)'R', 0) => CompressionType.Refpack,
            _ => CompressionType.None,
        };
    }

    /// <summary>
    /// Compresses the provided data using the specified compression type and writes a common 8-byte header.
    /// </summary>
    /// <param name="compressionType">The compression algorithm to use.</param>
    /// <param name="data">The input data to compress.</param>
    /// <returns>
    /// A new byte array containing:
    /// - 4-byte magic identifying the algorithm,
    /// - 4-byte big-endian original length,
    /// - followed by the algorithm-specific payload.
    /// If the type is unsupported, the original data is returned unchanged.
    /// </returns>
    public static byte[] Compress(CompressionType compressionType, ReadOnlySpan<byte> data)
    {
        switch (compressionType)
        {
            case CompressionType.BinaryTree:
            {
                using var output = CreateOutputWithHeader("EAB\0"u8);
                BinaryTreeCodex codex = new();
                var compressed = codex.Encode(data);
                output.Write(compressed);
                return FinalizeWithLength(output, data.Length);
            }
            case CompressionType.HuffmanWithRunlength:
            {
                using var output = CreateOutputWithHeader("EAH\0"u8);
                HuffmanWithRunlengthCodex codex = new();
                var compressed = codex.Encode(data);
                output.Write(compressed);
                return FinalizeWithLength(output, data.Length);
            }
            case CompressionType.Refpack:
            {
                using var output = CreateOutputWithHeader("EAR\0"u8);
                RefpackCodex codex = new();
                var compressed = codex.Encode(data);
                output.Write(compressed);
                return FinalizeWithLength(output, data.Length);
            }
            case CompressionType.NoxLzh:
            {
                using var output = CreateOutputWithHeader("NOX\0"u8);

                var compressed = Compressor.CompressMemory(
                    new ReadOnlyMemory<byte>(data.ToArray())
                );
                output.Write(compressed.ToArray());

                return FinalizeWithLength(output, data.Length);
            }
            case CompressionType.ZLib1
            or CompressionType.ZLib2
            or CompressionType.ZLib3
            or CompressionType.ZLib4
            or CompressionType.ZLib5
            or CompressionType.ZLib6
            or CompressionType.ZLib7
            or CompressionType.ZLib8
            or CompressionType.ZLib9:
            {
                var level = (int)compressionType - (int)CompressionType.ZLib1 + 1;
                var magic = "ZL0\0"u8.ToArray();
                magic[2] = (byte)('0' + level);

                using var output = CreateOutputWithHeader(magic);
                var compressionLevel = MapZLibLevel(level);

                using (ZLibStream zlib = new(output, compressionLevel, leaveOpen: true))
                {
                    zlib.Write(data);
                }

                return FinalizeWithLength(output, data.Length);
            }
        }

        return data.ToArray();
    }

    /// <summary>
    /// Decompresses a buffer that starts with a supported 8-byte header.
    /// </summary>
    /// <param name="data">
    /// The buffer to decompress. It should begin with a 4-byte magic and a 4-byte big-endian original length.
    /// </param>
    /// <returns>
    /// The decompressed data if the header indicates a supported format; otherwise, the original buffer is returned.
    /// </returns>
    public static byte[] Decompress(ReadOnlySpan<byte> data)
    {
        var payload = data.Length > 8 ? data[8..] : ReadOnlySpan<byte>.Empty;

        return GetCompressionType(data) switch
        {
            CompressionType.BinaryTree => new BinaryTreeCodex().Decode(payload).ToArray(),
            CompressionType.HuffmanWithRunlength => new HuffmanWithRunlengthCodex()
                .Decode(payload)
                .ToArray(),
            CompressionType.Refpack => new RefpackCodex().Decode(payload).ToArray(),
            CompressionType.NoxLzh => Decompressor
                .DecompressMemory(new ReadOnlyMemory<byte>(payload.ToArray()))
                .ToArray(),
            >= CompressionType.ZLib1 and <= CompressionType.ZLib9 => DecompressZLib(payload),
            _ => data.ToArray(),
        };
    }

    private static MemoryStream CreateOutputWithHeader(ReadOnlySpan<byte> magic)
    {
        MemoryStream output = new();
        output.Write(magic);

        // Reserve 4 bytes for the original length
        output.Write(new byte[4]);
        return output;
    }

    private static byte[] FinalizeWithLength(MemoryStream output, int originalLength)
    {
        output.Position = 4;
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, originalLength);

        output.Write(length);
        return output.ToArray();
    }

    private static CompressionLevel MapZLibLevel(int level) =>
        level switch
        {
            <= 3 => CompressionLevel.Fastest,
            <= 7 => CompressionLevel.Optimal,
            _ => CompressionLevel.SmallestSize,
        };

    private static byte[] DecompressZLib(ReadOnlySpan<byte> payload)
    {
        using MemoryStream input = new(payload.ToArray());
        using ZLibStream zlib = new(input, CompressionMode.Decompress, leaveOpen: true);
        using MemoryStream output = new();

        zlib.CopyTo(output);
        return output.ToArray();
    }
}
