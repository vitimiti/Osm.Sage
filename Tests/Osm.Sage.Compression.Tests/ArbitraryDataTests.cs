using Osm.Sage.Compression.Eac.Codex;

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
}
