namespace Osm.Sage.Compression.Eac.Codex;

/// <summary>
///
/// </summary>
public partial class HuffmanWithRunlengthCodex
{
    private const int BigNum = 32000;
    private const int TreeSize = 520;
    private const int HuffCodes = 256;
    private const int MaxBits = 16;
    private const int RepTbl = 252;

    private struct MemStruct()
    {
        public List<byte> Buffer { get; } = [];
        public int Length { get; set; }
    }

    private struct EncodingContext()
    {
        public byte[] QLeapCode { get; } = new byte[HuffCodes];
        public uint[] Count { get; } = new uint[768];
        public uint[] BitNum { get; } = new uint[MaxBits + 1];
        public uint[] RepBits { get; } = new uint[RepTbl];
        public uint[] RepBase { get; } = new uint[RepTbl];
        public uint[] TreeLeft { get; } = new uint[TreeSize];
        public uint[] TreeRight { get; } = new uint[TreeSize];
        public uint[] BitsArray { get; } = new uint[HuffCodes];
        public uint[] PatternArray { get; } = new uint[HuffCodes];
        public uint[] Masks { get; } = new uint[17];
        public uint PackBits { get; set; }
        public uint WorkPattern { get; set; }
        public byte[]? Buffer { get; set; }
        public int FLength { get; set; }
        public uint MostBits { get; set; }
        public uint Codes { get; set; }
        public uint Clue { get; set; }
        public uint DClue { get; set; }
        public uint Clues { get; set; }
        public uint DClues { get; set; }
        public int MinDelta { get; set; }
        public int MaxDelta { get; set; }
        public uint PLength { get; set; }
        public uint ULength { get; set; }
        public uint[] SortPtr { get; } = new uint[HuffCodes];
    }

    private readonly struct ListState(uint[] count, uint[] ptr)
    {
        public uint[] Count { get; } = count;
        public uint[] Ptr { get; } = ptr;
    }

    private readonly struct MergeSelection(uint ptr1, uint val1, uint ptr2, uint val2)
    {
        public uint Ptr1 { get; } = ptr1;
        public uint Val1 { get; } = val1;
        public uint Ptr2 { get; } = ptr2;
        public uint Val2 { get; } = val2;
    }

    private static void DeltaBytes(ReadOnlySpan<byte> source, Span<byte> dest, int length)
    {
        byte c = 0;
        var s = 0;
        while (s < length)
        {
            var c1 = source[s++];
            dest[s - 1] = (byte)(c1 - c);
            c = c1;
        }
    }

    private static byte[] DeltaOnce(byte[] src)
    {
        var dst = new byte[src.Length];
        DeltaBytes(src, dst, src.Length);
        return dst;
    }

    private static void EmitBitsChunk(
        ref EncodingContext context,
        ref MemStruct dest,
        uint bits,
        uint length
    )
    {
        context.PackBits += length;
        var mask = context.Masks[length];
        context.WorkPattern += ((bits & mask) << (int)(24 - context.PackBits));

        while (context.PackBits > 7)
        {
            var outByte = (byte)((context.WorkPattern >> 16) & 0xFF);
            if (dest.Length == dest.Buffer.Count)
            {
                dest.Buffer.Add(outByte);
            }
            else
            {
                dest.Buffer[dest.Length] = outByte;
            }

            dest.Length++;

            context.WorkPattern <<= 8;
            context.PackBits -= 8;
            context.PLength++;
        }
    }

    private static void WriteBits(
        ref EncodingContext context,
        ref MemStruct dest,
        uint bitPattern,
        uint length
    )
    {
        if (length > 16)
        {
            var prefix = (int)(length % 16);
            if (prefix != 0)
            {
                var highBits = bitPattern >> (int)(length - prefix);
                EmitBitsChunk(ref context, ref dest, highBits, (uint)prefix);
            }

            var chunks = (int)((length - (uint)prefix) / 16);
            for (var k = chunks; k > 0; --k)
            {
                var segment = (bitPattern >> ((k - 1) * 16)) & 0xFFFF;
                EmitBitsChunk(ref context, ref dest, segment, 16);
            }
        }
        else
        {
            EmitBitsChunk(ref context, ref dest, bitPattern, length);
        }
    }

    private static void TreeChase(ref EncodingContext context, uint node, uint bits)
    {
        if (node < HuffCodes)
        {
            context.BitsArray[node] = bits;
        }
        else
        {
            // Use the stack to prevent recursion
            var stack = new Stack<(uint Node, uint Bits)>();
            stack.Push((node, bits));
            while (stack.Count > 0)
            {
                (uint n, uint b) = stack.Pop();
                if (n < HuffCodes)
                {
                    context.BitsArray[n] = b;
                    continue;
                }

                // Push right first, then left so left is processed first (matching original order)
                stack.Push((context.TreeRight[n], b + 1));
                stack.Push((context.TreeLeft[n], b + 1));
            }
        }
    }

