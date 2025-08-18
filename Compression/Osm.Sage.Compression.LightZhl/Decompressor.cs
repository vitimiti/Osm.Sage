using JetBrains.Annotations;
using Osm.Sage.Compression.LightZhl.Exceptions;
using Osm.Sage.Compression.LightZhl.Internals;

namespace Osm.Sage.Compression.LightZhl;

[PublicAPI]
public class Decompressor : Buffer
{
    private const int LzMin = 4;

    private enum DecodeStage
    {
        Init,
        ReadGroup,
        ReadSymbol,
        RecalcTables,
        DecodeMatchOver,
        DecodeDisplacement,
        CopyMatch,
        EndOfStream,
    }

    private readonly IHuffStat _stat = new DecoderStat();
    private int _nBits;
    private uint _bits;

    private DecodeStage _stage = DecodeStage.Init;
    private int _lastGroup = -1;
    private int _lastSymbol = -1;

    private static (int nExtraBits, int @base)[] MatchOverTable =>
        [(1, 8), (2, 10), (3, 14), (4, 22), (5, 38), (6, 70), (7, 134), (8, 262)];

    private static (int nBits, int disp)[] DispTable =>
        [(0, 0), (0, 1), (1, 2), (2, 4), (3, 8), (4, 16), (5, 32), (6, 64)];

    public List<byte> Decompress(ReadOnlySpan<byte> source)
    {
        var output = new List<byte>(source.Length * 2); // heuristic; will grow as needed
        var dec = (DecoderStat)_stat; // access group and symbol tables
        _nBits = 0;
        _bits = 0;
        _stage = DecodeStage.Init;
        _lastGroup = -1;
        _lastSymbol = -1;

        var srcIndex = 0;

        // Allocate once and reuse to avoid CA2014 warning (stackalloc inside loop)
        Span<HuffStatTmpStruct> s = stackalloc HuffStatTmpStruct[Globals.HufSymbols];
        while (true)
        {
            // Read group and symbol
            var grp = ReadGroup(source, ref srcIndex);
            ref readonly var group = ref dec.GroupTable[grp];
            var symbol = ReadSymbol(dec, in group, source, ref srcIndex);
            _lastSymbol = symbol;
            // Update statistics
            unchecked
            {
                dec.Stat[symbol]++;
            }

            // Fast-path: literal
            if (TryHandleLiteral(symbol, output))
            {
                continue;
            }

            // Recalc Huffman tables if requested
            if (TryHandleRecalcSymbol(symbol, dec, s, source, ref srcIndex))
            {
                continue;
            }

            // End-of-stream
            if (IsEndSymbol(symbol))
            {
                _stage = DecodeStage.EndOfStream;
                break;
            }

            // Decode length and displacement, then copy match
            var matchOver = DecodeMatchOver(symbol, source, ref srcIndex);
            var disp = DecodeDisplacement(source, ref srcIndex);

            if ((uint)disp >= Globals.BufSize)
            {
                throw CreateException($"Displacement out of range: disp={disp}", srcIndex);
            }

            var matchLen = matchOver + LzMin;
            if (matchLen <= 0)
            {
                throw CreateException($"Invalid match length: matchLen={matchLen}", srcIndex);
            }

            CopyMatchToOutput(matchLen, disp, output);
        }

        return output;
    }

    private DecodingException CreateException(
        string message,
        int sourceIndex,
        Exception? inner = null
    ) =>
        new(
            new DecodingExceptionData
            {
                Stage = _stage.ToString(),
                SourceIndex = sourceIndex,
                BitCount = _nBits,
                BitBuffer = _bits,
                BufferPosition = BufPos,
                LastGroup = _lastGroup,
                LastSymbol = _lastSymbol,
            },
            message,
            inner
        );

    private int ReadGroup(ReadOnlySpan<byte> source, ref int srcIndex)
    {
        _stage = DecodeStage.ReadGroup;
        var grp = Get(source, ref srcIndex, 4);
        if (grp < 0)
        {
            throw CreateException(
                "Unexpected end of compressed data while reading group.",
                srcIndex
            );
        }

        _lastGroup = grp;
        return grp;
    }

    private int ReadSymbol(
        DecoderStat dec,
        in DecoderStat.Group group,
        ReadOnlySpan<byte> source,
        ref int srcIndex
    )
    {
        _stage = DecodeStage.ReadSymbol;

        if (group.NBits == 0)
        {
            return dec.SymbolTable[group.Pos];
        }

        var got = Get(source, ref srcIndex, group.NBits);
        if (got < 0)
        {
            throw CreateException(
                "Unexpected end of compressed data while reading symbol.",
                srcIndex
            );
        }

        var pos = group.Pos + got;
        return (uint)pos >= Globals.HufSymbols
            ? throw CreateException($"Huffman position out of range: pos={pos}", srcIndex)
            : dec.SymbolTable[pos];
    }

    private bool TryHandleLiteral(int symbol, List<byte> output)
    {
        if (symbol >= 256)
        {
            return false;
        }

        var b = (byte)symbol;
        output.Add(b);
        ToBuf(b);
        return true;
    }

