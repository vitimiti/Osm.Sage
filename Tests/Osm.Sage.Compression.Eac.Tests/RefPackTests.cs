using Osm.Sage.Compression.Eac.Codex;

namespace Osm.Sage.Compression.Eac.Tests;

public class RefPackTests
{
    [Theory]
    [InlineData(new byte[] { 0 }, new byte[] { 0x10, 0xFB, 0x00, 0x00, 0x00, 0xFC })]
    public void EmptyData_CompressesProperly(byte[] originalData, byte[] expectedCompressedData)
    {
        RefpackCodex codex = new();
        var compressedData = codex.Encode(originalData).ToArray();
        for (var i = 0; i < compressedData.Length; ++i)
        {
            Assert.Equal(compressedData[i], expectedCompressedData[i]);
        }
    }
}