    private static uint PrepareInitialLists(
        ref EncodingContext context,
        uint[] listCount,
        uint[] listPtr
    )
    {
        var i1 = 0U;
        listCount[i1++] = 0;
        for (var i = 0U; i < HuffCodes; ++i)
        {
            context.BitsArray[i] = 99;
            if (context.Count[i] == 0)
            {
                continue;
            }

            listCount[i1] = context.Count[i];
            listPtr[i1++] = i;
        }

        context.Codes = i1 - 1;
        return i1;
    }

    private static void SelectTwoSmallest(
        uint[] listCount,
        uint i1,
        out uint ptr1,
        out uint val1,
        out uint ptr2,
        out uint val2
    )
    {
        var i = i1;
        val1 = listCount[--i];
        ptr1 = i;
        val2 = listCount[--i];
        ptr2 = i;

        if (val1 < val2)
        {
            val2 = val1;
            ptr2 = ptr1;
            val1 = listCount[i];
            ptr1 = i;
        }

        while (i != 0)
        {
            --i;
            while (listCount[i] > val1)
            {
                --i;
            }

            if (i == 0)
            {
                continue;
            }

            val1 = listCount[i];
            ptr1 = i;
            if (val1 > val2)
            {
                continue;
            }

            val1 = val2;
            ptr1 = ptr2;
            val2 = listCount[i];
            ptr2 = i;
        }
    }

    private static void MergeSelectedNodes(
        ref EncodingContext context,
        ref uint nodes,
        ref uint i1,
        in ListState list,
        in MergeSelection sel
    )
    {
        context.TreeLeft[nodes] = list.Ptr[sel.Ptr1];
        context.TreeRight[nodes] = list.Ptr[sel.Ptr2];
        list.Count[sel.Ptr1] = sel.Val1 + sel.Val2;
        list.Ptr[sel.Ptr1] = nodes;
        list.Count[sel.Ptr2] = list.Count[--i1];
        list.Ptr[sel.Ptr2] = list.Ptr[i1];
        ++nodes;
    }

    private static void MakeTree(ref EncodingContext context)
    {
        uint nodes = HuffCodes;
        var listCount = new uint[HuffCodes + 2];
        var listPtr = new uint[HuffCodes + 2];
        var list = new ListState(listCount, listPtr);

        var i1 = PrepareInitialLists(ref context, listCount, listPtr);

        if (i1 > 2)
        {
            while (i1 > 2)
            {
                SelectTwoSmallest(
                    listCount,
                    i1,
                    out var ptr1,
                    out var val1,
                    out var ptr2,
                    out var val2
                );

                var sel = new MergeSelection(ptr1, val1, ptr2, val2);
                MergeSelectedNodes(ref context, ref nodes, ref i1, in list, in sel);
            }

            TreeChase(ref context, nodes - 1, 0);
        }
        else
        {
            TreeChase(ref context, listPtr[context.Codes], 1);
        }
    }

    private static void InitBaseLayer(
        ref EncodingContext context,
        int[] dpPrev,
        int actualRemaining
    )
    {
        // Base layer: k = 0
        for (int rem = 0; rem <= actualRemaining; rem++)
        {
            if (rem == 0)
            {
                dpPrev[rem] = 0;
            }
            else if ((uint)rem < RepTbl)
            {
                dpPrev[rem] =
                    (int)context.BitsArray[context.Clue] + 3 + (int)context.RepBits[rem] * 2;
            }
            else
            {
                dpPrev[rem] = 20;
            }
        }
    }

    private static void CopyLayer(int[] src, int[] dst, int length) => Array.Copy(src, dst, length);

    private static void ComputeLayer(
        ref EncodingContext context,
        int k,
        int[] prev,
        int[] curr,
        int actualRemaining
    )
    {
        var costK = (int)context.BitsArray[context.Clue + (uint)k];

        for (int rem = 0; rem <= actualRemaining; rem++)
        {
            var best = prev[rem];

            var use = rem / k;
            if (use > 0)
            {
                int newRemaining = rem - use * k;
                int withK = prev[newRemaining] + costK * use;
                best = Math.Min(best, withK);
            }

            curr[rem] = best;
        }
    }

    private static void Swap<T>(ref T a, ref T b) => (a, b) = (b, a);

