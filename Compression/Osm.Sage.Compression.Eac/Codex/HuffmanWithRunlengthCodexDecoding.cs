namespace Osm.Sage.Compression.Eac.Codex;

public partial class HuffmanWithRunlengthCodex
{
    private struct BitReader
    {
        public byte[] Src { get; }
        public int SrcLen { get; }

        public uint Bits { get; set; }
        public int BitsLeft { get; set; }
        public uint BitsUnshifted { get; set; }
        public int Qs { get; set; }

        internal BitReader(ReadOnlySpan<byte> source)
        {
            Src = source.ToArray();
            SrcLen = Src.Length;

            Bits = 0;
            BitsLeft = -16;
            BitsUnshifted = 0;
            Qs = 0;

            uint dummy = 0;
            GetBits(ref dummy, 0);
        }

        private byte NextByte()
        {
            if (Qs < SrcLen)
            {
                return Src[Qs++];
            }
            Qs++; // preserve relative behavior when overrun
            return 0;
        }

        public void Get16Bits()
        {
            BitsUnshifted = NextByte() | (BitsUnshifted << 8);
            BitsUnshifted = NextByte() | (BitsUnshifted << 8);
        }

        public void GetBits(ref uint v, int n)
        {
            if (n != 0)
            {
                v = Bits >> (32 - n);
                Bits <<= n;
                BitsLeft -= n;
            }

            if (BitsLeft >= 0)
            {
                return;
            }

            Get16Bits();
            Bits = BitsUnshifted << (-BitsLeft);
            BitsLeft += 16;
        }

        public void GetNum(ref int v)
        {
            if ((int)Bits < 0)
            {
                uint tv = 0;
                GetBits(ref tv, 3);
                v = (int)tv - 4;
                return;
            }

            uint tv2 = 0;

            var n = 2;
            if ((Bits >> 16) != 0)
            {
                do
                {
                    Bits <<= 1;
                    ++n;
                } while ((int)Bits >= 0);
                Bits <<= 1;
                BitsLeft -= (n - 1);
                GetBits(ref tv2, 0);
            }
            else
            {
                do
                {
                    ++n;
                    GetBits(ref tv2, 1);
                } while (tv2 == 0);
            }

            if (n > 16)
            {
                uint hi = 0;
                GetBits(ref tv2, n - 16);
                GetBits(ref hi, 16);
                v = (int)((hi | (tv2 << 16)) + ((1u << n) - 4u));
            }
            else
            {
                GetBits(ref tv2, n);
                v = (int)(tv2 + ((1u << n) - 4u));
            }
        }
    }

    private struct Tables()
    {
        public int[] BitNumTbl { get; } = new int[16];
        public uint[] DeltaTbl { get; } = new uint[16];
        public uint[] CmpTbl { get; } = new uint[16];
        public byte[] CodeTbl { get; } = new byte[256];
        public byte[] QuickCodeTbl { get; } = new byte[256];
        public byte[] QuickLenTbl { get; } = new byte[256];

        public byte Clue { get; set; } = 0;
        public int ClueLen { get; set; } = 0;
        public int MostBits { get; set; } = 0;
    }

    private static (uint type, int ulen) ReadHeader(ref BitReader reader)
    {
        var type = 0U;
        reader.GetBits(ref type, 16);

        int ulen;
        if ((type & 0x8000) != 0)
        {
            if ((type & 0x0100) != 0)
            {
                var skip = 0U;
                reader.GetBits(ref skip, 16);
                reader.GetBits(ref skip, 16);
            }

            type &= ~0x0100U;

            var hi = 0U;
            var lo = 0U;
            reader.GetBits(ref hi, 16);
            reader.GetBits(ref lo, 16);
            ulen = (int)(lo | (hi << 16));
        }
        else
        {
            if ((type & 0x0100) != 0)
            {
                var skip = 0U;
                reader.GetBits(ref skip, 8);
                reader.GetBits(ref skip, 16);
            }

            type &= ~0x0100U;

            var hi8 = 0U;
            var lo16 = 0U;
            reader.GetBits(ref hi8, 8);
            reader.GetBits(ref lo16, 16);
            ulen = (int)(lo16 | (hi8 << 16));
        }

        return (type, ulen);
    }

    private static byte ReadClue(ref BitReader reader)
    {
        var tTmp = 0U;
        reader.GetBits(ref tTmp, 8);
        return (byte)tTmp;
    }

