using System.Buffers.Binary;
using System.Text;
using JetBrains.Annotations;

namespace Osm.Sage.Gimex;

/// <summary>
/// Represents a 4-byte signature used for identifying file formats or data structures.
/// The signature is stored as a big-endian 32-bit unsigned integer value.
/// </summary>
[PublicAPI]
public class Signature
{
    /// <summary>
    /// Gets the signature value as a 32-bit unsigned integer in big-endian format.
    /// </summary>
    /// <value>The signature represented as a big-endian uint32 value.</value>
    public uint Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Signature"/> class from an ASCII string.
    /// </summary>
    /// <param name="value">The ASCII string to convert to a signature. Must contain only ASCII characters.
    /// If shorter than 4 characters, it will be padded with spaces. If longer than 4 characters,
    /// only the first 4 characters will be used.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> contains non-ASCII characters.</exception>
    /// <remarks>
    /// The string is converted to bytes using ASCII encoding, padded or truncated to exactly 4 bytes,
    /// and then interpreted as a big-endian 32-bit unsigned integer.
    /// </remarks>
    public Signature(string value)
    {
        if (!value.All(char.IsAscii))
        {
            throw new ArgumentException("The signature must be ASCII", nameof(value));
        }

        Span<byte> bytes = stackalloc byte[4];
        bytes.Fill((byte)' ');
        Encoding.ASCII.GetBytes(value.AsSpan(0, int.Min(value.Length, 4)), bytes);
        Value = BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }

    /// <summary>
    /// Converts the signature value back to its byte representation.
    /// </summary>
    /// <returns>A read-only collection containing the 4 bytes of the signature in big-endian order.</returns>
    /// <remarks>
    /// The returned bytes represent the signature value as it would appear in binary data,
    /// with the most significant byte first (big-endian format).
    /// </remarks>
    public IReadOnlyCollection<byte> ToBytes()
    {
        Span<byte> valueBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(valueBytes, Value);
        return valueBytes.ToArray();
    }
}