    private static int MinRep(ref EncodingContext context, uint remaining, uint r)
    {
        int actualR = (int)r;
        int actualRemaining = (int)remaining;

        // Use heap arrays to keep this method simple (lower cognitive complexity)
        var dpPrev = new int[actualRemaining + 1];
        var dpCurr = new int[actualRemaining + 1];

        InitBaseLayer(ref context, dpPrev, actualRemaining);

        for (int k = 1; k <= actualR; k++)
        {
            bool sizeAvailable = context.Count[context.Clue + (uint)k] != 0;

            if (!sizeAvailable)
            {
                CopyLayer(dpPrev, dpCurr, actualRemaining + 1);
            }
            else
            {
                ComputeLayer(ref context, k, dpPrev, dpCurr, actualRemaining);
            }

            Swap(ref dpPrev, ref dpCurr);
        }

        return dpPrev[actualRemaining];
    }

    private static void WriteNum(ref EncodingContext context, ref MemStruct dest, uint num)
    {
        uint dpHuf;
        uint dBase;
        switch (num)
        {
            case < RepTbl:
                dpHuf = context.RepBits[num];
                dBase = context.RepBase[num];
                break;
            case < 508U:
                dpHuf = 6;
                dBase = 252;
                break;
            case < 1020U:
                dpHuf = 7;
                dBase = 508;
                break;
            case < 2044U:
                dpHuf = 8;
                dBase = 1020;
                break;
            case < 4092U:
                dpHuf = 9;
                dBase = 2044;
                break;
            case < 8188U:
                dpHuf = 10;
                dBase = 4092;
                break;
            case < 16380U:
                dpHuf = 11;
                dBase = 8188;
                break;
            case < 32764U:
                dpHuf = 12;
                dBase = 16380;
                break;
            case < 65532U:
                dpHuf = 13;
                dBase = 32764;
                break;
            case < 131068U:
                dpHuf = 14;
                dBase = 65532;
                break;
            case < 262140U:
                dpHuf = 15;
                dBase = 131068;
                break;
            case < 524288U:
                dpHuf = 16;
                dBase = 262140;
                break;
            case < 1048576U:
                dpHuf = 17;
                dBase = 524288;
                break;
            default:
                dpHuf = 18;
                dBase = 1048576;
                break;
        }

        WriteBits(ref context, ref dest, 0x00000001, dpHuf + 1);
        WriteBits(ref context, ref dest, num - dBase, dpHuf + 2);
    }

    private static void WriteExp(ref EncodingContext context, ref MemStruct dest, uint code)
    {
        WriteBits(
            ref context,
            ref dest,
            context.PatternArray[context.Clue],
            context.BitsArray[context.Clue]
        );

        WriteNum(ref context, ref dest, 0);
        WriteBits(ref context, ref dest, code, 9);
    }

    private static void WriteCode(ref EncodingContext context, ref MemStruct dest, uint code)
    {
        if (code == context.Clue)
        {
            WriteExp(ref context, ref dest, code);
        }
        else
        {
            WriteBits(ref context, ref dest, context.PatternArray[code], context.BitsArray[code]);
        }
    }

    private static void Init(ref EncodingContext context)
    {
        uint i = 0;
        while (i < 4)
        {
            context.RepBits[i] = 0;
            context.RepBase[i++] = 0;
        }

        while (i < 12)
        {
            context.RepBits[i] = 1;
            context.RepBase[i++] = 4;
        }

        while (i < 28)
        {
            context.RepBits[i] = 2;
            context.RepBase[i++] = 12;
        }

        while (i < 60)
        {
            context.RepBits[i] = 3;
            context.RepBase[i++] = 28;
        }

        while (i < 124)
        {
            context.RepBits[i] = 4;
            context.RepBase[i++] = 60;
        }

        while (i < 252)
        {
            context.RepBits[i] = 5;
            context.RepBase[i++] = 124;
        }
    }

    private static void Pass1ComputeCounts(ref EncodingContext context, byte[] b)
    {
        var bPtr1 = 0;
        var i1 = 256U;
        while (bPtr1 < b.Length)
        {
            uint i = b[bPtr1++];
            if (i == i1)
            {
                var rep = 0U;
                var bPtr2 = bPtr1 + 30000;
                if (bPtr2 > b.Length)
                {
                    bPtr2 = b.Length;
                }

                var ii = i;
                while (ii == i1 && bPtr1 < bPtr2)
                {
                    ++rep;
                    ii = b[bPtr1++];
                }

                ++context.Count[rep < 255 ? 512 + rep : 512];
                i = ii;
            }

            ++context.Count[i];
            ++context.Count[((i + 256 - i1) & 255) + 256];
            i1 = i;
        }

        if (context.Count[512] == 0)
        {
            ++context.Count[512];
        }
    }

