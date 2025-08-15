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

        DecodingContext context = new() { Source = compressedData.ToArray() };

        PopulateSize(ref context);
        InitializeClueTable(ref context);
        ProcessNodes(ref context);
        TraverseFile(ref context);

        return context.Destination;
    }

    #region Decoding Utilities

    private struct DecodingContext()
    {
        public byte[] Source { get; init; }
        public int SourceIndex { get; set; }
        public List<byte> Destination { get; } = [];
        public int DestinationIndex { get; set; }
        public sbyte[] ClueTable { get; } = new sbyte[256];
        public byte[] Left { get; } = new byte[256];
        public byte[] Right { get; } = new byte[256];
        public int Node { get; set; }
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

    private static void PopulateSize(ref DecodingContext context)
    {
        uint type = BinaryPrimitives.ReadUInt16BigEndian(context.Source);
        context.SourceIndex += 2;

        // (Skip nothing for 0x46FB)
        if (type is 0x47FB) // Skip unpackedLength
        {
            context.SourceIndex += 3;
        }

        int unpackedLength =
            (context.Source[context.SourceIndex] << 16)
            | BinaryPrimitives.ReadUInt16BigEndian(
                context.Source.AsSpan()[(context.SourceIndex + 1)..]
            );

        context.SourceIndex += 3;
        context.Destination.Capacity = unpackedLength;
    }

    private static void InitializeClueTable(ref DecodingContext context)
    {
        Array.Clear(context.ClueTable); // 0 means a code is a leaf
        var clue = context.Source[context.SourceIndex++];
        context.ClueTable[clue] = 1; // Mark the clue as special
    }

    private static void ProcessNodes(ref DecodingContext context)
    {
        var nodes = context.Source[context.SourceIndex++];
        for (var i = 0; i < nodes; ++i)
        {
            context.Node = context.Source[context.SourceIndex++];
            context.Left[context.Node] = context.Source[context.SourceIndex++];
            context.Right[context.Node] = context.Source[context.SourceIndex++];
            context.ClueTable[context.Node] = -1;
        }
    }

    private static void TraverseFile(ref DecodingContext context)
    {
        while (context.SourceIndex < context.Source.Length)
        {
            context.Node = context.Source[context.SourceIndex++];
            sbyte clueValue = context.ClueTable[context.Node];
            switch (clueValue)
            {
                case 0:
                    context.Destination.Add((byte)context.Node);
                    ++context.DestinationIndex;
                    continue;
                case < 0:
                    Chase(ref context, context.Left[context.Node]);
                    Chase(ref context, context.Right[context.Node]);
                    continue;
            }

            if (context.SourceIndex >= context.Source.Length)
            {
                break;
            }

            context.Node = context.Source[context.SourceIndex++];
            if (context.Node != 0)
            {
                context.Destination.Add((byte)context.Node);
                ++context.DestinationIndex;
                continue;
            }

            break;
        }
    }

    #endregion
}
