using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Osm.Sage.Compression.LightZhl;

/// <summary>
/// Represents a temporary Huffman statistic structure used for data compression processes.
/// </summary>
/// <remarks>
/// This structure is primarily used to store and compare statistical information
/// for Huffman encoding. It implements <see cref="IComparable{HuffStatTmpStruct}"/>
/// and provides custom comparison logic to facilitate sorting and equality checks.
/// Instances of this structure are compared based on their frequency (`N`) and index (`I`).
/// </remarks>
[PublicAPI]
public struct HuffStatTmpStruct : IComparable<HuffStatTmpStruct>, IComparable
{
    /// <summary>
    /// Gets or sets a value representing an index or identifier used in the Huffman statistical structure
    /// for compression-related operations.
    /// </summary>
    public short I { get; set; }

    /// <summary>
    /// Gets or sets the frequency associated with a Huffman symbol
    /// within the temporary Huffman statistic structure.
    /// </summary>
    public short N { get; set; }

    /// <summary>
    /// Compares the current instance with another instance of <see cref="HuffStatTmpStruct"/> to determine their relative order.
    /// </summary>
    /// <param name="other">
    /// The other <see cref="HuffStatTmpStruct"/> instance to compare with this instance.
    /// </param>
    /// <returns>
    /// An integer that indicates the relative order of the instances being compared:
    /// - A negative value if this instance precedes <paramref name="other"/> in the sort order.
    /// - Zero if this instance is equal to <paramref name="other"/> in the sort order.
    /// - A positive value if this instance follows <paramref name="other"/> in the sort order.
    /// </returns>
    public int CompareTo(HuffStatTmpStruct other)
    {
        var cmp = other.N - N;
        return cmp != 0 ? cmp : other.I - I;
    }

    /// <summary>
    /// Compares the current instance with another instance of <see cref="HuffStatTmpStruct"/> to determine their relative order.
    /// </summary>
    /// <param name="obj">
    /// The other <see cref="HuffStatTmpStruct"/> instance to compare with this instance.
    /// </param>
    /// <returns>
    /// An integer that indicates the relative order of the instances being compared:
    /// - A negative value if this instance precedes <paramref name="obj"/> in the sort order.
    /// - Zero if this instance is equal to <paramref name="obj"/> in the sort order.
    /// - A positive value if this instance follows <paramref name="obj"/> in the sort order.
    /// </returns>
    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        return obj is HuffStatTmpStruct other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(HuffStatTmpStruct)}");
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="HuffStatTmpStruct"/> instance.
    /// </summary>
    /// <param name="obj">
    /// The object to compare with the current <see cref="HuffStatTmpStruct"/> instance.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified object is equal to the current instance; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not HuffStatTmpStruct other)
        {
            return false;
        }

        return this.CompareTo((HuffStatTmpStruct?)other) == 0;
    }

    /// <summary>
    /// Returns a hash code for the current instance of <see cref="HuffStatTmpStruct"/>.
    /// </summary>
    /// <returns>
    /// A 32-bit signed integer hash code calculated based on the values of
    /// the <see cref="I"/> and <see cref="N"/> properties.
    /// </returns>
    public override int GetHashCode() => HashCode.Combine(I, N);

    /// <summary>
    /// Determines whether two instances of <see cref="HuffStatTmpStruct"/> are equal.
    /// </summary>
    /// <param name="left">
    /// The first <see cref="HuffStatTmpStruct"/> instance to compare.
    /// </param>
    /// <param name="right">
    /// The second <see cref="HuffStatTmpStruct"/> instance to compare.
    /// </param>
    /// <returns>
    /// <c>true</c> if the two instances are equal; otherwise, <c>false</c>.
    /// </returns>
    public static bool operator ==(HuffStatTmpStruct left, HuffStatTmpStruct right) =>
        left.CompareTo(right) == 0;

    /// <summary>
    /// Determines whether two instances of <see cref="HuffStatTmpStruct"/> are equal.
    /// </summary>
    /// <param name="left">
    /// The first <see cref="HuffStatTmpStruct"/> instance to compare.
    /// </param>
    /// <param name="right">
    /// The second <see cref="HuffStatTmpStruct"/> instance to compare.
    /// </param>
    /// <returns>
    /// <c>true</c> if the two <see cref="HuffStatTmpStruct"/> instances are equal; otherwise, <c>false</c>.
    /// </returns>
    public static bool operator !=(HuffStatTmpStruct left, HuffStatTmpStruct right) =>
        left.CompareTo(right) != 0;

    /// <summary>
    /// Determines whether one <see cref="HuffStatTmpStruct"/> instance is less than another.
    /// </summary>
    /// <param name="left">
    /// The first <see cref="HuffStatTmpStruct"/> instance to compare.
    /// </param>
    /// <param name="right">
    /// The second <see cref="HuffStatTmpStruct"/> instance to compare.
    /// </param>
    /// <returns>
    /// True if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, false.
    /// </returns>
    public static bool operator <(HuffStatTmpStruct left, HuffStatTmpStruct right) =>
        left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether a <see cref="HuffStatTmpStruct"/> instance is greater than another instance.
    /// </summary>
    /// <param name="left">
    /// The first <see cref="HuffStatTmpStruct"/> instance to compare.
    /// </param>
    /// <param name="right">
    /// The second <see cref="HuffStatTmpStruct"/> instance to compare.
    /// </param>
    /// <returns>
    /// True if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, false.
    /// </returns>
    public static bool operator >(HuffStatTmpStruct left, HuffStatTmpStruct right) =>
        left.CompareTo(right) > 0;

    /// <summary>
    /// Compares two instances of <see cref="HuffStatTmpStruct"/> to determine if the left operand is less than or equal to the right operand.
    /// </summary>
    /// <param name="left">
    /// The left <see cref="HuffStatTmpStruct"/> instance for comparison.
    /// </param>
    /// <param name="right">
    /// The right <see cref="HuffStatTmpStruct"/> instance for comparison.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the left instance is less than or equal to the right instance; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator <=(HuffStatTmpStruct left, HuffStatTmpStruct right) =>
        left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether the left-hand <see cref="HuffStatTmpStruct"/> instance is greater than or equal to the right-hand instance based on their relative order.
    /// </summary>
    /// <param name="left">
    /// The left-hand <see cref="HuffStatTmpStruct"/> instance to compare.
    /// </param>
    /// <param name="right">
    /// The right-hand <see cref="HuffStatTmpStruct"/> instance to compare.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the left-hand instance is greater than or equal to the right-hand instance; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator >=(HuffStatTmpStruct left, HuffStatTmpStruct right) =>
        left.CompareTo(right) >= 0;
}
