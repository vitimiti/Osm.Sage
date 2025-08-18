using Osm.Sage.Compression.Eac.Codex;
using Osm.Sage.Compression.LightZhl;

namespace Osm.Sage.Compression.Eac.Tests;

public class ArbitraryDataTests
{
    public static TheoryData<byte[], byte[]> RefpackTestData =>
        new()
        {
            { CommonData.Empty, RefpackData.Empty },
            { CommonData.SingleByte, RefpackData.SingleByte },
            { CommonData.LoremIpsumShort, RefpackData.LoremIpsumShort },
            { CommonData.LoremIpsumLong, RefpackData.LoremIpsumLong },
            { CommonData.LoremIpsumVeryLong, RefpackData.LoremIpsumVeryLong },
            { CommonData.LoremIpsumRepetitive, RefpackData.LoremIpsumRepetitive },
        };

    public static TheoryData<byte[], byte[]> BinaryTreeTestData =>
        new()
        {
            { CommonData.Empty, BinaryTreeData.Empty },
            { CommonData.SingleByte, BinaryTreeData.SingleByte },
            { CommonData.LoremIpsumShort, BinaryTreeData.LoremIpsumShort },
            { CommonData.LoremIpsumLong, BinaryTreeData.LoremIpsumLong },
            { CommonData.LoremIpsumVeryLong, BinaryTreeData.LoremIpsumVeryLong },
            { CommonData.LoremIpsumRepetitive, BinaryTreeData.LoremIpsumRepetitive },
        };

    public static TheoryData<byte[], byte[]> HuffmanWithRunlengthTestData =>
        new()
        {
            { CommonData.Empty, HuffmanWithRunlengthData.Empty },
            { CommonData.SingleByte, HuffmanWithRunlengthData.SingleByte },
            { CommonData.LoremIpsumShort, HuffmanWithRunlengthData.LoremIpsumShort },
            { CommonData.LoremIpsumLong, HuffmanWithRunlengthData.LoremIpsumLong },
            { CommonData.LoremIpsumVeryLong, HuffmanWithRunlengthData.LoremIpsumVeryLong },
            { CommonData.LoremIpsumRepetitive, HuffmanWithRunlengthData.LoremIpsumRepetitive },
        };

    public static TheoryData<byte[], byte[]> LightZhlTestData =>
        new()
        {
            { CommonData.Empty, LightZhlData.Empty },
            { CommonData.SingleByte, LightZhlData.SingleByte },
            { CommonData.LoremIpsumShort, LightZhlData.LoremIpsumShort },
            { CommonData.LoremIpsumLong, LightZhlData.LoremIpsumLong },
            { CommonData.LoremIpsumVeryLong, LightZhlData.LoremIpsumVeryLong },
            { CommonData.LoremIpsumRepetitive, LightZhlData.LoremIpsumRepetitive },
        };

    [Theory]
    [MemberData(nameof(RefpackTestData))]
    public void Refpack_EncodesAndDecodesCorrectly(
        byte[] originalData,
        byte[] expectedCompressedData
    )
    {
        RefpackCodex codex = new();
        var compressedData = codex.Encode(originalData).ToArray();
        var decompressedData = codex.Decode(compressedData).ToArray();

        // Basic checks
        Assert.True(codex.IsValid(compressedData));
        Assert.Equal(originalData.Length, codex.ExtractSize(compressedData));

        // Compare the compressed data
        Assert.Equal(expectedCompressedData, compressedData);

        // Compare the decompressed data
        Assert.Equal(originalData, decompressedData);
    }

    [Theory]
    [MemberData(nameof(BinaryTreeTestData))]
    public void BinaryTree_EncodesAndDecodesCorrectly(
        byte[] originalData,
        byte[] expectedCompressedData
    )
    {
        BinaryTreeCodex codex = new();
        var compressedData = codex.Encode(originalData).ToArray();
        var decompressedData = codex.Decode(compressedData).ToArray();

        // Basic checks
        Assert.True(codex.IsValid(compressedData));
        Assert.Equal(originalData.Length, codex.ExtractSize(compressedData));

        // Compare the compressed data
        Assert.Equal(expectedCompressedData, compressedData);

        // Compare the decompressed data
        Assert.Equal(originalData, decompressedData);
    }

    [Theory]
    [MemberData(nameof(HuffmanWithRunlengthTestData))]
    public void HuffmanWithRunlength_EncodesAndDecodesCorrectly(
        byte[] originalData,
        byte[] expectedCompressedData
    )
    {
        HuffmanWithRunlengthCodex codex = new();
        var compressedData = codex.Encode(originalData).ToArray();
        var decompressedData = codex.Decode(compressedData).ToArray();

        // Basic checks
        Assert.True(codex.IsValid(compressedData));
        Assert.Equal(decompressedData.Length, codex.ExtractSize(compressedData));

        // Compare the compressed data
        Assert.Equal(expectedCompressedData, compressedData);

        // Compare the decompressed data
        Assert.Equal(originalData, decompressedData);
    }

    [Theory]
    [MemberData(nameof(LightZhlTestData))]
    public void LightZhl_EncodesAndDecodesCorrectly(
        byte[] originalData,
        byte[] expectedCompressedData
    )
    {
        Compressor compressor = new();

        var compressed = compressor.Compress(originalData);
        Assert.Equal(expectedCompressedData, compressed);
    }
}
