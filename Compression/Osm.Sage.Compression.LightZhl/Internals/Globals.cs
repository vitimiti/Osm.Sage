namespace Osm.Sage.Compression.LightZhl.Internals;

internal static class Globals
{
    public const int HufSymbols = 256 + 16 + 2;
    public const int HuffRecalcLen = 4096;
    public const int BufBits = 16;
    public const int BufSize = 1 << BufBits;

    public static short RecalcStat(short s) => (short)(s >> 1);
}
