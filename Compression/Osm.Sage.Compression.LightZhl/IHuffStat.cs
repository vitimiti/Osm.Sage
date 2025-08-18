using JetBrains.Annotations;
using Osm.Sage.Compression.LightZhl.Internals;

namespace Osm.Sage.Compression.LightZhl;

[PublicAPI]
public interface IHuffStat
{
    short[] Stat { get; }

    int MakeSortedTmp(Span<HuffStatTmpStruct> s);
}
