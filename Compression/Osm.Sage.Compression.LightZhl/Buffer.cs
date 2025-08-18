using JetBrains.Annotations;
using Osm.Sage.Compression.LightZhl.Internals;

namespace Osm.Sage.Compression.LightZhl;

/// <summary>
/// Provides a circular sliding buffer used by LightZhl compression components.
/// </summary>
[PublicAPI]
public class Buffer
{
    /// <summary>
    /// Gets the underlying circular buffer storage with a fixed size of <see cref="Globals.BufSize"/>.
    /// </summary>
    protected byte[] Buf { get; } = new byte[Globals.BufSize];

    /// <summary>
    /// Gets the current write position in the logical stream of the circular buffer.
    /// The physical index into <see cref="Buf"/> is computed via <see cref="Wrap(uint)"/>.
    /// </summary>
    protected uint BufPos { get; private set; }

    /// <summary>
    /// Wraps a logical buffer position into a physical index within <see cref="Buf"/> using <see cref="Globals.BufMask"/>.
    /// </summary>
    /// <param name="pos">The logical position to wrap.</param>
    /// <returns>The wrapped index inside the buffer bounds.</returns>
    protected static int Wrap(uint pos) => (int)(pos & Globals.BufMask);

    /// <summary>
    /// Computes the circular distance for a difference value using <see cref="Globals.BufMask"/>.
    /// </summary>
    /// <param name="diff">The raw difference between two logical positions.</param>
    /// <returns>The masked distance within the circular buffer size.</returns>
    protected static int Distance(int diff) => diff & Globals.BufMask;

    /// <summary>
    /// Appends a single byte to the buffer at the current position, advancing <see cref="BufPos"/>.
    /// </summary>
    /// <param name="c">The byte to append.</param>
    protected void ToBuf(byte c) => Buf[Wrap(BufPos++)] = c;

    /// <summary>
    /// Appends a span of bytes to the buffer, wrapping as necessary, and advances <see cref="BufPos"/> by the span length.
    /// </summary>
    /// <param name="source">The data to append. Its length must be strictly less than <see cref="Globals.BufSize"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="source"/> length is greater than or equal to <see cref="Globals.BufSize"/>.</exception>
    protected void ToBuf(ReadOnlySpan<byte> source)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(source.Length, Globals.BufSize);
        var begin = Wrap(BufPos);
        var end = begin + source.Length;
        if (end > Globals.BufSize)
        {
            var left = Globals.BufSize - begin;
            source[..left].CopyTo(Buf.AsSpan()[begin..]);
            source[left..].CopyTo(Buf);
        }
        else
        {
            source.CopyTo(Buf.AsSpan()[begin..]);
        }

        BufPos += (uint)source.Length;
    }

    /// <summary>
    /// Copies a contiguous range from the circular buffer into the destination span, starting at a logical position and length.
    /// The copy wraps around the buffer boundary if necessary.
    /// </summary>
    /// <param name="dest">The destination span to receive data.</param>
    /// <param name="pos">The logical start position in the buffer to copy from.</param>
    /// <param name="len">The number of bytes to copy. Must be strictly less than <see cref="Globals.BufSize"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="len"/> is greater than or equal to <see cref="Globals.BufSize"/>.</exception>
    protected void BufCpy(Span<byte> dest, int pos, int len)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(len, Globals.BufSize);
        var begin = Wrap((uint)pos);
        var end = begin + len;
        if (end > Globals.BufSize)
        {
            var left = Globals.BufSize - begin;
            Buf.AsSpan(begin, left).CopyTo(dest);
            Buf.AsSpan(0, len - left).CopyTo(dest[left..]);
        }
        else
        {
            Buf.AsSpan(begin, len).CopyTo(dest);
        }
    }

    /// <summary>
    /// Computes the length of the match between the buffer contents starting at a physical position
    /// and the provided pattern, up to a specified limit, allowing wrap-around.
    /// </summary>
    /// <param name="pos">The physical starting index within the buffer (already wrapped).</param>
    /// <param name="p">The pattern to match against.</param>
    /// <param name="limit">The maximum number of bytes to compare. Must be strictly less than <see cref="Globals.BufSize"/>.</param>
    /// <returns>The number of consecutive matching bytes, up to <paramref name="limit"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="limit"/> is greater than or equal to <see cref="Globals.BufSize"/>.</exception>
    protected int Match(int pos, ReadOnlySpan<byte> p, int limit)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(limit, Globals.BufSize);

        var contiguous = Math.Min(limit, Globals.BufSize - pos);
        var first = FirstMismatch(Buf.AsSpan(pos, contiguous), p, contiguous);
        if (first != contiguous)
            return first;

        var remaining = limit - contiguous;
        if (remaining == 0)
            return limit;

        var second = FirstMismatch(Buf.AsSpan(0, remaining), p[first..], remaining);
        return first + second;
    }

    /// <summary>
    /// Moves the logical write position backwards by the specified amount.
    /// </summary>
    /// <param name="n">The number of bytes to rewind.</param>
    protected void Rewind(uint n) => BufPos -= n;

    private static int FirstMismatch(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, int len)
    {
        for (var i = 0; i < len; i++)
        {
            if (a[i] != b[i])
                return i;
        }
        return len;
    }
}
