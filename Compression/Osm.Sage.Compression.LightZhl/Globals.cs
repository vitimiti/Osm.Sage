namespace Osm.Sage.Compression.LightZhl;

internal static class Globals
{
    public const int HufSymbols = 256 + 16 + 2;
    public const int HuffRecalcLen = 4096;

    public static short RecalcStat(short s) => (short)(s >> 1);
}
