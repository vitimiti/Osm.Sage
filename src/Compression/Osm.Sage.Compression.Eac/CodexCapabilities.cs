using JetBrains.Annotations;

namespace Osm.Sage.Compression.Eac;

/// <summary>
/// Describes the supported operations and field width of a codex implementation.
/// </summary>
/// <remarks>
/// This record is immutable and uses value-based equality (because it is a record).
/// </remarks>
/// <param name="CanDecode">
/// Indicates whether the codex can read/decompress data.
/// </param>
/// <param name="CanEncode">
/// Indicates whether the codex can write/compress data.
/// </param>
/// <param name="Supports32BitFields">
/// Indicates whether the codex supports format variants that use 32-bit fields
/// (for example, 32-bit offsets or lengths). If <c>false</c>, only narrower fields are supported.
/// </param>
[PublicAPI]
public record CodexCapabilities(bool CanDecode, bool CanEncode, bool Supports32BitFields);
