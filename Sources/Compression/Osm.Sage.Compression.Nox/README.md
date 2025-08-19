# Osm.Sage.Compression.Nox

This library is simply a wrapper around the `Osm.Sage.Compression.LightZhl` library.

---

## Table of Contents

- [Compression](#compression)
- [Decompression](#decompression)

---

## Compression

It has two utilities, one to compress files and the other to compress memory:

```csharp
using Osm.Sage.Compression.Nox;

// This will produce a compressed file called "compressed.txt"
// from the file "decompressed.txt"
Compressor.CompressFile("./decompressed.txt", "./compressed.txt");

// This will produce a compressed byte data list from the given memory.
Memory<byte> data = // ...
var compressed = Compressor.CompressMemory(data);
```
---

## Decompression

It has two utilities, one to decompress files and the other to decompress memory:

```csharp
using Osm.Sage.Compression.Nox;

// This will produce a decompressed file called "decompressed.txt"
// from the file "compressed.txt"
Decompressor.DecompressFile("./compressed.txt", "./decompressed.txt");

// This will produce decompressed byte data list from the given compressed data.
Memory<byte> compressed = // ...
var decompressed = Decompressor.DecompressMemory(compressed);
```
