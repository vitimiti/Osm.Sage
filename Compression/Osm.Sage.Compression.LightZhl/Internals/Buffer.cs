namespace Osm.Sage.Compression.LightZhl.Internals;

internal class Buffer
{
    protected byte[] Buf { get; } = new byte[Globals.BufSize];
    protected uint BufPos { get; private set; } = 0;

    protected static int Wrap(uint pos) => (int)(pos & Globals.BufMask);

    protected static int Distance(int diff) => diff & Globals.BufMask;

    protected void ToBuf(byte c) => Buf[Wrap(BufPos++)] = c;

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
