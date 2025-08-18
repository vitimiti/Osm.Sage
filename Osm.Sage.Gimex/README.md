# Osm.Sage.Gimex

> <span style="color: yellow;">Note:</span>
>
> This project is under construction.

This library used to be a single header file within the EAC compression directory. However, it has its own functionality
and is used in other libraries of the original SAGE project, and so it has been moved to its own library.

This library is simply a collection of utilities.

---

## Table of Contents

- [Signature](#signature)

## Signature

This class is used to generate a 4-byte signature for a given string in big-endian format. For example:

```csharp
var signature = new Signature("ref");
Console.WriteLine($"0x{signature.Value:X8}"); // 0x52454620
```

Any signature longer than four bytes will be truncated without warning or errors. Any signature shorter than four bytes
will be left-padded with zeroes due to the big-endian format.
