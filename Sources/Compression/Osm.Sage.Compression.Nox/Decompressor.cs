using JetBrains.Annotations;

namespace Osm.Sage.Compression.Nox;

[PublicAPI]
public static class Decompressor
{
    public static void DecompressFile(string inputPath, string outputPath)
    {
        using var input = File.OpenRead(inputPath);
        using var binaryReader = new BinaryReader(input);
        using var output = File.OpenWrite(outputPath);
        using var binaryWriter = new BinaryWriter(output);

        LightZhl.Decompressor decompressor = new();
        var decompressed = decompressor.Decompress(binaryReader.ReadBytes((int)input.Length));
        binaryWriter.Write(decompressed.ToArray());
    }

    public static Memory<byte> DecompressMemory(ReadOnlyMemory<byte> input)
    {
        LightZhl.Decompressor decompressor = new();
        return new Memory<byte>(decompressor.Decompress(input.Span).ToArray());
    }
}
