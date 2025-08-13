using Osm.Sage.Gimex;

namespace Osm.Sage.Compression.Eac;

public record CodexInformation()
{
    public required Signature Signature { get; init; }
    public required CodexCapabilities Capabilities { get; init; }
    public required string ShortType { get; init; }
    public required string LongType { get; init; }
}