    private static uint FindClueBytes(ref EncodingContext context)
    {
        context.Clues = 0;
        context.DClues = 0;
        var bestForced = 0U;

        uint i = 0U;
        while (i < HuffCodes)
        {
            var start = i;
            var zc = 0U;
            if (context.Count[i] < context.Count[bestForced])
            {
                bestForced = i;
            }

            uint j = i;
            while (j < 256 && context.Count[j] == 0)
            {
                ++zc;
                ++j;
            }

            if (zc >= context.DClues)
            {
                context.DClue = start;
                context.DClues = zc;
                if (context.DClues >= context.Clues)
                {
                    context.DClue = context.Clue;
                    context.DClues = context.Clues;
                    context.Clue = start;
                    context.Clues = zc;
                }
            }

            i = j + 1;
        }

        return bestForced;
    }

    private static void ApplyClueOptions(ref EncodingContext context, uint opt, uint bestForced)
    {
        // Force a clue byte
        if ((opt & 32) != 0 && context.Clues == 0)
        {
            context.Clues = 1;
            context.Clue = bestForced;
        }

        // Disable and split clue bytes
        if (((~opt) & 2) != 0)
        {
            if (context.Clues > 1)
            {
                context.Clues = 1;
            }

            if (((~opt) & 1) != 0)
            {
                context.Clues = 0;
            }
        }

        if (((~opt) & 4) != 0)
        {
            context.DClues = 0;
        }
        else
        {
            if (context.DClues > 10)
            {
                var tClue = context.Clue;
                var tClues = context.Clues;
                context.Clue = context.DClue;
                context.Clues = context.DClues;
                context.DClue = tClue;
                context.DClues = tClues;
            }

            if ((context.Clues * 4) >= context.DClues)
            {
                return;
            }

            context.Clues = context.DClues / 4;
            context.DClues = context.DClues - context.Clues;
            context.Clue = context.DClue + context.DClues;
        }
    }

    private static uint SetupDeltaClues(ref EncodingContext context)
    {
        var threshold = 0U;
        if (context.DClues == 0)
        {
            return threshold;
        }

        context.MinDelta = -(int)(context.DClues / 2);
        context.MaxDelta = (int)context.DClues + context.MinDelta;
        threshold = context.ULength / 25;

        for (var i = 1U; i <= (uint)context.MaxDelta; ++i)
        {
            if (context.Count[256 + i] > threshold)
            {
                context.Count[context.DClue + (i - 1) * 2] = context.Count[256 + i];
            }
        }

        for (var i = 1U; i <= (uint)(-context.MinDelta); ++i)
        {
            if (context.Count[512 - i] > threshold)
            {
                context.Count[context.DClue + (i - 1) * 2 + 1] = context.Count[512 - i];
            }
        }

        // Adjust delta clues
        var i2 = 0U;
        for (var i = 0U; i < context.DClues; ++i)
        {
            if (context.Count[context.DClue + i] != 0)
            {
                i2 = i;
            }
        }

        var di = (int)context.DClues - (int)i2 - 1;
        context.DClues = (uint)((int)context.DClues - di);
        if (context.Clue == (context.DClue + context.DClues + (uint)di))
        {
            context.Clue = (uint)((int)context.Clue - di);
            context.Clues = (uint)((int)context.Clues + di);
        }

        context.MinDelta = -(int)(context.DClues / 2);
        context.MaxDelta = (int)context.DClues + context.MinDelta;

        return threshold;
    }

    private static void CopyRepClueBytes(ref EncodingContext context)
    {
        if (context.Clues == 0)
        {
            return;
        }

        for (var i = 0U; i < context.Clues; ++i)
        {
            context.Count[context.Clue + i] = context.Count[512 + i];
        }
    }

    private static void RemoveImpliedRepClues(ref EncodingContext context)
    {
        if (context.Clues <= 1)
        {
            return;
        }

        for (var i = 1U; i < context.Clues; ++i)
        {
            var i1U = i - 1;
            if (i1U > 8)
            {
                i1U = 8;
            }

            if (context.Count[context.Clue + i] == 0)
            {
                continue;
            }

            var minR = MinRep(ref context, i, i1U);
            if (
                (minR <= context.BitsArray[context.Clue + i])
                || (
                    context.Count[context.Clue + i]
                        * (uint)(minR - context.BitsArray[context.Clue + i])
                    < (i / 2)
                )
            )
            {
                context.Clue = i;
                context.Count[context.Clue] = 0;
            }
        }
    }

    private static uint[] PrepareSecondPass(ref EncodingContext context)
    {
        var count2 = new uint[HuffCodes];
        for (var i = 0; i < HuffCodes; ++i)
        {
            count2[i] = context.Count[i];
            context.Count[i] = 0;
            context.Count[256 + i] = 0;
            context.Count[512 + i] = 0;
        }

        return count2;
    }

