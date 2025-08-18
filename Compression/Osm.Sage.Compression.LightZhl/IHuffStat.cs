using JetBrains.Annotations;
using Osm.Sage.Compression.LightZhl.Internals;

namespace Osm.Sage.Compression.LightZhl;

/// <summary>
/// Provides statistical data and manipulation methods for Huffman compression.
/// </summary>
[PublicAPI]
public interface IHuffStat
{
    /// <summary>
    /// Represents a property that holds an array of statistical data used in Huffman compression.
    /// </summary>
    /// <remarks>
    /// This property provides direct access to the statistical information required by the Huffman encoding
    /// and decoding algorithms. It is implemented by classes like <c>DecoderStat</c> and <c>EncoderStat</c>
    /// to facilitate the manipulation and analysis of Huffman-related data.
    /// </remarks>
    short[] Stat { get; }

    /// <summary>
    /// Sorts the provided span of HuffStatTmpStruct elements based on a custom logic
    /// and prepares them for further processing in Huffman compression.
    /// </summary>
    /// <param name="s">
    /// A span of <see cref="HuffStatTmpStruct"/> elements that will be sorted
    /// and processed during this operation.
    /// </param>
    /// <returns>
    /// The number of elements successfully processed and sorted within the span.
    /// </returns>
    int MakeSortedTmp(Span<HuffStatTmpStruct> s);
}
