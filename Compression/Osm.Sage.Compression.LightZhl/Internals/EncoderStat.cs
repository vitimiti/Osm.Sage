using System.Numerics;

namespace Osm.Sage.Compression.LightZhl.Internals;

internal class EncoderStat : HuffStat
{
    internal struct Symbol
    {
        public short NBits { get; set; }
        public ushort Code { get; set; }
    }

    public int NextStat { get; set; } = Globals.HuffRecalcLen;

    public Symbol[] SymbolTable { get; } =
    [
        new() { NBits = 7, Code = 0x0014 },
        new() { NBits = 8, Code = 0x0030 },
        new() { NBits = 8, Code = 0x0031 },
        new() { NBits = 8, Code = 0x0032 },
        new() { NBits = 8, Code = 0x0033 },
        new() { NBits = 8, Code = 0x0034 },
        new() { NBits = 8, Code = 0x0035 },
        new() { NBits = 8, Code = 0x0036 },
        new() { NBits = 8, Code = 0x0037 },
        new() { NBits = 8, Code = 0x0038 },
        new() { NBits = 8, Code = 0x0039 },
        new() { NBits = 8, Code = 0x003A },
        new() { NBits = 8, Code = 0x003B },
        new() { NBits = 8, Code = 0x003C },
        new() { NBits = 8, Code = 0x003D },
        new() { NBits = 8, Code = 0x003E },
        new() { NBits = 8, Code = 0x003F },
        new() { NBits = 8, Code = 0x0040 },
        new() { NBits = 8, Code = 0x0041 },
        new() { NBits = 8, Code = 0x0042 },
        new() { NBits = 8, Code = 0x0043 },
        new() { NBits = 8, Code = 0x0044 },
        new() { NBits = 8, Code = 0x0045 },
        new() { NBits = 8, Code = 0x0046 },
        new() { NBits = 8, Code = 0x0047 },
        new() { NBits = 8, Code = 0x0048 },
        new() { NBits = 8, Code = 0x0049 },
        new() { NBits = 8, Code = 0x004A },
        new() { NBits = 8, Code = 0x004B },
        new() { NBits = 8, Code = 0x004C },
        new() { NBits = 8, Code = 0x004D },
        new() { NBits = 8, Code = 0x004E },
        new() { NBits = 7, Code = 0x0015 },
        new() { NBits = 8, Code = 0x004F },
        new() { NBits = 8, Code = 0x0050 },
        new() { NBits = 8, Code = 0x0051 },
        new() { NBits = 8, Code = 0x0052 },
        new() { NBits = 8, Code = 0x0053 },
        new() { NBits = 8, Code = 0x0054 },
        new() { NBits = 8, Code = 0x0055 },
        new() { NBits = 8, Code = 0x0056 },
        new() { NBits = 8, Code = 0x0057 },
        new() { NBits = 8, Code = 0x0058 },
        new() { NBits = 8, Code = 0x0059 },
        new() { NBits = 8, Code = 0x005A },
        new() { NBits = 8, Code = 0x005B },
        new() { NBits = 8, Code = 0x005C },
        new() { NBits = 8, Code = 0x005D },
        new() { NBits = 7, Code = 0x0016 },
        new() { NBits = 8, Code = 0x005E },
        new() { NBits = 8, Code = 0x005F },
        new() { NBits = 8, Code = 0x0060 },
        new() { NBits = 8, Code = 0x0061 },
        new() { NBits = 8, Code = 0x0062 },
        new() { NBits = 8, Code = 0x0063 },
        new() { NBits = 8, Code = 0x0064 },
        new() { NBits = 8, Code = 0x0065 },
        new() { NBits = 8, Code = 0x0066 },
        new() { NBits = 8, Code = 0x0067 },
        new() { NBits = 8, Code = 0x0068 },
        new() { NBits = 8, Code = 0x0069 },
        new() { NBits = 8, Code = 0x006A },
        new() { NBits = 8, Code = 0x006B },
        new() { NBits = 8, Code = 0x006C },
        new() { NBits = 8, Code = 0x006D },
        new() { NBits = 8, Code = 0x006E },
        new() { NBits = 8, Code = 0x006F },
        new() { NBits = 8, Code = 0x0070 },
        new() { NBits = 8, Code = 0x0071 },
        new() { NBits = 8, Code = 0x0072 },
        new() { NBits = 8, Code = 0x0073 },
        new() { NBits = 8, Code = 0x0074 },
        new() { NBits = 8, Code = 0x0075 },
        new() { NBits = 8, Code = 0x0076 },
        new() { NBits = 8, Code = 0x0077 },
        new() { NBits = 8, Code = 0x0078 },
        new() { NBits = 8, Code = 0x0079 },
        new() { NBits = 8, Code = 0x007A },
        new() { NBits = 8, Code = 0x007B },
        new() { NBits = 8, Code = 0x007C },
        new() { NBits = 8, Code = 0x007D },
        new() { NBits = 8, Code = 0x007E },
        new() { NBits = 8, Code = 0x007F },
        new() { NBits = 8, Code = 0x0080 },
        new() { NBits = 8, Code = 0x0081 },
        new() { NBits = 8, Code = 0x0082 },
        new() { NBits = 8, Code = 0x0083 },
        new() { NBits = 8, Code = 0x0084 },
        new() { NBits = 8, Code = 0x0085 },
        new() { NBits = 8, Code = 0x0086 },
        new() { NBits = 8, Code = 0x0087 },
        new() { NBits = 8, Code = 0x0088 },
        new() { NBits = 8, Code = 0x0089 },
        new() { NBits = 8, Code = 0x008A },
        new() { NBits = 8, Code = 0x008B },
        new() { NBits = 8, Code = 0x008C },
        new() { NBits = 8, Code = 0x008D },
        new() { NBits = 8, Code = 0x008E },
        new() { NBits = 8, Code = 0x008F },
        new() { NBits = 8, Code = 0x0090 },
        new() { NBits = 8, Code = 0x0091 },
        new() { NBits = 8, Code = 0x0092 },
        new() { NBits = 8, Code = 0x0093 },
        new() { NBits = 8, Code = 0x0094 },
        new() { NBits = 8, Code = 0x0095 },
        new() { NBits = 8, Code = 0x0096 },
        new() { NBits = 8, Code = 0x0097 },
        new() { NBits = 8, Code = 0x0098 },
        new() { NBits = 8, Code = 0x0099 },
        new() { NBits = 8, Code = 0x009A },
        new() { NBits = 8, Code = 0x009B },
        new() { NBits = 8, Code = 0x009C },
        new() { NBits = 8, Code = 0x009D },
        new() { NBits = 8, Code = 0x009E },
        new() { NBits = 8, Code = 0x009F },
        new() { NBits = 8, Code = 0x00A0 },
        new() { NBits = 8, Code = 0x00A1 },
        new() { NBits = 8, Code = 0x00A2 },
        new() { NBits = 8, Code = 0x00A3 },
        new() { NBits = 8, Code = 0x00A4 },
        new() { NBits = 8, Code = 0x00A5 },
        new() { NBits = 8, Code = 0x00A6 },
        new() { NBits = 8, Code = 0x00A7 },
        new() { NBits = 8, Code = 0x00A8 },
        new() { NBits = 8, Code = 0x00A9 },
        new() { NBits = 8, Code = 0x00AA },
        new() { NBits = 8, Code = 0x00AB },
        new() { NBits = 8, Code = 0x00AC },
        new() { NBits = 8, Code = 0x00AD },
        new() { NBits = 8, Code = 0x00AE },
        new() { NBits = 8, Code = 0x00AF },
        new() { NBits = 8, Code = 0x00B0 },
        new() { NBits = 8, Code = 0x00B1 },
        new() { NBits = 8, Code = 0x00B2 },
        new() { NBits = 8, Code = 0x00B3 },
        new() { NBits = 8, Code = 0x00B4 },
        new() { NBits = 8, Code = 0x00B5 },
        new() { NBits = 8, Code = 0x00B6 },
        new() { NBits = 8, Code = 0x00B7 },
        new() { NBits = 8, Code = 0x00B8 },
        new() { NBits = 8, Code = 0x00B9 },
        new() { NBits = 8, Code = 0x00BA },
        new() { NBits = 8, Code = 0x00BB },
        new() { NBits = 8, Code = 0x00BC },
        new() { NBits = 8, Code = 0x00BD },
        new() { NBits = 8, Code = 0x00BE },
        new() { NBits = 8, Code = 0x00BF },
        new() { NBits = 8, Code = 0x00C0 },
        new() { NBits = 8, Code = 0x00C1 },
        new() { NBits = 8, Code = 0x00C2 },
        new() { NBits = 8, Code = 0x00C3 },
        new() { NBits = 8, Code = 0x00C4 },
        new() { NBits = 8, Code = 0x00C5 },
        new() { NBits = 8, Code = 0x00C6 },
        new() { NBits = 8, Code = 0x00C7 },
        new() { NBits = 8, Code = 0x00C8 },
        new() { NBits = 8, Code = 0x00C9 },
        new() { NBits = 8, Code = 0x00CA },
        new() { NBits = 8, Code = 0x00CB },
        new() { NBits = 8, Code = 0x00CC },
        new() { NBits = 8, Code = 0x00CD },
        new() { NBits = 8, Code = 0x00CE },
        new() { NBits = 8, Code = 0x00CF },
        new() { NBits = 9, Code = 0x01A0 },
        new() { NBits = 9, Code = 0x01A1 },
        new() { NBits = 9, Code = 0x01A2 },
        new() { NBits = 9, Code = 0x01A3 },
        new() { NBits = 9, Code = 0x01A4 },
        new() { NBits = 9, Code = 0x01A5 },
        new() { NBits = 9, Code = 0x01A6 },
        new() { NBits = 9, Code = 0x01A7 },
        new() { NBits = 9, Code = 0x01A8 },
        new() { NBits = 9, Code = 0x01A9 },
        new() { NBits = 9, Code = 0x01AA },
        new() { NBits = 9, Code = 0x01AB },
        new() { NBits = 9, Code = 0x01AC },
        new() { NBits = 9, Code = 0x01AD },
        new() { NBits = 9, Code = 0x01AE },
        new() { NBits = 9, Code = 0x01AF },
        new() { NBits = 9, Code = 0x01B0 },
        new() { NBits = 9, Code = 0x01B1 },
        new() { NBits = 9, Code = 0x01B2 },
        new() { NBits = 9, Code = 0x01B3 },
        new() { NBits = 9, Code = 0x01B4 },
        new() { NBits = 9, Code = 0x01B5 },
        new() { NBits = 9, Code = 0x01B6 },
        new() { NBits = 9, Code = 0x01B7 },
        new() { NBits = 9, Code = 0x01B8 },
        new() { NBits = 9, Code = 0x01B9 },
        new() { NBits = 9, Code = 0x01BA },
        new() { NBits = 9, Code = 0x01BB },
        new() { NBits = 9, Code = 0x01BC },
        new() { NBits = 9, Code = 0x01BD },
        new() { NBits = 9, Code = 0x01BE },
        new() { NBits = 9, Code = 0x01BF },
        new() { NBits = 9, Code = 0x01C0 },
        new() { NBits = 9, Code = 0x01C1 },
        new() { NBits = 9, Code = 0x01C2 },
        new() { NBits = 9, Code = 0x01C3 },
        new() { NBits = 9, Code = 0x01C4 },
        new() { NBits = 9, Code = 0x01C5 },
        new() { NBits = 9, Code = 0x01C6 },
        new() { NBits = 9, Code = 0x01C7 },
        new() { NBits = 9, Code = 0x01C8 },
        new() { NBits = 9, Code = 0x01C9 },
        new() { NBits = 9, Code = 0x01CA },
        new() { NBits = 9, Code = 0x01CB },
        new() { NBits = 9, Code = 0x01CC },
        new() { NBits = 9, Code = 0x01CD },
        new() { NBits = 9, Code = 0x01CE },
        new() { NBits = 9, Code = 0x01CF },
        new() { NBits = 9, Code = 0x01D0 },
        new() { NBits = 9, Code = 0x01D1 },
        new() { NBits = 9, Code = 0x01D2 },
        new() { NBits = 9, Code = 0x01D3 },
        new() { NBits = 9, Code = 0x01D4 },
        new() { NBits = 9, Code = 0x01D5 },
        new() { NBits = 9, Code = 0x01D6 },
        new() { NBits = 9, Code = 0x01D7 },
        new() { NBits = 9, Code = 0x01D8 },
        new() { NBits = 9, Code = 0x01D9 },
        new() { NBits = 9, Code = 0x01DA },
        new() { NBits = 9, Code = 0x01DB },
        new() { NBits = 9, Code = 0x01DC },
        new() { NBits = 9, Code = 0x01DD },
        new() { NBits = 9, Code = 0x01DE },
        new() { NBits = 9, Code = 0x01DF },
        new() { NBits = 9, Code = 0x01E0 },
        new() { NBits = 9, Code = 0x01E1 },
        new() { NBits = 9, Code = 0x01E2 },
        new() { NBits = 9, Code = 0x01E3 },
        new() { NBits = 9, Code = 0x01E4 },
        new() { NBits = 9, Code = 0x01E5 },
        new() { NBits = 9, Code = 0x01E6 },
        new() { NBits = 9, Code = 0x01E7 },
        new() { NBits = 9, Code = 0x01E8 },
        new() { NBits = 9, Code = 0x01E9 },
        new() { NBits = 9, Code = 0x01EA },
        new() { NBits = 9, Code = 0x01EB },
        new() { NBits = 9, Code = 0x01EC },
        new() { NBits = 9, Code = 0x01ED },
        new() { NBits = 9, Code = 0x01EE },
        new() { NBits = 9, Code = 0x01EF },
        new() { NBits = 9, Code = 0x01F0 },
        new() { NBits = 9, Code = 0x01F1 },
        new() { NBits = 9, Code = 0x01F2 },
        new() { NBits = 9, Code = 0x01F3 },
        new() { NBits = 9, Code = 0x01F4 },
        new() { NBits = 9, Code = 0x01F5 },
        new() { NBits = 9, Code = 0x01F6 },
        new() { NBits = 9, Code = 0x01F7 },
        new() { NBits = 9, Code = 0x01F8 },
        new() { NBits = 9, Code = 0x01F9 },
        new() { NBits = 9, Code = 0x01FA },
        new() { NBits = 9, Code = 0x01FB },
        new() { NBits = 7, Code = 0x0017 },
        new() { NBits = 6, Code = 0x0000 },
        new() { NBits = 6, Code = 0x0001 },
        new() { NBits = 6, Code = 0x0002 },
        new() { NBits = 6, Code = 0x0003 },
        new() { NBits = 7, Code = 0x0008 },
        new() { NBits = 7, Code = 0x0009 },
        new() { NBits = 7, Code = 0x000A },
        new() { NBits = 7, Code = 0x000B },
        new() { NBits = 7, Code = 0x000C },
        new() { NBits = 7, Code = 0x000D },
        new() { NBits = 7, Code = 0x000E },
        new() { NBits = 7, Code = 0x000F },
        new() { NBits = 7, Code = 0x0010 },
        new() { NBits = 7, Code = 0x0011 },
        new() { NBits = 7, Code = 0x0012 },
        new() { NBits = 7, Code = 0x0013 },
        new() { NBits = 9, Code = 0x01FC },
        new() { NBits = 9, Code = 0x01FD },
    ];