    private static (uint nCode, uint repN, uint iRep) ComputeRunCosts(
        ref EncodingContext context,
        uint previous,
        uint runLength,
        uint[] count2
    )
    {
        // Cost of just writing the repeated symbol 'previous' runLength times
        var nCode = runLength * context.BitsArray[previous];

        // Cost of using the clue (RepTbl); default is "very large" to skip
        uint repN = BigNum;

        // Cost of using extended reps (clue + k); default is "very large" to skip
        uint iRep = BigNum;

        if (context.Clues == 0)
        {
            return (nCode, repN, iRep);
        }

        // Using the main clue (rep count encoding)
        if (count2[context.Clue] != 0)
        {
            repN = 20;
            if (runLength < RepTbl)
            {
                repN = context.BitsArray[context.Clue] + 3 + context.RepBits[runLength] * 2;
            }
        }

        // Using composed reps (clue + k) pieces
        if (context.Clues <= 1)
        {
            return (nCode, repN, iRep);
        }

        var remaining = runLength;
        iRep = 0;
        var k = context.Clues - 1;
        while (k != 0)
        {
            if (count2[context.Clue + k] != 0)
            {
                var use = remaining / k;
                iRep += use * context.BitsArray[context.Clue + k];
                remaining -= use * k;
            }

            --k;
        }

        if (remaining != 0)
        {
            iRep = BigNum;
        }

        return (nCode, repN, iRep);
    }

    private static void DistributeRunToClues(
        ref EncodingContext context,
        uint runLength,
        uint[] count2
    )
    {
        var remaining = runLength;
        var k = context.Clues - 1;

        while (k != 0)
        {
            if (count2[context.Clue + k] != 0)
            {
                var use = remaining / k;
                if (use != 0)
                {
                    context.Count[context.Clue + k] += use;
                    remaining -= use * k;
                }
            }

            --k;
        }
    }

    private static bool TryGetDeltaIndex(
        ref EncodingContext context,
        uint previous,
        uint current,
        uint[] count2,
        uint threshold,
        out uint deltaIndex
    )
    {
        deltaIndex = 0;

        if (context.DClues == 0)
        {
            return false;
        }

        var di = (int)current - (int)previous;
        if (di > context.MaxDelta || di < context.MinDelta)
        {
            return false;
        }

        var idx = (current - previous - 1) * 2 + context.DClue;
        if (current < previous)
        {
            idx = (previous - current - 1) * 2 + context.DClue + 1;
        }

        // Only consider deltas that were significant in pass 1
        if (count2[idx] <= threshold)
        {
            return false;
        }

        // Any of these heuristics pushes us to choose delta
        bool choose =
            (count2[current] < 4)
            || (context.BitsArray[idx] < context.BitsArray[current])
            || (
                context.BitsArray[idx] == context.BitsArray[current]
                && context.Count[idx] > context.Count[current]
            );

        if (!choose)
        {
            return false;
        }

        deltaIndex = idx;
        return true;
    }

    private static void UpdateCountsForRun(
        ref EncodingContext context,
        uint previous,
        uint runLen,
        uint[] count2,
        uint nCode,
        uint repN,
        uint iRep
    )
    {
        if (nCode <= repN && nCode <= iRep)
        {
            context.Count[previous] += runLen;
        }
        else if (repN < iRep)
        {
            ++context.Count[context.Clue];
        }
        else
        {
            DistributeRunToClues(ref context, runLen, count2);
        }
    }

    private static void IncrementDeltaOrLiteralCount(
        ref EncodingContext context,
        uint previous,
        uint current,
        uint[] count2,
        uint threshold
    )
    {
        if (
            TryGetDeltaIndex(ref context, previous, current, count2, threshold, out uint deltaIndex)
        )
        {
            ++context.Count[deltaIndex];
        }
        else
        {
            ++context.Count[current];
        }
    }

    private static void SecondPassAccumulate(
        ref EncodingContext context,
        byte[] b,
        uint[] count2,
        uint threshold
    )
    {
        var bPtr1 = 0;
        var i1 = 256U;

        while (bPtr1 < b.Length)
        {
            uint cur = b[bPtr1++];
            if (cur == i1)
            {
                // Scan run of repeated previous byte (i1)
                var runLen = ScanRun(b, ref bPtr1, i1);

                // Decide how to account for this run in counts
                (uint nCode, uint repN, uint iRep) = ComputeRunCosts(
                    ref context,
                    i1,
                    runLen,
                    count2
                );

                UpdateCountsForRun(ref context, i1, runLen, count2, nCode, repN, iRep);

                cur = b[bPtr1 - 1];
            }

            // Choose delta vs literal
            IncrementDeltaOrLiteralCount(ref context, i1, cur, count2, threshold);

            i1 = cur;
        }
    }

