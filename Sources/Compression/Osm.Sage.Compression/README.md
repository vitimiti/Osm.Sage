# Osm.Sage.Compression

This is the SAGE compression library.

This library is simply a manager that wraps the `Osm.Sage.Compression.Eac`, `Osm.Sage.Compression.LightZhl` and
`Osm.Sage.Compression.Nox` libraries, and adds ZLib compression.

---

## Table of Contents

- [Utilities](#utilities)
- [Compression](#compression)
- [Decompression](#decompression)

---

## Utilities

You may find out if some data is compressed or not by using the
`Osm.Sage.Compression.CompressionManager.IsCompressed(ReadOnlySpan<byte>)` method.

You may also use the `Osm.Sage.Compression.CompressionManager.IsCompressed(ReadOnlySpan<byte>, out CompressionType)`
method to get the compression type.

---

## Compression

To compress data, use the `Osm.Sage.Compression.CompressionManager.Compress(ReadOnlySpan<byte>, CompressionType)` method.

---

## Decompression

To decompress data, use the `Osm.Sage.Compression.CompressionManager.Decompress(ReadOnlySpan<byte>)` method.