    public void CalcStat(Span<int> groups)
    {
        var s = new HuffStatTmpStruct[Globals.HufSymbols];
        var total = MakeSortedTmp(s);

        NextStat = Globals.HuffRecalcLen;

        var pos = 0;
        var nTotal = 0;
        for (var group = 0; group < 14; ++group)
        {
            var avgGroup = (total - nTotal) / (16 - group);
            (int bits, int count) = ChooseGroupBits(s, pos, avgGroup);
            AddGroup(groups, group, bits);
            nTotal += count;
            pos += 1 << bits;
        }

        (int bestNBits, int bestNBits15) = FindBestSplit(s, pos);
        AddGroup(groups, 14, bestNBits);
        AddGroup(groups, 15, bestNBits15);

        FillSymbolTable(s, groups, bestNBits);
    }

    private static void AddGroup(Span<int> groups, int group, int nBits)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(nBits, 8);

        // Bubble sort
        int j;
        for (j = group; j > 0 && nBits < groups[j - 1]; --j)
        {
            groups[j] = groups[j - 1];
        }

        groups[j] = nBits;
    }

    private static (int bits, int count) ChooseGroupBits(
        Span<HuffStatTmpStruct> s,
        int pos,
        int avgGroup
    )
    {
        var i = 0;
        var n = 0;
        var nn = 0;
        var bits = 0;

        while (true)
        {
            var nItems = 1 << bits;
            var over = false;

            if (pos + i + nItems > Globals.HufSymbols)
            {
                nItems = Globals.HufSymbols - pos;
                over = true;
            }

            for (; i < nItems; ++i)
            {
                nn += s[pos + i].N;
            }

            if (over || bits >= 8 || nn > avgGroup)
            {
                if (bits == 0 || int.Abs(n - avgGroup) > int.Abs(nn - avgGroup))
                {
                    n = nn;
                }
                else
                {
                    --bits;
                }

                return (bits, n);
            }

            n = nn;
            ++bits;
        }
    }

    private static (int bestNBits, int bestNBits15) FindBestSplit(
        Span<HuffStatTmpStruct> s,
        int pos
    )
    {
        var left = 0;
        for (var j = pos; j < Globals.HufSymbols; ++j)
        {
            left += s[j].N;
        }

        var bestNBits = 0;
        var bestNBits15 = 0;
        var best = int.MaxValue;

        var i = 0;
        var nn = 0;

        var nBits = 0;
        while (true)
        {
            var nItems = 1 << nBits;
            if (pos + i + nItems > Globals.HufSymbols)
            {
                break;
            }

            for (; i < nItems; ++i)
            {
                nn += s[pos + i].N;
            }

            var nItems15 = Globals.HufSymbols - (pos + i);
            var nBits15 = nItems15 == 0 ? 0 : BitOperations.Log2((uint)(nItems15 - 1)) + 1;

            ArgumentOutOfRangeException.ThrowIfLessThan(left, nn);
            if (nBits > 8 || nBits15 > 8)
            {
                continue;
            }

            var cost = nn * nBits + (left - nn) * nBits15;
            if (cost < best)
            {
                best = cost;
                bestNBits = nBits;
                bestNBits15 = nBits15;
            }
            else
            {
                break; // PERF optimization
            }

            ++nBits;
        }

        return (bestNBits, bestNBits15);
    }

    private void FillSymbolTable(Span<HuffStatTmpStruct> s, Span<int> groups, int codeNBits)
    {
        var pos = 0;
        for (var j = 0; j < 16; ++j)
        {
            var nBitsInner = groups[j];
            var nItems = 1 << nBitsInner;
            var maxK = int.Min(nItems, Globals.HufSymbols - pos);
            for (var k = 0; k < maxK; ++k)
            {
                var symbol = s[pos + k].I;
                SymbolTable[symbol].NBits = (short)(nBitsInner + 4);
                SymbolTable[symbol].Code = (ushort)((j << codeNBits) | k);
            }

            pos += 1 << nBitsInner;
        }
    }
}