    private static void FindMaxBitsAndTopTwo(
        ref EncodingContext context,
        out uint maxBits,
        out uint i2Ptr,
        out uint i3Ptr
    )
    {
        maxBits = 0;
        i2Ptr = 0;
        i3Ptr = 0;

        for (var i = 0U; i < HuffCodes; ++i)
        {
            if (context.Count[i] == 0 || context.BitsArray[i] < maxBits)
            {
                continue;
            }

            i3Ptr = i2Ptr;
            i2Ptr = i;
            maxBits = context.BitsArray[i];
        }
    }

    private static bool TryFindFirstBelowChainsaw(
        ref EncodingContext context,
        uint chainsaw,
        out uint p
    )
    {
        p = 0U;
        while (p < HuffCodes)
        {
            if (context.Count[p] != 0 && context.BitsArray[p] < chainsaw)
            {
                return true;
            }

            ++p;
        }

        return false;
    }

    private static uint FindBestCandidateBelowChainsaw(
        ref EncodingContext context,
        uint chainsaw,
        uint p
    )
    {
        for (var i = p; i < HuffCodes; ++i)
        {
            if (
                context.Count[i] != 0
                && context.BitsArray[i] < chainsaw
                && context.BitsArray[i] > context.BitsArray[p]
            )
            {
                p = i;
            }
        }

        return p;
    }

    private static void AdjustBitLengths(
        ref EncodingContext context,
        uint p,
        uint i2Ptr,
        uint i3Ptr
    )
    {
        var newLen = context.BitsArray[p] + 1;
        context.BitsArray[p] = newLen;
        context.BitsArray[i2Ptr] = newLen;
        context.BitsArray[i3Ptr] = context.BitsArray[i3Ptr] - 1;
    }

    private static void ClipChainSaw(ref EncodingContext context, uint chainsaw)
    {
        var maxBits = 99U;
        while (maxBits > chainsaw)
        {
            FindMaxBitsAndTopTwo(ref context, out maxBits, out var i2Ptr, out var i3Ptr);
            if (maxBits <= chainsaw)
            {
                break;
            }

            if (!TryFindFirstBelowChainsaw(ref context, chainsaw, out var p))
            {
                break;
            }

            p = FindBestCandidateBelowChainsaw(ref context, chainsaw, p);
            AdjustBitLengths(ref context, p, i2Ptr, i3Ptr);

            maxBits = 99;
        }
    }

    private static void ApplyHuffmanInhibit(ref EncodingContext context, uint opt)
    {
        if (((~opt) & 8) == 0)
        {
            return;
        }

        for (var i = 0; i < HuffCodes; ++i)
        {
            context.BitsArray[i] = 8;
        }
    }

    private static void BuildBitNums(ref EncodingContext context)
    {
        for (var i = 0; i < MaxBits; ++i)
        {
            context.BitNum[i] = 0;
        }

        for (var i = 0; i < HuffCodes; ++i)
        {
            if (context.BitsArray[i] <= MaxBits)
            {
                ++context.BitNum[context.BitsArray[i]];
            }
        }
    }

    private static void SortCodes(ref EncodingContext context)
    {
        var idX = 0U;
        var most = 0U;

        for (var bits = 1U; bits <= MaxBits; ++bits)
        {
            if (context.BitNum[bits] == 0)
            {
                continue;
            }

            for (var code = 0U; code < HuffCodes; ++code)
            {
                if (context.BitsArray[code] == bits)
                {
                    context.SortPtr[idX++] = code;
                }
            }

            most = bits;
        }

        context.MostBits = most;
        context.Codes = idX;
    }

    private static void AssignBitPatterns(ref EncodingContext context)
    {
        var pattern = 0U;
        var curBits = 0U;
        for (var i = 0U; i < context.Codes; ++i)
        {
            var code = context.SortPtr[i];
            while (curBits < context.BitsArray[code])
            {
                ++curBits;
                pattern <<= 1;
            }

            context.PatternArray[code] = pattern;
            ++pattern;
        }
    }

