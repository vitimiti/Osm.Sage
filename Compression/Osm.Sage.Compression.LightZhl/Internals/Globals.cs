using System.Numerics;

namespace Osm.Sage.Compression.LightZhl.Internals;

internal static class Globals
{
    private const int TableBits = 15;
    private const int SlowHashShift = 5;
    private const int FastHashShift = (TableBits + Match - 1) / Match;
    private const int HashMask = TableSize - 1;

    public const int HufSymbols = 256 + 16 + 2;
    public const int HuffRecalcLen = 4096;
    public const int BufBits = 16;
    public const int BufSize = 1 << BufBits;
    public const int TableSize = 1 << TableBits;
    public const int BufMask = BufSize - 1;
    public const int SkipHash = 1024;
    public const int Match = 5;

    public static short RecalcStat(short s) => (short)(s >> 1);

    public static uint UpdateHash(uint hash, uint c, bool slowHash)
    {
        if (slowHash)
        {
            hash ^= c;
            hash = BitOperations.RotateLeft(hash, SlowHashShift);
            return hash;
        }

        hash = (hash << FastHashShift) ^ c;
        return hash;
    }

    public static uint UpdateHash(uint hash, ReadOnlySpan<byte> source, int offset, bool slowHash)
    {
        if (slowHash)
        {
            hash ^= BitOperations.RotateLeft(source[offset], SlowHashShift * Match);
            hash ^= source[offset + Match];
            hash = BitOperations.RotateLeft(hash, SlowHashShift);
            return hash;
        }

        hash = (hash << FastHashShift) ^ source[offset + Match];
        return hash;
    }

    public static uint HashPos(uint hash, bool slowHash) =>
        slowHash ? (hash * 214013 + 2531011) >> (32 - TableBits) : hash & HashMask;

    public static uint CalcHash(ReadOnlySpan<byte> source, bool slowHash)
    {
        var hash = 0U;
        foreach (var b in source)
        {
            hash = UpdateHash(hash, b, slowHash);
        }

        return hash;
    }
}
