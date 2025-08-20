using JetBrains.Annotations;

namespace Osm.Sage.Compression;

[PublicAPI]
public enum CompressionType
{
    None,
    Refpack,
    NoxLzh,
    ZLib1,
    ZLib2,
    ZLib3,
    ZLib4,
    ZLib5,
    ZLib6,
    ZLib7,
    ZLib8,
    ZLib9,
    BinaryTree,
    HuffmanWithRunlength,
}
