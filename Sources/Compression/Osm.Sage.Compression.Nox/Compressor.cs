using JetBrains.Annotations;

namespace Osm.Sage.Compression.Nox;

[PublicAPI]
public static class Compressor
{
    public static void CompressFile(string inputPath, string outputPath)
    {
        throw new NotImplementedException();
    }

    public static Memory<byte> CompressMemory(ReadOnlyMemory<byte> input)
    {
        LightZhl.Compressor compressor = new();
        return new Memory<byte>(compressor.Compress(input.Span).ToArray());
    }
}
