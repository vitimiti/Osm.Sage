namespace Osm.Sage.Compression.LightZhl.Options;

/// <summary>
/// Specifies tuning switches for the LightZhl compression encoder.
/// These options control match-finding heuristics and trade-offs between speed and compression ratio.
/// </summary>
public record CompressionOptions()
{
    /// <summary>
    /// Gets a value indicating whether to use a slower, more thorough hashing strategy
    /// to find longer or more optimal matches at the cost of encoding speed.
    /// </summary>
    public bool SlowHash { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to enable lazy matching, which defers emitting
    /// a current match to check if a longer match begins at the next position.
    /// This can improve compression ratio with a modest speed impact.
    /// </summary>
    public bool LazyMatch { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether overlapping matches are permitted when copying data.
    /// Allowing overlap can enable efficient repetition runs but may slightly increase complexity.
    /// </summary>
    public bool Overlap { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether backward-oriented match strategies are allowed during search.
    /// When enabled, the encoder may consider additional candidate matches discovered via backward traversal.
    /// </summary>
    public bool BackwardMatch { get; init; } = true;
}
