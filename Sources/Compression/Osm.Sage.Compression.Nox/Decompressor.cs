using JetBrains.Annotations;

namespace Osm.Sage.Compression.Nox;

[PublicAPI]
public static class Decompressor
{
    public static void DecompressFile(string inputPath, string outputPath)
    {
        throw new NotImplementedException();
    }

    public static Memory<byte> DecompressMemory(ReadOnlyMemory<byte> input)
    {
        throw new NotImplementedException();
    }
}
