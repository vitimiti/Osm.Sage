using JetBrains.Annotations;

namespace Osm.Sage.Compression.Eac;

/// <summary>
/// Represents the capabilities of a compression codec, indicating what operations it supports.
/// </summary>
[PublicAPI]
public record CodexCapabilities()
{
    /// <summary>
    /// Gets a value indicating whether the codec can decode compressed data.
    /// </summary>
    public required bool CanDecode { get; init; }

    /// <summary>
    /// Gets a value indicating whether the codec can encode uncompressed data.
    /// </summary>
    public required bool CanEncode { get; init; }

    /// <summary>
    /// Gets a value indicating whether the codec supports 32-bit field operations.
    /// </summary>
    public required bool Supports32BitFields { get; init; }
}
