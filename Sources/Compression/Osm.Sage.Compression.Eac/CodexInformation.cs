using JetBrains.Annotations;
using Osm.Sage.Gimex;

namespace Osm.Sage.Compression.Eac;

/// <summary>
/// Represents information about a compression codec, including its signature, capabilities, version, and type descriptions.
/// </summary>
[PublicAPI]
public record CodexInformation()
{
    /// <summary>
    /// Gets the signature that identifies the codec format.
    /// </summary>
    public required Signature Signature { get; init; }

    /// <summary>
    /// Gets the capabilities of the codec, indicating what operations it supports.
    /// </summary>
    public required CodexCapabilities Capabilities { get; init; }

    /// <summary>
    /// Gets the version of the codec.
    /// </summary>
    public required Version Version { get; init; }

    /// <summary>
    /// Gets the short type identifier for the codec.
    /// </summary>
    public required string ShortType { get; init; }

    /// <summary>
    /// Gets the long type description for the codec.
    /// </summary>
    public required string LongType { get; init; }
}
