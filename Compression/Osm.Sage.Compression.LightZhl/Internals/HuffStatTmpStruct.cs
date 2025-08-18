using System.Diagnostics.CodeAnalysis;

namespace Osm.Sage.Compression.LightZhl.Internals;

internal struct HuffStatTmpStruct : IComparable<HuffStatTmpStruct>, IComparable
{
    public short I { get; set; }
    public short N { get; set; }

    public int CompareTo(HuffStatTmpStruct other)
    {
        var cmp = other.N - N;
        return cmp != 0 ? cmp : other.I - I;
    }

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

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not HuffStatTmpStruct other)
        {
            return false;
        }

        return this.CompareTo((HuffStatTmpStruct?)other) == 0;
    }

    public override int GetHashCode() => HashCode.Combine(I, N);

    public static bool operator ==(HuffStatTmpStruct left, HuffStatTmpStruct right) =>
        left.CompareTo(right) == 0;

    public static bool operator !=(HuffStatTmpStruct left, HuffStatTmpStruct right) =>
        left.CompareTo(right) != 0;

    public static bool operator <(HuffStatTmpStruct left, HuffStatTmpStruct right) =>
        left.CompareTo(right) < 0;

    public static bool operator >(HuffStatTmpStruct left, HuffStatTmpStruct right) =>
        left.CompareTo(right) > 0;

    public static bool operator <=(HuffStatTmpStruct left, HuffStatTmpStruct right) =>
        left.CompareTo(right) <= 0;

    public static bool operator >=(HuffStatTmpStruct left, HuffStatTmpStruct right) =>
        left.CompareTo(right) >= 0;
}
