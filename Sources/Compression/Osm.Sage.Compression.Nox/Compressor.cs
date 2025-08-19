using JetBrains.Annotations;

namespace Osm.Sage.Compression.Nox;

[PublicAPI]
public static class Compressor
{
    public static void CompressFile(string inputPath, string outputPath)
    {
        using var input = File.OpenRead(inputPath);
        using var binaryReader = new BinaryReader(input);
        using var output = File.OpenWrite(outputPath);
        using var binaryWriter = new BinaryWriter(output);

        LightZhl.Compressor compressor = new();
        var compressed = compressor.Compress(binaryReader.ReadBytes((int)input.Length));
        binaryWriter.Write(compressed.ToArray());
    }

    public static Memory<byte> CompressMemory(ReadOnlyMemory<byte> input)
    {
        LightZhl.Compressor compressor = new();
        return new Memory<byte>(compressor.Compress(input.Span).ToArray());
    }
}
