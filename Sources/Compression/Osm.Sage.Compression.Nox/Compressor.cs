using JetBrains.Annotations;

namespace Osm.Sage.Compression.Nox;

/// <summary>
/// Provides utility methods for compressing files and in-memory data using the <see cref="LightZhl"/> compression
/// algorithms.
/// </summary>
[PublicAPI]
public static class Compressor
{
    /// <summary>
    /// Compresses the input file specified by the given path and writes the compressed data to the output file path.
    /// </summary>
    /// <param name="inputPath">The path of the file to be compressed.</param>
    /// <param name="outputPath">The path where the compressed file will be written.</param>
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

    /// <summary>
    /// Compresses the input data provided as a read-only memory and returns the compressed data as memory.
    /// </summary>
    /// <param name="input">The input data to be compressed, provided as a ReadOnlyMemory of bytes.</param>
    /// <returns>The compressed data as a Memory of bytes.</returns>
    public static Memory<byte> CompressMemory(ReadOnlyMemory<byte> input)
    {
        LightZhl.Compressor compressor = new();
        return new Memory<byte>(compressor.Compress(input.Span).ToArray());
    }
}
