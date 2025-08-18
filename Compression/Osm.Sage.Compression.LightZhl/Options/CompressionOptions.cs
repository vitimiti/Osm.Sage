namespace Osm.Sage.Compression.LightZhl.Options;

public record CompressionOptions()
{
    public bool SlowHash { get; init; } = true;
    public bool LazyMatch { get; init; } = true;
    public bool Overlap { get; init; } = true;
    public bool BackwardMatch { get; init; } = true;
}
