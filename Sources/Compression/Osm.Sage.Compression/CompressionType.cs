using JetBrains.Annotations;

namespace Osm.Sage.Compression;

/// <summary>
/// Represents the supported types of compression formats and levels.
/// </summary>
/// <remarks>
/// ZLib levels are grouped when selecting a <see cref="System.IO.Compression.CompressionLevel"/>:
/// - Levels 1–3 map to <see cref="System.IO.Compression.CompressionLevel.Fastest"/>
/// - Levels 4–7 map to <see cref="System.IO.Compression.CompressionLevel.Optimal"/>
/// - Levels 8–9 map to <see cref="System.IO.Compression.CompressionLevel.SmallestSize"/>
/// </remarks>
[PublicAPI]
public enum CompressionType
{
    /// <summary>
    /// No compression; data is stored uncompressed.
    /// </summary>
    None,

    /// <summary>
    /// RefPack compression format.
    /// </summary>
    Refpack,

    /// <summary>
    /// LZH-based compression variant used by Nox.
    /// </summary>
    NoxLzh,

    /// <summary>
    /// zlib/deflate level 1; maps to <see cref="System.IO.Compression.CompressionLevel.Fastest"/>.
    /// </summary>
    ZLib1,

    /// <summary>
    /// zlib/deflate level 2; maps to <see cref="System.IO.Compression.CompressionLevel.Fastest"/>.
    /// </summary>
    ZLib2,

    /// <summary>
    /// zlib/deflate level 3; maps to <see cref="System.IO.Compression.CompressionLevel.Fastest"/>.
    /// </summary>
    ZLib3,

    /// <summary>
    /// zlib/deflate level 4; maps to <see cref="System.IO.Compression.CompressionLevel.Optimal"/>.
    /// </summary>
    ZLib4,

    /// <summary>
    /// zlib/deflate level 5; maps to <see cref="System.IO.Compression.CompressionLevel.Optimal"/>.
    /// </summary>
    ZLib5,

    /// <summary>
    /// zlib/deflate level 6; maps to <see cref="System.IO.Compression.CompressionLevel.Optimal"/>.
    /// </summary>
    ZLib6,

    /// <summary>
    /// zlib/deflate level 7; maps to <see cref="System.IO.Compression.CompressionLevel.Optimal"/>.
    /// </summary>
    ZLib7,

    /// <summary>
    /// zlib/deflate level 8; maps to <see cref="System.IO.Compression.CompressionLevel.SmallestSize"/>.
    /// </summary>
    ZLib8,

    /// <summary>
    /// zlib/deflate level 9; maps to <see cref="System.IO.Compression.CompressionLevel.SmallestSize"/>.
    /// </summary>
    ZLib9,

    /// <summary>
    /// Compression using a binary-tree-based dictionary/matcher.
    /// </summary>
    BinaryTree,

    /// <summary>
    /// Compression using Huffman coding combined with run-length encoding (RLE).
    /// </summary>
    HuffmanWithRunlength,
}
