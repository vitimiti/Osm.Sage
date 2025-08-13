using System.Buffers.Binary;
using System.Text;

namespace Osm.Sage.Gimex;

public class Signature
{
    public uint Value { get; }

    public Signature(string value)
    {
        if (!value.All(char.IsAscii))
        {
            throw new ArgumentException("The signature must be ASCII", nameof(value));
        }

        Span<byte> bytes = stackalloc byte[4];
        bytes.Fill((byte)' ');
        Encoding.ASCII.GetBytes(value.AsSpan(0, int.Min(value.Length, 4)), bytes);
        Value = bytes.GetBigEndianValue(bytes.Length);
    }

    public IReadOnlyCollection<byte> ToBytes()
    {
        Span<byte> valueBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(valueBytes, Value);
        return valueBytes.ToArray();
    }
}
