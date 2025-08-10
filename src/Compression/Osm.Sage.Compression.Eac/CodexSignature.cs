using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using JetBrains.Annotations;

namespace Osm.Sage.Compression.Eac;

/// <summary>
/// Represents a 4-byte ASCII code (signature) packed into a 32-bit signed integer
/// using big-endian byte order.
/// </summary>
/// <remarks>
/// <para>
/// The input signature is normalized to exactly four ASCII bytes by:
/// <list type="bullet">
/// <item>Truncating to the first four characters if longer than four.</item>
/// <item>Left-padding with spaces (0x20) if shorter than four, so the space occupies the most significant byte(s).</item>
/// </list>
/// </para>
/// <para>
/// The normalized 4 bytes are then interpreted as a big-endian 32-bit signed integer.
/// Examples:
/// <list type="bullet">
/// <item><c>"REFPACK"</c> → <c>"REFP"</c> → <c>Value = 0x52_45_46_50</c></item>
/// <item><c>"TGA"</c> → <c>" TGA"</c> → <c>Value = 0x20_54_47_41</c></item>
/// </list>
/// </para>
/// <para>
/// This type is immutable and thread-safe.
/// </para>
/// </remarks>
/// <seealso cref="IEquatable{T}"/>
[PublicAPI]
public sealed class CodexSignature : IEquatable<CodexSignature>
{
    /// <summary>
    /// Gets the big-endian 32-bit signed integer computed from the 4 normalized ASCII bytes.
    /// </summary>
    /// <seealso cref="BinaryPrimitives.ReadInt32BigEndian(ReadOnlySpan{byte})"/>
    public int Value { get; }

    /// <summary>
    /// Gets the normalized 4-character ASCII signature used to compute <see cref="Value"/>.
    /// </summary>
    /// <remarks>
    /// Always exactly four ASCII characters. If the input has fewer than four characters,
    /// this is left-padded with spaces; if longer, it is truncated to the first four.
    /// </remarks>
    public string Signature { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodexSignature"/> class from an ASCII signature.
    /// </summary>
    /// <param name="signature">
    /// The source signature. Must contain only ASCII characters. Will be normalized to
    /// exactly four bytes as described in the remarks.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="signature"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="signature"/> contains non-ASCII characters.
    /// </exception>
    public CodexSignature(string signature)
    {
        ArgumentNullException.ThrowIfNull(signature);
        ArgumentOutOfRangeException.ThrowIfNotEqual(Ascii.IsValid(signature.AsSpan()), true);

        Span<byte> bytes = stackalloc byte[4];
        bytes.Fill((byte)' ');

        var signatureBytes = Encoding.ASCII.GetBytes(signature);
        var size = int.Min(signatureBytes.Length, bytes.Length);
        var start = 4 - size;

        signatureBytes[..size].CopyTo(bytes[start..]);

        Value = BinaryPrimitives.ReadInt32BigEndian(bytes);
        Signature = Encoding.ASCII.GetString(bytes);
    }

    /// <summary>
    /// Indicates whether the current instance is equal to another <see cref="CodexSignature"/> instance.
    /// </summary>
    /// <param name="other">The other signature to compare with this instance.</param>
    /// <returns>
    /// <c>true</c> if both <see cref="Value"/> and <see cref="Signature"/> are equal; otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(CodexSignature? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Value == other.Value && Signature == other.Signature;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="obj"/> is a <see cref="CodexSignature"/> and is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((CodexSignature)obj);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code combining <see cref="Value"/> and <see cref="Signature"/>.</returns>
    public override int GetHashCode() => HashCode.Combine(Value, Signature);

    /// <summary>
    /// Returns a string representation of the signature and its integer value.
    /// </summary>
    /// <returns>A string in the form <c>"ABCD (0xXXXXXXXX)"</c>.</returns>
    public override string ToString() => $"{Signature} (0x{Value:X8})";

    /// <summary>
    /// Deconstructs this instance into its constituent values.
    /// </summary>
    /// <param name="signature">The normalized 4-character ASCII signature.</param>
    /// <param name="value">The big-endian 32-bit signed integer representation.</param>
    public void Deconstruct(out string signature, out int value)
    {
        signature = Signature;
        value = Value;
    }

    /// <summary>
    /// Determines whether two <see cref="CodexSignature"/> instances are equal.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>
    /// <c>true</c> if both operands are equal (including when both are <c>null</c>); otherwise, <c>false</c>.
    /// </returns>
    public static bool operator ==(CodexSignature? left, CodexSignature? right) =>
        Equals(left, right);

    /// <summary>
    /// Determines whether two <see cref="CodexSignature"/> instances are not equal.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>
    /// <c>true</c> if the operands are not equal; otherwise, <c>false</c>.
    /// </returns>
    public static bool operator !=(CodexSignature? left, CodexSignature? right) =>
        !Equals(left, right);
}
