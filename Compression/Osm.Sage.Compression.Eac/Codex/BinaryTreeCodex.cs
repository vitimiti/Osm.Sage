using System.Buffers.Binary;
using JetBrains.Annotations;
using Osm.Sage.Gimex;

namespace Osm.Sage.Compression.Eac.Codex;

[PublicAPI]
public class BinaryTreeCodex : ICodex
{
    public CodexInformation About =>
        new()
        {
            Signature = new Signature("BTRE"),
            Capabilities = new CodexCapabilities
            {
                CanDecode = true,
                CanEncode = true,
                Supports32BitFields = false,
            },
            Version = new Version(1, 2),
            ShortType = "btr",
            LongType = "BTree",
        };

    public bool IsValid(ReadOnlySpan<byte> compressedData)
    {
        if (compressedData.Length < 2)
        {
            return false;
        }

        return BinaryPrimitives.ReadUInt16BigEndian(compressedData) is 0x46FB or 0x47FB;
    }

    public int ExtractSize(ReadOnlySpan<byte> compressedData)
    {
        if (!IsValid(compressedData))
        {
            throw new ArgumentException(
                $"The data is not a valid {nameof(BinaryTreeCodex)}",
                nameof(compressedData)
            );
        }

        var header = BinaryPrimitives.ReadUInt16BigEndian(compressedData);
        var offset = header is 0x46FB ? 2 : 5;
        return compressedData[offset] << 16
            | BinaryPrimitives.ReadUInt16BigEndian(compressedData[(offset + 1)..]);
    }

    public ICollection<byte> Encode(ReadOnlySpan<byte> uncompressedData)
    {
        throw new NotImplementedException();
    }

    public ICollection<byte> Decode(ReadOnlySpan<byte> compressedData)
    {
        if (!IsValid(compressedData))
        {
            throw new ArgumentException(
                $"The data is not a valid {nameof(BinaryTreeCodex)}",
                nameof(compressedData)
            );
        }

        int node;
        int i;
        int nodes;
        int clue;
        int unpackedLength;
        byte[] source = compressedData.ToArray();
        int sourceIndex = 0;
        sbyte clueValue;
        uint type;
        DecodingContext context = new();

        type = BinaryPrimitives.ReadUInt16BigEndian(source);
        sourceIndex += 2;

        // (Skip nothing for 0x46FB)
        if (type is 0x47FB) // Skip unpackedLength
        {
            sourceIndex += 3;
        }

        unpackedLength =
            (source[sourceIndex] << 16)
            | BinaryPrimitives.ReadUInt16BigEndian(source.AsSpan()[(sourceIndex + 1)..]);

        sourceIndex += 3;
        context.Destination.Capacity = unpackedLength;

        Array.Clear(context.ClueTable); // 0 means a code is a leaf

        clue = source[sourceIndex++];
        context.ClueTable[clue] = 1; // Mark the clue as special

        nodes = source[sourceIndex++];
        for (i = 0; i < nodes; ++i)
        {
            node = source[sourceIndex++];
            context.Left[node] = source[sourceIndex++];
            context.Right[node] = source[sourceIndex++];
            context.ClueTable[node] = -1;
        }

        while (sourceIndex < source.Length)
        {
            node = source[sourceIndex++];
            clueValue = context.ClueTable[node];
            if (clueValue == 0)
            {
                context.Destination.Add((byte)node);
                ++context.DestinationIndex;
                continue;
            }

            if (clueValue < 0)
            {
                Chase(ref context, context.Left[node]);
                Chase(ref context, context.Right[node]);
                continue;
            }

            if (sourceIndex >= source.Length)
            {
                break;
            }

            node = source[sourceIndex++];
            if (node != 0)
            {
                context.Destination.Add((byte)node);
                ++context.DestinationIndex;
                continue;
            }

            break;
        }

        return context.Destination;
    }

    #region Decoding Utilities

    private struct DecodingContext()
    {
        public sbyte[] ClueTable { get; } = new sbyte[256];
        public byte[] Left { get; } = new byte[256];
        public byte[] Right { get; } = new byte[256];
        public List<byte> Destination { get; } = [];
        public int DestinationIndex { get; set; }
    }

    private static void Chase(ref DecodingContext context, byte node)
    {
        // Use the stack to avoid the recursion like in the original code
        var stack = new Stack<byte>();
        stack.Push(node);

        while (stack.Count > 0)
        {
            byte currentNode = stack.Pop();

            switch (context.ClueTable[currentNode])
            {
                case 0:
                    // This is a leaf node - add it to destination
                    context.Destination.Add(currentNode);
                    ++context.DestinationIndex;
                    break;
                case < 0:
                    // This is an internal node - push children in reverse order
                    // (right first, then left, so left gets processed first)
                    stack.Push(context.Right[currentNode]);
                    stack.Push(context.Left[currentNode]);
                    break;
            }
            // If ClueTable[currentNode] > 0, it's the special clue node, skip it
        }
    }

    #endregion
}