    private static int BuildBitTables(ref BitReader reader, ref Tables t)
    {
        var numChars = 0;
        var numBits = 1;
        var baseCmp = 0U;
        uint cmp;

        do
        {
            baseCmp <<= 1;
            t.DeltaTbl[numBits] = baseCmp - (uint)numChars;

            var bn = 0;
            reader.GetNum(ref bn);
            t.BitNumTbl[numBits] = bn;

            numChars += bn;
            baseCmp += (uint)bn;

            cmp = bn != 0 ? ((baseCmp << (16 - numBits)) & 0xFFFF) : 0;

            t.CmpTbl[numBits++] = cmp;
        } while (t.BitNumTbl[numBits - 1] == 0 || cmp != 0);

        t.CmpTbl[numBits - 1] = 0xFFFFFFFF;
        t.MostBits = numBits - 1;

        return numChars;
    }

    private static void DecodeLeapfrogTable(ref BitReader reader, ref Tables t, int numChars)
    {
        var leap = new sbyte[256];
        byte nextChar = 0xFF;

        for (var i = 0; i < numChars; ++i)
        {
            var leapDelta = 0;
            reader.GetNum(ref leapDelta);
            ++leapDelta;

            do
            {
                nextChar++;
                if (leap[nextChar] == 0)
                {
                    --leapDelta;
                }
            } while (leapDelta != 0);

            leap[nextChar] = 1;
            t.CodeTbl[i] = nextChar;
        }
    }

    private static void BuildQuickTables(ref Tables t)
    {
        Array.Fill(t.QuickLenTbl, (byte)64);

        int bitsLocal;
        var codePtr = 0;
        var quickCodePtr = 0;
        var quickLenPtr = 0;

        for (bitsLocal = 1; bitsLocal <= t.MostBits; ++bitsLocal)
        {
            var bitNumLocal = t.BitNumTbl[bitsLocal];
            if (bitsLocal >= 9)
            {
                break;
            }

            var numBitEntries = 1 << (8 - bitsLocal);

            while (bitNumLocal-- > 0)
            {
                int nextCode = t.CodeTbl[codePtr++];
                var nextLen = bitsLocal;

                if (nextCode == t.Clue)
                {
                    t.ClueLen = bitsLocal;
                    nextLen = 96; // forces slow path
                }

                for (var i = 0; i < numBitEntries; ++i)
                {
                    t.QuickCodeTbl[quickCodePtr++] = (byte)nextCode;
                    t.QuickLenTbl[quickLenPtr++] = (byte)nextLen;
                }
            }
        }
    }

    private static Tables BuildTables(ref BitReader reader)
    {
        Tables t = new() { Clue = ReadClue(ref reader) };

        int numChars = BuildBitTables(ref reader, ref t);

        DecodeLeapfrogTable(ref reader, ref t, numChars);

        BuildQuickTables(ref t);

        return t;
    }

    private static void EmitQuickSymbolsUntilUnderflow(
        ref BitReader reader,
        ref Tables t,
        byte[] unpack,
        ref int qd,
        ref int numBitsLocal
    )
    {
        // Invariant: BitsLeft has already been reduced by numBitsLocal for the next emit
        while (reader.BitsLeft >= 0)
        {
            // 1
            unpack[qd++] = t.QuickCodeTbl[reader.Bits >> 24];
            reader.Bits <<= numBitsLocal;

            numBitsLocal = t.QuickLenTbl[reader.Bits >> 24];
            reader.BitsLeft -= numBitsLocal;
            if (reader.BitsLeft < 0)
            {
                break;
            }

            // 2
            unpack[qd++] = t.QuickCodeTbl[reader.Bits >> 24];
            reader.Bits <<= numBitsLocal;

            numBitsLocal = t.QuickLenTbl[reader.Bits >> 24];
            reader.BitsLeft -= numBitsLocal;
            if (reader.BitsLeft < 0)
            {
                break;
            }

            // 3
            unpack[qd++] = t.QuickCodeTbl[reader.Bits >> 24];
            reader.Bits <<= numBitsLocal;

            numBitsLocal = t.QuickLenTbl[reader.Bits >> 24];
            reader.BitsLeft -= numBitsLocal;
            if (reader.BitsLeft < 0)
            {
                break;
            }

            // 4
            unpack[qd++] = t.QuickCodeTbl[reader.Bits >> 24];
            reader.Bits <<= numBitsLocal;

            numBitsLocal = t.QuickLenTbl[reader.Bits >> 24];
            reader.BitsLeft -= numBitsLocal;
        }
    }

