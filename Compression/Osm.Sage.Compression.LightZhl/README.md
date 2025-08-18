# Osm.Sage.Compression.LightZhl

This is the ZHL light compression library.

This library would originally be private and used by the private NOX compression library, but for maintainability
reasons, it has been made public.

This library only supports the ZHL compression format.

---

## Table of Contents

- [Compression](#compression)
- [Decompression](#decompression)

---

## Compression

To compress a stream of data, use the `Osm.Sage.Compression.LightZhl.Compressor` class:

```csharp
using Osm.Sage.Compression.LightZhl;

var compressor = new Compressor();
var compressedData = compressor.Compress(uncompressedData);
```

---

## Decompression

To decompress a stream of data, use the `Osm.Sage.Compression.LightZhl.Decompressor` class:

```csharp
using Osm.Sage.Compression.LightZhl;

var decompressor = new Decompressor();
var decompressedData = compressor.Decompress(compressedData);
```

---