    private bool TryHandleRecalcSymbol(
        int symbol,
        DecoderStat dec,
        Span<HuffStatTmpStruct> s,
        ReadOnlySpan<byte> source,
        ref int srcIndex
    )
    {
        if (symbol != Globals.HufSymbols - 2)
        {
            return false;
        }

        _stage = DecodeStage.RecalcTables;

        // Recalculate Huffman tables
        dec.MakeSortedTmp(s);
        for (var i = 0; i < Globals.HufSymbols; i++)
        {
            dec.SymbolTable[i] = s[i].I;
        }

        var lastNBits = 0;
        var pos = 0;
        for (var i = 0; i < 16; i++)
        {
            // Unary-coded delta: count zeros until a 1
            var n = 0;
            while (true)
            {
                var bit = Get(source, ref srcIndex, 1);
                if (bit < 0)
                {
                    throw CreateException(
                        "Unexpected end of compressed data while rebuilding groups.",
                        srcIndex
                    );
                }

                if (bit != 0)
                {
                    break;
                }

                n++;
            }

            lastNBits += n;
            dec.GroupTable[i] = new DecoderStat.Group { NBits = lastNBits, Pos = pos };
            pos += 1 << lastNBits;
        }

        return true;
    }

    private static bool IsEndSymbol(int symbol) => symbol == Globals.HufSymbols - 1;

    private int DecodeMatchOver(int symbol, ReadOnlySpan<byte> source, ref int srcIndex)
    {
        _stage = DecodeStage.DecodeMatchOver;

        if (symbol < 256 + 8)
        {
            return symbol - 256;
        }

        var moIdx = symbol - 256 - 8;
        if ((uint)moIdx >= (uint)MatchOverTable.Length)
        {
            throw CreateException(
                $"Invalid match-over symbol index: idx={moIdx}, symbol={symbol}",
                srcIndex
            );
        }

        var mo = MatchOverTable[moIdx];
        var extra = Get(source, ref srcIndex, mo.nExtraBits);
        if (extra < 0)
        {
            throw CreateException(
                "Unexpected end of compressed data while reading match-over bits.",
                srcIndex
            );
        }

        return mo.@base + extra;
    }

    private int ReadBitsOrThrow(ReadOnlySpan<byte> source, ref int srcIndex, int n)
    {
        var v = Get(source, ref srcIndex, n);
        return v < 0
            ? throw CreateException(
                $"Unexpected end of compressed data while reading {n} bit(s).",
                srcIndex
            )
            : v;
    }

    private int ComposeWideDisp(ReadOnlySpan<byte> source, ref int srcIndex, int nBits)
    {
        var hi = ReadBitsOrThrow(source, ref srcIndex, nBits - 16);
        var lo = ReadBitsOrThrow(source, ref srcIndex, 16);
        return (hi << 16) | lo;
    }

    private int ComposeNarrowDisp(ReadOnlySpan<byte> source, ref int srcIndex, int nBits)
    {
        if (nBits <= 0)
        {
            return 0;
        }

        int disp = 0;
        if (nBits > 8)
        {
            var hi = ReadBitsOrThrow(source, ref srcIndex, 8);
            disp = hi << (nBits - 8);
            nBits -= 8;
        }

        var lo = ReadBitsOrThrow(source, ref srcIndex, nBits);
        return disp | lo;
    }

    private int DecodeDisplacement(ReadOnlySpan<byte> source, ref int srcIndex)
    {
        var dispPrefix = ReadBitsOrThrow(source, ref srcIndex, 3);

        var dItem = DispTable[dispPrefix];
        var nBits = dItem.nBits + (Globals.BufBits - 7);

        var disp =
            nBits > 16
                ? ComposeWideDisp(source, ref srcIndex, nBits)
                : ComposeNarrowDisp(source, ref srcIndex, nBits);

        return disp + (dItem.disp << (Globals.BufBits - 7));
    }

    private void CopyMatchToOutput(int matchLen, int disp, List<byte> output)
    {
        var fromPos = (int)(BufPos - (uint)disp);

        var tmp = new byte[matchLen];

        if (matchLen < disp)
        {
            // Non-overlapping copy
            BufCpy(tmp, fromPos, matchLen);
        }
        else
        {
            // Overlapping copy: first disp bytes from buffer, remaining from tmp as it fills
            BufCpy(tmp, fromPos, disp);
            for (var i = 0; i < matchLen - disp; i++)
            {
                tmp[disp + i] = tmp[i];
            }
        }

        output.AddRange(tmp);
        ToBuf(tmp);
    }

    private int Get(ReadOnlySpan<byte> source, ref int sourceIndex, int n)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(n, 8);
        // Ensure we have enough bits; keep loading until we do
        while (_nBits < n)
        {
            if (sourceIndex >= source.Length)
            {
                _nBits = 0;
                return -1;
            }

            _bits |= (uint)(source[sourceIndex++] << (24 - _nBits));
            _nBits += 8;
        }

        // Logical shift to avoid sign extension
        var ret = (int)(_bits >> (32 - n));
        _bits <<= n;
        _nBits -= n;
        return ret;
    }
}
