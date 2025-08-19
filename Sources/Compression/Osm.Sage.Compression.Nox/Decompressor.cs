using JetBrains.Annotations;

namespace Osm.Sage.Compression.Nox;

/// <summary>
/// Provides methods for decompressing data from files or memory using a custom LightZhl compression algorithm.
/// </summary>
/// <remarks>
/// This class serves as a utility to handle decompression operations. It leverages the functionality of the
/// <see cref="Osm.Sage.Compression.LightZhl.Decompressor"/> to process compressed data either from file streams or memory buffers.
/// </remarks>
[PublicAPI]
public static class Decompressor
{
    /// <summary>
    /// Decompresses a file containing compressed data using the LightZhl compression algorithm.
    /// </summary>
    /// <param name="inputPath">The file path to the input compressed file.</param>
    /// <param name="outputPath">The file path where the decompressed output should be written.</param>
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

    /// <summary>
    /// Decompresses a memory buffer containing compressed data using the LightZhl compression algorithm.
    /// </summary>
    /// <param name="input">The read-only memory buffer containing the compressed data to be decompressed.</param>
    /// <returns>A memory buffer containing the decompressed data.</returns>
    public static Memory<byte> DecompressMemory(ReadOnlyMemory<byte> input)
    {
        LightZhl.Decompressor decompressor = new();
        return new Memory<byte>(decompressor.Decompress(input.Span).ToArray());
    }
}
