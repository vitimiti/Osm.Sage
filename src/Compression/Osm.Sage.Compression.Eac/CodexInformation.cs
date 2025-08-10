using JetBrains.Annotations;

namespace Osm.Sage.Compression.Eac;

/// <summary>
/// Aggregates descriptive metadata about a codex implementation.
/// </summary>
/// <remarks>
/// This immutable record uses value-based equality and is suitable for exposing codex
/// capabilities and identity information to callers, UI, or registries.
/// </remarks>
/// <param name="Signature">
/// The 4-character ASCII identifier that uniquely represents the codex.
/// </param>
/// <param name="Capabilities">
/// The supported operations and field-width characteristics of the codex.
/// </param>
/// <param name="Version">
/// The codex version (e.g., library or format version).
/// </param>
/// <param name="ShortType">
/// A concise, human-friendly type name (e.g., for labels or lists).
/// </param>
/// <param name="LongType">
/// A longer, descriptive type name (e.g., for detailed tooltips or logs).
/// </param>
[PublicAPI]
public record CodexInformation(
    CodexSignature Signature,
    CodexCapabilities Capabilities,
    Version Version,
    string ShortType,
    string LongType
);
