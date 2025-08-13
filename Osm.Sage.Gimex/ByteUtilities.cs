using System.Buffers.Binary;
using JetBrains.Annotations;

namespace Osm.Sage.Gimex;

[PublicAPI]
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

    public static uint GetBigEndianValue(this Span<byte> source, int byteCount = 1) =>
        ((ReadOnlySpan<byte>)source).GetBigEndianValue(byteCount);

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

    public static void AppendBigEndianValue(
        this List<byte> destination,
        uint value,
        int byteCount = 1
    )
    {
        switch (byteCount)
        {
            case 1:
                destination.Add((byte)value);
                break;
            case 2:
                Span<byte> temp2 = stackalloc byte[2];
                BinaryPrimitives.WriteUInt16BigEndian(temp2, (ushort)value);
                destination.AddRange(temp2);
                break;
            case 3:
                destination.AddRange([(byte)(value >> 16), (byte)(value >> 8), (byte)value]);
                break;
            default:
                Span<byte> temp4 = stackalloc byte[4];
                BinaryPrimitives.WriteUInt32BigEndian(temp4, value);
                destination.AddRange(temp4);
                break;
        }
    }
}