    private static bool TryQuickFetchAndRestart(
        ref BitReader reader,
        ref Tables t,
        byte[] unpack,
        ref int qd
    )
    {
        if (reader.BitsLeft < 0)
        {
            return false;
        }

        // Emit one symbol, fetch 16 bits, and restart outer decode loop
        unpack[qd++] = t.QuickCodeTbl[reader.Bits >> 24];
        reader.Get16Bits();
        reader.Bits = reader.BitsUnshifted << (16 - reader.BitsLeft);
        return true;
    }

    private static bool ProcessSlowSymbol(
        ref BitReader reader,
        ref Tables t,
        byte[] unpack,
        ref int qd,
        ref int numBitsLocal
    )
    {
        // 16-bit decoder
        uint cmp;
        if (numBitsLocal != 96)
        {
            cmp = reader.Bits >> 16; // 16-bit left-justified compare

            numBitsLocal = 8;
            do
            {
                ++numBitsLocal;
            } while (cmp >= t.CmpTbl[numBitsLocal]);
        }
        else
        {
            numBitsLocal = t.ClueLen;
        }

        var take = reader.Bits >> (32 - numBitsLocal);
        reader.Bits <<= numBitsLocal;
        reader.BitsLeft -= numBitsLocal;

        var code = t.CodeTbl[take - t.DeltaTbl[numBitsLocal]];

        if (code != t.Clue && reader.BitsLeft >= 0)
        {
            unpack[qd++] = code;
            return true;
        }

        if (reader.BitsLeft < 0)
        {
            reader.Get16Bits();
            reader.Bits = reader.BitsUnshifted << -reader.BitsLeft;
            reader.BitsLeft += 16;
        }

        if (code != t.Clue)
        {
            unpack[qd++] = code;
            return true;
        }

        // Handle clue (run-length or literal/EOF)
        var runLen = 0;
        var d = qd;

        reader.GetNum(ref runLen);
        if (runLen != 0)
        {
            var dest = d + runLen;
            var val = unpack[d - 1];
            while (d < dest)
            {
                unpack[d++] = val;
            }

            qd = d;
            return true;
        }

        // End Of File bit
        var eofBit = 0U;
        reader.GetBits(ref eofBit, 1);
        if (eofBit != 0)
        {
            return false; // signal EOF to caller
        }

        var explicitByte = 0U;
        reader.GetBits(ref explicitByte, 8);
        unpack[qd++] = (byte)explicitByte;
        return true;
    }

    private static void DecodeStream(ref BitReader reader, ref Tables t, byte[] unpack, ref int qd)
    {
        while (true)
        {
            int numBitsLocal = t.QuickLenTbl[reader.Bits >> 24];
            reader.BitsLeft -= numBitsLocal;

            EmitQuickSymbolsUntilUnderflow(ref reader, ref t, unpack, ref qd, ref numBitsLocal);

            reader.BitsLeft += 16;

            if (TryQuickFetchAndRestart(ref reader, ref t, unpack, ref qd))
            {
                continue;
            }

            reader.BitsLeft = reader.BitsLeft - 16 + numBitsLocal;

            if (ProcessSlowSymbol(ref reader, ref t, unpack, ref qd, ref numBitsLocal))
            {
                continue;
            }

            // EOF reached
            return;
        }
    }

    private static void ApplyPostProcess(uint type, byte[] unpack, int ulen)
    {
        switch (type)
        {
            case 0x32FB or 0xB2FB:
            {
                var acc = 0;
                for (var i = 0; i < ulen; i++)
                {
                    acc += unpack[i];
                    unpack[i] = (byte)acc;
                }

                break;
            }
            case 0x34FB or 0xB4FB:
            {
                var iAcc = 0;
                var nextChar = 0;
                for (var i = 0; i < ulen; i++)
                {
                    iAcc += unpack[i];
                    nextChar += iAcc;
                    unpack[i] = (byte)nextChar;
                }

                break;
            }
        }
    }

    private static List<byte> Decompress(ReadOnlySpan<byte> source)
    {
        var reader = new BitReader(source);
        (uint type, int ulen) = ReadHeader(ref reader);

        if (ulen == 0)
        {
            return [];
        }

        var unpack = new byte[ulen];
        int qd = 0;

        var tables = BuildTables(ref reader);

        DecodeStream(ref reader, ref tables, unpack, ref qd);

        ApplyPostProcess(type, unpack, ulen);

        return unpack.ToList();
    }
}