    private static void Analysis(ref EncodingContext context, uint opt, uint chainsaw)
    {
        Array.Clear(context.Count, 0, 768);
        var b = context.Buffer ?? [];

        // Pass 1: compute counts
        Pass1ComputeCounts(ref context, b);

        // Determine clue bytes based on the first pass counts
        var bestForced = FindClueBytes(ref context);

        // Apply options affecting clues and delta clues
        ApplyClueOptions(ref context, opt, bestForced);

        // Copy delta clues and adjust deltas, return threshold
        var threshold = SetupDeltaClues(ref context);

        // Copy rep clue bytes into their final locations
        CopyRepClueBytes(ref context);

        // First approximation tree
        MakeTree(ref context);

        // Remove implied rep clues (can reduce clues > 1)
        RemoveImpliedRepClues(ref context);

        // Prepare for pass 2 by snapshotting counts and clearing work arrays
        var count2 = PrepareSecondPass(ref context);

        // Pass 2: reassess counts using decisions based on tree and clues
        SecondPassAccumulate(ref context, b, count2, threshold);

        // Force a clue byte if required by opt flags
        if ((opt & 32) != 0)
        {
            ++context.Count[context.Clue];
        }

        // Second approximation tree
        MakeTree(ref context);

        // Chainsaw clip + inhibit + build final tables
        ClipChainSaw(ref context, chainsaw);
        ApplyHuffmanInhibit(ref context, opt);
        BuildBitNums(ref context);
        SortCodes(ref context);
        AssignBitPatterns(ref context);
    }

    private static void ResetQLeap(ref EncodingContext context)
    {
        for (var i = 0; i < HuffCodes; ++i)
        {
            context.QLeapCode[i] = 0;
        }
    }

    private static void EmitQLeapTable(ref EncodingContext context, ref MemStruct dest)
    {
        var i2 = 255U;
        var idX = 0U;
        while (idX < context.Codes)
        {
            var i1Code = context.SortPtr[idX];
            var di = -1;
            do
            {
                i2 = (i2 + 1) & 255;
                if (context.QLeapCode[i2] == 0)
                {
                    ++di;
                }
            } while (i1Code != i2);

            context.QLeapCode[i2] = 1;
            WriteNum(ref context, ref dest, (uint)di);
            ++idX;
        }
    }

    private static void WriteHeaderAndTables(ref EncodingContext context, ref MemStruct dest)
    {
        WriteBits(ref context, ref dest, context.Clue, 8);
        for (var i = 1U; i <= context.MostBits; ++i)
        {
            WriteNum(ref context, ref dest, context.BitNum[i]);
        }

        ResetQLeap(ref context);
        EmitQLeapTable(ref context, ref dest);
    }

    private static uint ScanRun(byte[] buffer, ref int index, uint value)
    {
        uint run = 0;
        var limit = index + 30000;
        if (limit > buffer.Length)
        {
            limit = buffer.Length;
        }

        var lookahead = value;
        while (index < limit && lookahead == value)
        {
            lookahead = buffer[index++];
            ++run;
        }

        // After loop, index moved one past last equal byte; keep last read for caller
        return run;
    }

    private static void WriteLiteralRun(
        ref EncodingContext context,
        ref MemStruct dest,
        uint symbol,
        uint count
    )
    {
        while (count-- != 0)
        {
            WriteCode(ref context, ref dest, symbol);
        }
    }

    private static void WriteRepCount(ref EncodingContext context, ref MemStruct dest, uint runLen)
    {
        const uint rlAdjust = 0;
        WriteBits(
            ref context,
            ref dest,
            context.PatternArray[context.Clue],
            context.BitsArray[context.Clue]
        );

        WriteNum(ref context, ref dest, runLen - rlAdjust);
    }

    private static void WriteIRepPieces(
        ref EncodingContext context,
        ref MemStruct dest,
        uint runLen
    )
    {
        var remaining = runLen;
        var k = context.Clues - 1;
        while (k != 0)
        {
            if (context.Count[context.Clue + k] != 0)
            {
                var use = remaining / k;
                remaining -= use * k;
                while (use-- != 0)
                {
                    WriteCode(ref context, ref dest, context.Clue + k);
                }
            }

            --k;
        }

        // If remaining != 0, caller's decision logic ensured this path wasn't chosen
    }

    private static bool TryWriteDelta(
        ref EncodingContext context,
        ref MemStruct dest,
        uint prev,
        uint cur
    )
    {
        if (context.DClues == 0)
        {
            return false;
        }

        var di = (int)cur - (int)prev;
        if (di > context.MaxDelta || di < context.MinDelta)
        {
            return false;
        }

        var diIndex = (cur - prev - 1) * 2 + context.DClue;
        if (cur < prev)
        {
            diIndex = (prev - cur - 1) * 2 + context.DClue + 1;
        }

        if (context.BitsArray[diIndex] >= context.BitsArray[cur])
        {
            return false;
        }

        WriteBits(ref context, ref dest, context.PatternArray[diIndex], context.BitsArray[diIndex]);
        return true;
    }

