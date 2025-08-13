namespace Osm.Sage.Compression.Eac;

public record CodexCapabilities()
{
    public bool CanDecode { get; init; }
    public bool CanEncode { get; init; }
    public bool Supports32BitFields { get; init; }
}
