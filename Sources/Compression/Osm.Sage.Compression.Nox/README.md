# Osm.Sage.Compression.Nox

This library is simply a wrapper around the `Osm.Sage.Compression.LightZhl` library.

This library would originally be a private static library that was used by the SAGE library and was used as the actual
Light ZHL library. Since the Light ZHL library in this project is a standalone library, this project instead is a
wrapper for the `Osm.Sage.Compression.LightZhl` library.

Like others, it has been made into a standalone library for easier maintenance.

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