    private static void WriteEofMarker(ref EncodingContext context, ref MemStruct dest)
    {
        WriteBits(
            ref context,
            ref dest,
            context.PatternArray[context.Clue],
            context.BitsArray[context.Clue]
        );

        WriteNum(ref context, ref dest, 0);
        WriteBits(ref context, ref dest, 2, 2);
    }

    private static void FlushBits(ref EncodingContext context, ref MemStruct dest) =>
        WriteBits(ref context, ref dest, 0, 7);

    private static void Pack(ref EncodingContext context, ref MemStruct dest)
    {
        var b = context.Buffer ?? [];
        var bPtr1 = 0;

        WriteHeaderAndTables(ref context, ref dest);

        if (context.Clues == 0)
        {
            context.Clue = BigNum;
        }

        // Write packed file
        var prev = 256U;
        while (bPtr1 < b.Length)
        {
            uint cur = b[bPtr1++];
            if (cur == prev)
            {
                var runLen = ScanRun(b, ref bPtr1, prev);
                (uint nCode, uint repN, uint iRep) = ComputeRunCosts(
                    ref context,
                    prev,
                    runLen,
                    context.Count
                );

                if (nCode <= repN && nCode <= iRep)
                {
                    WriteLiteralRun(ref context, ref dest, prev, runLen);
                }
                else if (repN < iRep)
                {
                    WriteRepCount(ref context, ref dest, runLen);
                }
                else
                {
                    WriteIRepPieces(ref context, ref dest, runLen);
                }

                cur = b[bPtr1 - 1];
            }

            bool usedDelta = TryWriteDelta(ref context, ref dest, prev, cur);
            prev = cur;

            if (!usedDelta)
            {
                WriteCode(ref context, ref dest, cur);
            }
        }

        WriteEofMarker(ref context, ref dest);
        FlushBits(ref context, ref dest);
    }

    private static void InitializeMasks(ref EncodingContext context)
    {
        context.Masks[0] = 0;
        for (var i = 1; i < 17; ++i)
        {
            context.Masks[i] = (context.Masks[i - 1] << 1) + 1;
        }
    }

    private static void ResetOutputState(ref EncodingContext context, ref MemStruct outFile)
    {
        outFile.Length = 0;
        context.PackBits = 0;
        context.WorkPattern = 0;
        context.PLength = 0;
    }

    private static uint ResolveUpType(bool is32Bit, bool sameLength, int deltaed) =>
        (is32Bit, sameLength, deltaed) switch
        {
            (true, true, 1) => 0xB2FBU,
            (true, true, 2) => 0xB4FBU,
            (true, true, _) => 0xB0FBU,
            (true, false, 1) => 0xB3FBU,
            (true, false, 2) => 0xB5FBU,
            (true, false, _) => 0xB1FBU,
            (false, true, 1) => 0x32FBU,
            (false, true, 2) => 0x34FBU,
            (false, true, _) => 0x30FBU,
            (false, false, 1) => 0x33FBU,
            (false, false, 2) => 0x35FBU,
            (false, false, _) => 0x31FBU,
        };

    private static void WriteLength(
        ref EncodingContext context,
        ref MemStruct outFile,
        uint value,
        bool is32Bit
    ) => WriteBits(ref context, ref outFile, value, is32Bit ? 32U : 24U);

    private static void WriteStandardHeader(
        ref EncodingContext context,
        ref MemStruct outFile,
        int uLength,
        int deltaed
    )
    {
        bool is32Bit = (uint)uLength > 0xFFFFFF;
        bool sameLen = uLength == context.FLength;

        var upType = ResolveUpType(is32Bit, sameLen, deltaed);
        WriteBits(ref context, ref outFile, upType, 16);

        if (!sameLen)
        {
            WriteLength(ref context, ref outFile, (uint)uLength, is32Bit);
        }

        WriteLength(ref context, ref outFile, (uint)context.FLength, is32Bit);
    }

    private static void PackFile(
        ref EncodingContext context,
        byte[] inFileBytes,
        ref MemStruct outFile,
        int uLength,
        int deltaed
    )
    {
        // Set defaults
        context.PackBits = 0;
        context.WorkPattern = 0;
        const uint chainsaw = 15U;

        InitializeMasks(ref context);

        // Initialize Huffman vars
        Init(ref context);

        // Source buffer
        context.Buffer = inFileBytes;
        context.FLength = inFileBytes.Length;
        context.ULength = (uint)context.FLength;

        // Init output
        ResetOutputState(ref context, ref outFile);

        // Options identical to source (57 | 49)
        const uint opt = 57U | 49U;

        Analysis(ref context, opt, chainsaw);

        // Write standard header (type/signature/ulen/adjust)
        WriteStandardHeader(ref context, ref outFile, uLength, deltaed);

        Pack(ref context, ref outFile);
    }
}
