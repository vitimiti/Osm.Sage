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

    public uint Ratio { get; set; } = 2;

    public bool ZeroSuppress { get; set; }

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
        var context = new BTreeEncodeContext
        {
            Source = uncompressedData.ToArray(),
            SourceLength = uncompressedData.Length,
            Destination = [],
            PackBits = 0,
            WorkPattern = 0,
            PLen = 0,
        };

        InitializeMasks(ref context);
        return CompressFile(ref context);
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

    #region Encoding Utilities

    private struct BTreeEncodeContext()
    {
        public uint PackBits { get; set; }
        public uint WorkPattern { get; set; }
        public byte[] Source { get; init; }
        public int SourceLength { get; init; }
        public List<byte> Destination { get; init; }
        public uint[] Masks { get; } = new uint[17];
        public byte[] ClueQueue { get; } = new byte[256];
        public byte[] Right { get; } = new byte[256];
        public byte[] Join { get; } = new byte[256];
        public uint PLen { get; set; }
        public byte[] BufBase { get; set; } = null!;
        public int BufEnd { get; set; }
        public byte[] Buffer { get; set; } = null!;
        public byte[] Buf1 { get; set; } = null!;
        public byte[] Buf2 { get; set; } = null!;
    }

    private struct FindBestContext
    {
        public short[] CountPtr;
        public byte[] TryQueue;
        public uint[] BestN;
        public uint[] BestVal;
        public int Ratio;
        public uint BestSize;
        public uint Threshold;
        public uint BaseIndex;
        public int CountIndex;
    }

    private struct ProcessingData
    {
        public uint[] Count2;
        public int Ratio;
        public byte[] TryQueue;
        public byte[] FreeQueue;
        public uint[] SortPtr;
        public uint Clue;
        public uint FreePtr;
    }

    private struct NodeArrays
    {
        public byte[] BestJoin;
        public uint[] BestN;
        public uint[] BestVal;
        public uint BtSize;
        public uint[] BtNode;
        public uint[] BtLeft;
        public uint[] BtRight;
    }

    private static void InitializeMasks(ref BTreeEncodeContext context)
    {
        context.Masks[0] = 0;
        for (int i = 1; i < 17; i++)
        {
            context.Masks[i] = (context.Masks[i - 1] << 1) + 1;
        }
    }

    private static void WriteBits(ref BTreeEncodeContext context, uint bitPattern, uint length)
    {
        // Loop to avoid recursion
        while (length > 0)
        {
            var currentLength = length > 16 ? 16 : length;
            var currentPattern =
                length > 16 ? (bitPattern >> (int)(length - 16)) & 0xFFFF : bitPattern;

            context.PackBits += currentLength;
            context.WorkPattern +=
                (currentPattern & context.Masks[currentLength]) << (int)(24 - context.PackBits);

            while (context.PackBits > 7)
            {
                context.Destination.Add((byte)(context.WorkPattern >> 16));
                context.WorkPattern <<= 8;
                context.PackBits -= 8;
                context.PLen++;
            }

            length -= currentLength;
        }
    }

    private static void AdjCount(byte[] s, int sIndex, int bend, short[] count)
    {
        if (sIndex >= s.Length)
        {
            return;
        }

        ushort i = s[sIndex++];
        while (sIndex < bend && sIndex < s.Length)
        {
            i = (ushort)((i << 8) | s[sIndex]);
            count[i]++;
            sIndex++;
        }
    }

    private static void ClearCount(byte[] tryQueue, short[] countBuf)
    {
        int tryIndex = 0;
        int countIndex = 0;

        for (int j = 0; j < 256; j++)
        {
            if (tryQueue[tryIndex] != 0)
            {
                tryQueue[tryIndex] = 1;

                // Clear 256 shorts (512 bytes) for this tryQueue entry
                // The original clears 128 ints which is 256 shorts
                for (int k = 0; k < 256; k++)
                {
                    countBuf[countIndex + k] = 0;
                }
            }

            // Skip 256 shorts for this tryQueue entry
            countIndex += 256;
            tryIndex++;
        }
    }

    private static void JoinNodes(
        ref BTreeEncodeContext context,
        byte[] cluePtr,
        byte[] rightPtr,
        byte[] joinPtr,
        uint clue
    )
    {
        byte[] source = context.BufBase;
        byte[] destination = context.BufBase == context.Buf1 ? context.Buf2 : context.Buf1;

        context.BufBase = destination;
        int sourceEnd = context.BufEnd;
        int sourceIndex = 0;
        int destIndex = 0;

        // Add sentinel value
        source[sourceEnd] = (byte)clue;

        while (sourceIndex <= sourceEnd)
        {
            // Copy bytes until we hit a clue
            do
            {
                destination[destIndex++] = source[sourceIndex++];
            } while (cluePtr[destination[destIndex - 1]] == 0);

            byte lastByte = source[sourceIndex - 1];
            byte clueValue = cluePtr[lastByte];

            switch (clueValue)
            {
                case 1:
                {
                    if (sourceIndex <= sourceEnd && source[sourceIndex] == rightPtr[lastByte])
                    {
                        // Replace with join value and skip next byte
                        destination[destIndex - 1] = joinPtr[lastByte];
                        sourceIndex++;
                    }

                    break;
                }
                case 3:
                    // Replace with clue and add original byte
                    destination[destIndex - 1] = (byte)clue;
                    destination[destIndex++] = lastByte;
                    break;
                default:
                    // Copy next byte for any other clue value
                    destination[destIndex++] = source[sourceIndex++];
                    break;
            }
        }

        context.BufEnd = destIndex - 2;
    }

    private static void InsertIntoSortedArray(
        uint[] bestN,
        uint[] bestVal,
        ref uint bestSize,
        uint nodeIndex,
        uint value
    )
    {
        // Find insertion position (descending order)
        var insertPos = bestSize;
        while (insertPos > 0 && bestVal[insertPos - 1] < value)
        {
            if (insertPos < bestN.Length)
            {
                bestN[insertPos] = bestN[insertPos - 1];
                bestVal[insertPos] = bestVal[insertPos - 1];
            }

            insertPos--;
        }

        // Insert new element
        if (insertPos < bestN.Length)
        {
            bestN[insertPos] = nodeIndex;
            bestVal[insertPos] = value;
        }

        // Increase size if we haven't reached maximum
        if (bestSize < 48)
        {
            bestSize++;
        }
    }

    private static void UpdateThresholdAndSize(
        uint[] bestVal,
        int ratio,
        ref uint bestSize,
        ref uint threshold
    )
    {
        // Remove entries below threshold
        while (bestSize > 1 && bestVal[bestSize - 1] < (bestVal[1] / (uint)ratio))
        {
            bestSize--;
        }

        // Update threshold based on current state
        if (bestSize < 48)
        {
            threshold = bestVal[1] / (uint)ratio;
        }
        else
        {
            threshold = bestVal[bestSize - 1];
        }
    }

    private static void ProcessInnerLoop(ref FindBestContext context)
    {
        for (int innerIndex = 0; innerIndex < 256; innerIndex++)
        {
            if (
                context.CountPtr[context.CountIndex] > context.Threshold
                && context.TryQueue[innerIndex] != 0
            )
            {
                uint currentValue = (uint)context.CountPtr[context.CountIndex];
                InsertIntoSortedArray(
                    context.BestN,
                    context.BestVal,
                    ref context.BestSize,
                    context.BaseIndex + (uint)innerIndex,
                    currentValue
                );

                UpdateThresholdAndSize(
                    context.BestVal,
                    context.Ratio,
                    ref context.BestSize,
                    ref context.Threshold
                );
            }

            context.CountIndex++;
        }
    }

    private static uint FindBest(
        short[] countPtr,
        byte[] tryQueue,
        uint[] bestN,
        uint[] bestVal,
        int ratio
    )
    {
        var context = new FindBestContext
        {
            CountPtr = countPtr,
            TryQueue = tryQueue,
            BestN = bestN,
            BestVal = bestVal,
            Ratio = ratio,
            BestSize = 1,
            Threshold = 3,
            BaseIndex = 0,
            CountIndex = 0,
        };

        for (int outerIndex = 0; outerIndex < 256; outerIndex++)
        {
            if (context.TryQueue[outerIndex] != 0)
            {
                ProcessInnerLoop(ref context);
            }
            else
            {
                context.CountIndex += 256;
            }
            context.BaseIndex += 256;
        }

        return context.BestSize;
    }

    private static void InitializeBuffersAndArrays(ref BTreeEncodeContext context)
    {
        const int btreeSlop = 16384;

        int buf1Size = context.SourceLength * 3 / 2 + btreeSlop;
        int buf2Size = context.SourceLength * 3 / 2 + btreeSlop;

        context.Buf1 = new byte[buf1Size];
        context.Buf2 = new byte[buf2Size];

        Array.Copy(context.Source, context.Buf1, context.SourceLength);
        context.Buffer = context.Buf1;
        context.BufBase = context.Buffer;
        context.BufEnd = context.SourceLength;
    }

    private static void InitializeQueuesAndSuppression(
        uint[] count2,
        int zeroSuppress,
        out byte[] tryQueue,
        out byte[] freeQueue
    )
    {
        const int btreeCodes = 256;
        const int btreeBigNum = 32000;

        tryQueue = new byte[btreeCodes];
        freeQueue = new byte[btreeCodes];

        for (int i = 0; i < btreeCodes; i++)
        {
            freeQueue[i] = 1;
            tryQueue[i] = count2[i] > 3 ? (byte)1 : (byte)0;
        }

        // Don't use 0 for clue or node
        count2[0] = btreeBigNum;

        // Zero suppression
        if (zeroSuppress == 0)
        {
            return;
        }

        for (int i = 0; i < 32; i++)
        {
            count2[i] = btreeBigNum;
            tryQueue[i] = 0;
            freeQueue[i] = 0;
        }
    }

    private static bool ShouldSwapElements(uint[] count2, uint[] sortPtr, uint i) =>
        count2[sortPtr[i]] < count2[sortPtr[i - 1]]
        || (count2[sortPtr[i]] == count2[sortPtr[i - 1]] && sortPtr[i] > sortPtr[i - 1]);

    private static uint[] CreateSortedCodeArray(uint[] count2)
    {
        const int btreeCodes = 256;

        var sortPtr = new uint[btreeCodes];
        for (uint i = 0; i < btreeCodes; i++)
        {
            sortPtr[i] = i;
        }

        bool hasSwapped;
        do
        {
            hasSwapped = false;
            for (uint i = 1; i < btreeCodes; i++)
            {
                if (ShouldSwapElements(count2, sortPtr, i))
                {
                    (sortPtr[i], sortPtr[i - 1]) = (sortPtr[i - 1], sortPtr[i]);
                    hasSwapped = true;
                }
            }
        } while (hasSwapped);

        return sortPtr;
    }

    private static void InitializeClueNode(
        ref BTreeEncodeContext context,
        ref ProcessingData data,
        out uint clue,
        out uint freePtr
    )
    {
        freePtr = 0;
        var clueIndex = data.SortPtr[freePtr++];
        clue = clueIndex;

        data.FreeQueue[clueIndex] = 0;
        data.TryQueue[clueIndex] = 0;
        context.ClueQueue[clueIndex] = 3;

        if (data.Count2[clueIndex] != 0)
        {
            JoinNodes(ref context, context.ClueQueue, context.Right, context.Join, clue);
        }
    }

    private static NodeArrays InitializeNodeArrays()
    {
        const int btreeCodes = 256;

        var bestVal = new uint[btreeCodes];
        bestVal[0] = uint.MaxValue;

        return new NodeArrays
        {
            BestJoin = new byte[btreeCodes],
            BestN = new uint[btreeCodes],
            BestVal = bestVal,
            BtSize = 0,
            BtNode = new uint[btreeCodes],
            BtLeft = new uint[btreeCodes],
            BtRight = new uint[btreeCodes],
        };
    }

    private static bool CanJoinNodes(byte[] tryQueue, int leftNode, int rightNode) =>
        tryQueue[leftNode] == 1 && tryQueue[rightNode] == 1;

    private static bool FindFreeNode(ref ProcessingData data, out int joinNode)
    {
        const int btreeCodes = 256;

        while (data.FreePtr < btreeCodes && data.FreeQueue[data.SortPtr[data.FreePtr]] == 0)
        {
            data.FreePtr++;
        }

        if (data.FreePtr < btreeCodes)
        {
            joinNode = (int)data.SortPtr[data.FreePtr];
            return true;
        }

        joinNode = -1;
        return false;
    }

    private static bool IsCostEffective(uint[] count2, int joinNode, uint saveValue)
    {
        uint cost = 3 + count2[joinNode];
        return cost < saveValue;
    }

    private static void UpdateNodeStates(
        ref BTreeEncodeContext context,
        ref ProcessingData data,
        int leftNode,
        int rightNode,
        int joinNode
    )
    {
        data.FreeQueue[joinNode] = 0;
        data.TryQueue[joinNode] = 2;
        context.ClueQueue[joinNode] = 3;

        data.FreeQueue[leftNode] = 0;
        data.TryQueue[leftNode] = 2;
        context.ClueQueue[leftNode] = 1;
        context.Right[leftNode] = (byte)rightNode;
        context.Join[leftNode] = (byte)joinNode;

        data.FreeQueue[rightNode] = 0;
        data.TryQueue[rightNode] = 2;
    }

    private static void AddToTree(
        ref NodeArrays nodeArrays,
        int leftNode,
        int rightNode,
        int joinNode
    )
    {
        nodeArrays.BtNode[nodeArrays.BtSize] = (uint)joinNode;
        nodeArrays.BtLeft[nodeArrays.BtSize] = (uint)leftNode;
        nodeArrays.BtRight[nodeArrays.BtSize] = (uint)rightNode;
        nodeArrays.BtSize++;
    }

    private static void PerformNodeJoin(
        ref BTreeEncodeContext context,
        ref ProcessingData data,
        ref NodeArrays nodeArrays,
        (int Left, int Right) nodeInfo,
        int joinNode,
        uint joinedCount,
        uint bestIndex
    )
    {
        nodeArrays.BestJoin[joinedCount] = (byte)joinNode;
        nodeArrays.BestN[joinedCount] = nodeArrays.BestN[bestIndex];

        UpdateNodeStates(ref context, ref data, nodeInfo.Left, nodeInfo.Right, joinNode);
        AddToTree(ref nodeArrays, nodeInfo.Left, nodeInfo.Right, joinNode);
    }

    private static (int Left, int Right) ExtractNodeInfo(uint bestN) =>
        ((int)((bestN >> 8) & 255), (int)(bestN & 255));

    private static uint ProcessNodeJoining(
        ref BTreeEncodeContext context,
        uint multiMax,
        ref ProcessingData data,
        ref NodeArrays nodeArrays,
        uint bestSize
    )
    {
        uint joinedCount = 1;

        for (uint i = 1; i < bestSize && joinedCount <= multiMax; i++)
        {
            var nodeInfo = ExtractNodeInfo(nodeArrays.BestN[i]);

            if (!CanJoinNodes(data.TryQueue, nodeInfo.Left, nodeInfo.Right))
            {
                continue;
            }

            if (!FindFreeNode(ref data, out int joinNode))
            {
                break;
            }

            if (!IsCostEffective(data.Count2, joinNode, nodeArrays.BestVal[i]))
            {
                continue;
            }

            PerformNodeJoin(
                ref context,
                ref data,
                ref nodeArrays,
                nodeInfo,
                joinNode,
                joinedCount,
                i
            );

            joinedCount++;
        }

        return joinedCount;
    }

    private static void RestoreClueTable(
        ref BTreeEncodeContext context,
        ref NodeArrays nodeArrays,
        uint joinedNodes
    )
    {
        for (uint i = 1; i < joinedNodes; i++)
        {
            int leftNode = (int)((nodeArrays.BestN[i] >> 8) & 255);
            int joinNode = nodeArrays.BestJoin[i];
            context.ClueQueue[leftNode] = 0;
            context.ClueQueue[joinNode] = 0;
        }
    }

    private static bool ProcessBestNodes(
        ref BTreeEncodeContext context,
        uint multiMax,
        ref ProcessingData data,
        ref NodeArrays nodeArrays,
        uint bestSize,
        ref uint remainingPasses
    )
    {
        if (bestSize <= 1)
        {
            return false;
        }

        uint joinedNodes = ProcessNodeJoining(
            ref context,
            multiMax,
            ref data,
            ref nodeArrays,
            bestSize
        );

        if (joinedNodes > 1)
        {
            // Multi-join nodes
            JoinNodes(ref context, context.ClueQueue, context.Right, context.Join, data.Clue);

            // Restore clue table
            RestoreClueTable(ref context, ref nodeArrays, joinedNodes);

            remainingPasses--;
            return remainingPasses > 0;
        }

        return false;
    }

    private static void PerformTreeBuilding(
        ref BTreeEncodeContext context,
        uint passes,
        uint multiMax,
        ref ProcessingData data,
        ref NodeArrays nodeArrays
    )
    {
        var count = new short[65536];
        uint remainingPasses = passes;

        while (remainingPasses > 0)
        {
            // Clear count array and do adjacency count
            ClearCount(data.TryQueue, count);
            AdjCount(context.BufBase, 0, context.BufEnd, count);

            // Find most common nodes
            uint bestSize = FindBest(
                count,
                data.TryQueue,
                nodeArrays.BestN,
                nodeArrays.BestVal,
                data.Ratio
            );

            if (
                !ProcessBestNodes(
                    ref context,
                    multiMax,
                    ref data,
                    ref nodeArrays,
                    bestSize,
                    ref remainingPasses
                )
            )
            {
                break;
            }
        }
    }

    private static void WriteCompressedOutput(
        ref BTreeEncodeContext context,
        uint clue,
        ref NodeArrays nodeArrays
    )
    {
        // Write header
        WriteBits(ref context, clue, 8);
        WriteBits(ref context, nodeArrays.BtSize, 8);

        for (uint i = 0; i < nodeArrays.BtSize; i++)
        {
            WriteBits(ref context, nodeArrays.BtNode[i], 8);
            WriteBits(ref context, nodeArrays.BtLeft[i], 8);
            WriteBits(ref context, nodeArrays.BtRight[i], 8);
        }

        // Write packed file
        for (int i = 0; i < context.BufEnd; i++)
        {
            WriteBits(ref context, context.BufBase[i], 8);
        }

        WriteBits(ref context, clue, 8);
        WriteBits(ref context, 0, 8);
        WriteBits(ref context, 0, 7); // Flush bits
    }

    private static uint[] InitializeFileCount(ref BTreeEncodeContext context)
    {
        const int btreeCodes = 256;
        var count2 = new uint[btreeCodes];

        for (int i = 0; i < context.SourceLength; i++)
        {
            uint val = context.Buffer[i];
            count2[val]++;
        }

        return count2;
    }

    private static ProcessingData InitializeProcessingData(
        ref BTreeEncodeContext context,
        uint quick,
        int zeroSuppress
    )
    {
        var data = new ProcessingData
        {
            Count2 = InitializeFileCount(ref context),
            Ratio = quick > 0 ? (int)quick : 2,
        };

        InitializeQueuesAndSuppression(
            data.Count2,
            zeroSuppress,
            out data.TryQueue,
            out data.FreeQueue
        );

        data.SortPtr = CreateSortedCodeArray(data.Count2);
        InitializeClueNode(ref context, ref data, out data.Clue, out data.FreePtr);

        return data;
    }

    private static void TreePack(
        ref BTreeEncodeContext context,
        uint passes,
        uint multiMax,
        uint quick,
        int zeroSuppress
    )
    {
        InitializeBuffersAndArrays(ref context);
        var processingData = InitializeProcessingData(ref context, quick, zeroSuppress);
        var nodeArrays = InitializeNodeArrays();

        PerformTreeBuilding(ref context, passes, multiMax, ref processingData, ref nodeArrays);
        WriteCompressedOutput(ref context, processingData.Clue, ref nodeArrays);
    }

    private List<byte> CompressFile(ref BTreeEncodeContext context)
    {
        context.PackBits = 0;
        context.WorkPattern = 0;
        context.PLen = 0;

        // Write header
        WriteBits(ref context, 0x46FB, 16);
        WriteBits(ref context, (uint)context.SourceLength, 24);

        // Tree pack
        TreePack(ref context, 256, 32, 0, ZeroSuppress ? 1 : 0);

        return context.Destination;
    }

    #endregion

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
