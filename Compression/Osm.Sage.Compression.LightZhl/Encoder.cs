using JetBrains.Annotations;
using DispItem = (int NBits, ushort Bits);
using MatchOverItem = (int Symbol, int NBits, ushort Bits);

namespace Osm.Sage.Compression.LightZhl;

[PublicAPI]
public class Encoder(EncoderStat stat, byte[] dest)
{
    public const int MaxMatchOver = 517;
    public const int MaxRaw = 64;

    private readonly short[] _sStat = stat.Stat;

    private int _nextStat = stat.NextStat;
    private int _destIndex;
    private int _bits;
    private int _nBits;

    public void PutRaw(ReadOnlySpan<byte> source)
    {
        foreach (var b in source)
        {
            Put(b);
        }
    }

    public void PutMatch(ReadOnlySpan<byte> source, int matchOver, int disp)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(source.Length, MaxRaw);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(matchOver, MaxMatchOver);
        ArgumentOutOfRangeException.ThrowIfLessThan(disp, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(disp, Globals.BufSize);

        PutRaw(source);

        MatchOverItem[] matchOverTable =
        [
            (264, 1, 0x00),
            (265, 2, 0x00),
            (265, 2, 0x02),
            (266, 3, 0x00),
            (266, 3, 0x02),
            (266, 3, 0x04),
            (266, 3, 0x06),
            (267, 4, 0x00),
            (267, 4, 0x02),
            (267, 4, 0x04),
            (267, 4, 0x06),
            (267, 4, 0x08),
            (267, 4, 0x0A),
            (267, 4, 0x0C),
            (267, 4, 0x0E),
        ];

        switch (matchOver)
        {
            case < 8:
                Put((ushort)(256 + matchOver));
                break;
            case < 38:
            {
                matchOver -= 8;
                var matchItem = matchOverTable[matchOver >> 1];
                Put(
                    (ushort)matchItem.Symbol,
                    matchItem.NBits,
                    (uint)(matchItem.Bits | (matchOver & 0x01))
                );
                break;
            }
            default:
            {
                matchOver -= 38;
                var matchItem = matchOverTable[matchOver >> 5];
                Put((ushort)(matchItem.Symbol + 4));
                PutBits(matchItem.NBits + 4, (uint)((matchItem.Bits << 4) | (matchOver & 0x1F)));
                break;
            }
        }

        DispItem[] dispTable =
        [
            (3, 0x0000),
            (3, 0x0001),
            (4, 0x0004),
            (4, 0x0005),
            (5, 0x000C),
            (5, 0x000D),
            (5, 0x000E),
            (5, 0x000F),
            (6, 0x0020),
            (6, 0x0021),
            (6, 0x0022),
            (6, 0x0023),
            (6, 0x0024),
            (6, 0x0025),
            (6, 0x0026),
            (6, 0x0027),
            (7, 0x0050),
            (7, 0x0051),
            (7, 0x0052),
            (7, 0x0053),
            (7, 0x0054),
            (7, 0x0055),
            (7, 0x0056),
            (7, 0x0057),
            (7, 0x0058),
            (7, 0x0059),
            (7, 0x005A),
            (7, 0x005B),
            (7, 0x005C),
            (7, 0x005D),
            (7, 0x005E),
            (7, 0x005F),
            (8, 0x00C0),
            (8, 0x00C1),
            (8, 0x00C2),
            (8, 0x00C3),
            (8, 0x00C4),
            (8, 0x00C5),
            (8, 0x00C6),
            (8, 0x00C7),
            (8, 0x00C8),
            (8, 0x00C9),
            (8, 0x00CA),
            (8, 0x00CB),
            (8, 0x00CC),
            (8, 0x00CD),
            (8, 0x00CE),
            (8, 0x00CF),
            (8, 0x00D0),
            (8, 0x00D1),
            (8, 0x00D2),
            (8, 0x00D3),
            (8, 0x00D4),
            (8, 0x00D5),
            (8, 0x00D6),
            (8, 0x00D7),
            (8, 0x00D8),
            (8, 0x00D9),
            (8, 0x00DA),
            (8, 0x00DB),
            (8, 0x00DC),
            (8, 0x00DD),
            (8, 0x00DE),
            (8, 0x00DF),
            (9, 0x01C0),
            (9, 0x01C1),
            (9, 0x01C2),
            (9, 0x01C3),
            (9, 0x01C4),
            (9, 0x01C5),
            (9, 0x01C6),
            (9, 0x01C7),
            (9, 0x01C8),
            (9, 0x01C9),
            (9, 0x01CA),
            (9, 0x01CB),
            (9, 0x01CC),
            (9, 0x01CD),
            (9, 0x01CE),
            (9, 0x01CF),
            (9, 0x01D0),
            (9, 0x01D1),
            (9, 0x01D2),
            (9, 0x01D3),
            (9, 0x01D4),
            (9, 0x01D5),
            (9, 0x01D6),
            (9, 0x01D7),
            (9, 0x01D8),
            (9, 0x01D9),
            (9, 0x01DA),
            (9, 0x01DB),
            (9, 0x01DC),
            (9, 0x01DD),
            (9, 0x01DE),
            (9, 0x01DF),
            (9, 0x01E0),
            (9, 0x01E1),
            (9, 0x01E2),
            (9, 0x01E3),
            (9, 0x01E4),
            (9, 0x01E5),
            (9, 0x01E6),
            (9, 0x01E7),
            (9, 0x01E8),
            (9, 0x01E9),
            (9, 0x01EA),
            (9, 0x01EB),
            (9, 0x01EC),
            (9, 0x01ED),
            (9, 0x01EE),
            (9, 0x01EF),
            (9, 0x01F0),
            (9, 0x01F1),
            (9, 0x01F2),
            (9, 0x01F3),
            (9, 0x01F4),
            (9, 0x01F5),
            (9, 0x01F6),
            (9, 0x01F7),
            (9, 0x01F8),
            (9, 0x01F9),
            (9, 0x01FA),
            (9, 0x01FB),
            (9, 0x01FC),
            (9, 0x01FD),
            (9, 0x01FE),
            (9, 0x01FF),
        ];

        var dispItem = dispTable[disp >> (Globals.BufBits - 7)];
        var nBits = dispItem.NBits + (Globals.BufBits - 7);
        var bits = (uint)(
            dispItem.Bits << (Globals.BufBits - 7) | (disp & ((1 << (Globals.BufBits - 7)) - 1))
        );

        if (nBits > 16)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(nBits, 32);
            PutBits(nBits - 16, bits >> 16);
            PutBits(16, bits & 0xFFFF);
        }
        else
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(nBits, 16);
            PutBits(nBits, bits);
        }
    }

    public void Flush()
    {
        Put(Globals.HufSymbols - 1);
        while (_nBits > 0)
        {
            dest[_destIndex++] = (byte)(_bits >> 24);
            _nBits -= 8;
            _bits <<= 8;
        }
    }

    private void PutBits(int codeBits, uint code)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(codeBits, 16);
        _bits |= (int)(code << (32 - _nBits - codeBits));
        _nBits += codeBits;
        if (_nBits < 16)
        {
            return;
        }

        dest[_destIndex++] = (byte)(_bits >> 24);
        dest[_destIndex++] = (byte)(_bits >> 16);
        _nBits -= 16;
        _bits <<= 16;
    }

    private void CallStat()
    {
        _nextStat = 2; // To avoid recursion, >= 2
        Put(Globals.HufSymbols - 2);

        Span<int> groups = stackalloc int[16];
        stat.CalcStat(groups);

        var lastNBits = 0;
        for (var i = 0; i < 16; ++i)
        {
            var nBits = groups[i];
            ArgumentOutOfRangeException.ThrowIfLessThan(nBits, lastNBits);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(nBits, 8);
            var delta = nBits = lastNBits;
            lastNBits = nBits;
            PutBits(delta + 1, 1);
        }
    }

    private void Put(ushort symbol)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(symbol, Globals.HufSymbols);

        if (--_nextStat <= 0)
        {
            CallStat();
        }

        ++_sStat[symbol];

        EncoderStat.Symbol item = stat.SymbolTable[symbol];
        ArgumentOutOfRangeException.ThrowIfLessThan(item.NBits, 0);
        PutBits(item.NBits, item.Code);
    }

    private void Put(ushort symbol, int codeBits, uint code)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(symbol, Globals.HufSymbols);
        ArgumentOutOfRangeException.ThrowIfLessThan(codeBits, 4);
        if (--_nextStat <= 0)
        {
            CallStat();
        }

        ++_sStat[symbol];

        EncoderStat.Symbol item = stat.SymbolTable[symbol];
        ArgumentOutOfRangeException.ThrowIfLessThan(item.NBits, 0);

        var nBits = item.NBits;
        PutBits(nBits + codeBits, (uint)(item.Code << codeBits) | code);
    }
}
