using JetBrains.Annotations;

namespace Osm.Sage.Compression.LightZhl;

[PublicAPI]
public class HuffStat
{
    public short[] Stat { get; } = new short[Globals.HufSymbols];

    protected int MakeSortedTmp(Span<HuffStatTmpStruct> s)
    {
        var total = 0;
        for (short j = 0; j < Globals.HufSymbols; ++j)
        {
            s[j].I = j;
            s[j].N = Stat[j];
            total += Stat[j];
            Stat[j] = Globals.RecalcStat(Stat[j]);
        }

        ShellSort(s, Globals.HufSymbols);
        return total;
    }

    private static void ShellSort(Span<HuffStatTmpStruct> a, int n)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(n / 9, 13);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(n / 9, 40);

        for (var h = 40; h > 0; h /= 3)
        {
            for (var i = h + 1; i <= n; ++i)
            {
                var v = a[i];
                var j = i;

                while ((j >= h) && (v < a[j - h]))
                {
                    a[j] = a[j - h];
                    j -= h;
                }

                a[j] = v;
            }
        }
    }
}
