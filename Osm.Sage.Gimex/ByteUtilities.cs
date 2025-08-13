using System.Buffers.Binary;

namespace Osm.Sage.Gimex;

public static class ByteUtilities
{
    public static uint GetBigEndianValue(this ReadOnlySpan<byte> source, int byteCount = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(byteCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(byteCount, 4);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(byteCount, source.Length);

        return byteCount switch
        {
            1 => source[0],
            2 => BinaryPrimitives.ReadUInt16BigEndian(source),
            3 => (uint)(source[0] << 16 | source[1] << 8 | source[2]),
            _ => BinaryPrimitives.ReadUInt32BigEndian(source),
        };
    }

    public static void SetBigEndianValue(this Span<byte> destination, uint value, int byteCount = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(byteCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(byteCount, 4);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(byteCount, destination.Length);

        switch (byteCount)
        {
            case 1:
                destination[0] = (byte)value;
                break;
            case 2:
                BinaryPrimitives.WriteUInt16BigEndian(destination, (ushort)value);
                break;
            case 3:
                destination[0] = (byte)(value >> 16);
                destination[1] = (byte)(value >> 8);
                destination[2] = (byte)value;
                break;
            default:
                BinaryPrimitives.WriteUInt32BigEndian(destination, value);
                break;
        }
    }
}
