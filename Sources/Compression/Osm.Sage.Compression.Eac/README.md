# Osm.Sage.Compression.Eac

The EAC compression library.

Originally, this is not part of the public API, but for ease of development's sake, it has been split away and made into
its own library.

---

## Table of Contents

- [How to Use](#how-to-use)
- [Premade Compression Formats](#premade-compression-formats)

---

## How to Use

This library defines a single interface, `ICodex`. This interface defines the methods that are required to implement a
compression format.

After the interface is implemented, you may use the following code:

- `CodexInformation About => {...}`: allows you to get information about the compression format.
- `bool IsValid(ReadOnlySpan<byte> compressedData)`: allows you to check if the compressed data is valid.
- `int ExtractSize(ReadOnlySpan<byte> compressedData)`: allows you to extract the final size of the compressed data once
  it has been decompressed.
- `byte[] Encode(ReadOnlySpan<byte> uncompressedData)`: allows you to encode the uncompressed data.
- `byte[] Decode(ReadOnlySpan<byte> compressedData)`: allows you to decode the compressed data.

The engine itself will use the [premade formats](#premade-compression-formats), but by extracting this code into its own
library, you can create your own compression formats.

---

## Premade Compression Formats

This library supports the EAC compression formats, which are:

- BinaryTree: A binary tree compression format.
- Huffman with Runlength: A Huffman compression format with runlength encoding.
- Refpack: EA's/SAGE Refpack compression format.
