namespace Osm.Sage.Compression.Eac;

public record CodexCapabilities()
{
    public required bool CanDecode { get; init; }
    public required bool CanEncode { get; init; }
    public required bool Supports32BitFields { get; init; }
}
