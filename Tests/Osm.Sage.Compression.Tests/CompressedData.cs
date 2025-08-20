namespace Osm.Sage.Compression.Eac.Tests;

internal static class CompressedData
{
    internal static class BinaryTree
    {
        public static byte[] Empty =>
            [.. "EAB\0\0"u8.ToArray(), .. BinaryTreeData.Empty[2..5], .. BinaryTreeData.Empty];

        public static byte[] SingleByte =>
            [
                .. "EAB\0\0"u8.ToArray(),
                .. BinaryTreeData.SingleByte[2..5],
                .. BinaryTreeData.SingleByte,
            ];

        public static byte[] LoremIpsumShort =>
            [
                .. "EAB\0\0"u8.ToArray(),
                .. BinaryTreeData.LoremIpsumShort[2..5],
                .. BinaryTreeData.LoremIpsumShort,
            ];

        public static byte[] LoremIpsumLong =>
            [
                .. "EAB\0\0"u8.ToArray(),
                .. BinaryTreeData.LoremIpsumLong[2..5],
                .. BinaryTreeData.LoremIpsumLong,
            ];

        public static byte[] LoremIpsumVeryLong =>
            [
                .. "EAB\0\0"u8.ToArray(),
                .. BinaryTreeData.LoremIpsumVeryLong[2..5],
                .. BinaryTreeData.LoremIpsumVeryLong,
            ];

        public static byte[] LoremIpsumRepetitive =>
            [
                .. "EAB\0\0"u8.ToArray(),
                .. BinaryTreeData.LoremIpsumRepetitive[2..5],
                .. BinaryTreeData.LoremIpsumRepetitive,
            ];
    }

    internal static class HuffmanWithRunLength
    {
        public static byte[] Empty =>
            [
                .. "EAH\0\0"u8.ToArray(),
                .. HuffmanWithRunlengthData.Empty[2..5],
                .. HuffmanWithRunlengthData.Empty,
            ];

        public static byte[] SingleByte =>
            [
                .. "EAH\0\0"u8.ToArray(),
                .. HuffmanWithRunlengthData.SingleByte[2..5],
                .. HuffmanWithRunlengthData.SingleByte,
            ];

        public static byte[] LoremIpsumShort =>
            [
                .. "EAH\0\0"u8.ToArray(),
                .. HuffmanWithRunlengthData.LoremIpsumShort[2..5],
                .. HuffmanWithRunlengthData.LoremIpsumShort,
            ];

        public static byte[] LoremIpsumLong =>
            [
                .. "EAH\0\0"u8.ToArray(),
                .. HuffmanWithRunlengthData.LoremIpsumLong[2..5],
                .. HuffmanWithRunlengthData.LoremIpsumLong,
            ];

        public static byte[] LoremIpsumVeryLong =>
            [
                .. "EAH\0\0"u8.ToArray(),
                .. HuffmanWithRunlengthData.LoremIpsumVeryLong[2..5],
                .. HuffmanWithRunlengthData.LoremIpsumVeryLong,
            ];

        public static byte[] LoremIpsumRepetitive =>
            [
                .. "EAH\0\0"u8.ToArray(),
                .. HuffmanWithRunlengthData.LoremIpsumRepetitive[2..5],
                .. HuffmanWithRunlengthData.LoremIpsumRepetitive,
            ];
    }

    internal static class Refpack
    {
        public static byte[] Empty =>
            [.. "EAR\0\0"u8.ToArray(), .. RefpackData.Empty[2..5], .. RefpackData.Empty];

        public static byte[] SingleByte =>
            [.. "EAR\0\0"u8.ToArray(), .. RefpackData.SingleByte[2..5], .. RefpackData.SingleByte];

        public static byte[] LoremIpsumShort =>
            [
                .. "EAR\0\0"u8.ToArray(),
                .. RefpackData.LoremIpsumShort[2..5],
                .. RefpackData.LoremIpsumShort,
            ];

        public static byte[] LoremIpsumLong =>
            [
                .. "EAR\0\0"u8.ToArray(),
                .. RefpackData.LoremIpsumLong[2..5],
                .. RefpackData.LoremIpsumLong,
            ];

        public static byte[] LoremIpsumVeryLong =>
            [
                .. "EAR\0\0"u8.ToArray(),
                .. RefpackData.LoremIpsumVeryLong[2..5],
                .. RefpackData.LoremIpsumVeryLong,
            ];

        public static byte[] LoremIpsumRepetitive =>
            [
                .. "EAR\0\0"u8.ToArray(),
                .. RefpackData.LoremIpsumRepetitive[2..5],
                .. RefpackData.LoremIpsumRepetitive,
            ];
    }

    // The sizes aren't encoded here, so we are adding them manually.
    internal static class NoxLzh
    {
        public static byte[] Empty => [.. "NOX\0\0\0\0\0"u8.ToArray(), .. LightZhlData.Empty];

        public static byte[] SingleByte =>
            [.. "NOX\0\0\0\0"u8.ToArray(), 0x01, .. LightZhlData.SingleByte];

        public static byte[] LoremIpsumShort =>
            [.. "NOX\0\0\0\0"u8.ToArray(), 0x7B, .. LightZhlData.LoremIpsumShort];

        public static byte[] LoremIpsumLong =>
            [.. "NOX\0\0\0"u8.ToArray(), 0x02, 0x95, .. LightZhlData.LoremIpsumLong];

        public static byte[] LoremIpsumVeryLong =>
            [.. "NOX\0\0\0"u8.ToArray(), 0x05, 0x1D, .. LightZhlData.LoremIpsumVeryLong];

        public static byte[] LoremIpsumRepetitive =>
            [.. "NOX\0\0\0"u8.ToArray(), 0x05, 0x78, .. LightZhlData.LoremIpsumRepetitive];
    }
}
