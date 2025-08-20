# Osm.Sage

The OSM-SAGE (Open Source and Modernized SAGE) engine implementation.

> <span style="color: yellow;">Note:</span>
>
> This project is under construction.

> <span style="color: yellow;">Disclaimer:</span>
>
> This project is currently using AI generated XML docs and these will eventually be replaced with hand-written docs.
> I honestly don't know if they are absolutely correct until I actually rewrite them.

All code aims to be as faithful as possible to the original SAGE engine, but using dotnet. This does NOT, however, mean
that the engine aims to be online-play compatible with the original games. Memory related bugs and instability should be
removed within this project. Old bugs should be removed as well.

It is preferred to use this engine with the original game files.

To use the original game files, you NEED to purchase the original games, they will **NOT** be provided by this project.

While online backwards compatibility is not a goal, stopping the compatibility is **NOT** an objective, and it may be
possible to maintain the compatibility as a secondary happenstance.

---

## Table of Contents

- [Libraries](#libraries)
- [Tests](#tests)

---

## Libraries

- [Osm.Sage.Gimex](./Sources/Osm.Sage.Gimex/README.md) - No dependencies
- [Osm.Sage.Compression.Eac](./Sources/Compression/Osm.Sage.Compression.Eac/README.md) - Depends on:
    - [Osm.Sage.Gimex](./Sources/Osm.Sage.Gimex/README.md)
- [Osm.Sage.Compression.LightZhl](./Sources/Compression/Osm.Sage.Compression.LightZhl/README.md) - No dependencies
- [Osm.Sage.Compression.Nox](./Sources/Compression/Osm.Sage.Compression.Nox/README.md) - Depends on:
  - [Osm.Sage.LightZhl](./Sources/Compression/Osm.Sage.Compression.LightZhl/README.md)
- [Osm.Sage.Compression](./Sources/Compression/Osm.Sage.Compression/README.md) - Depends on:
  - [Osm.Sage.Compression.Eac](./Sources/Compression/Osm.Sage.Compression.Eac/README.md)
  - [Osm.Sage.Compression.LightZhl](./Sources/Compression/Osm.Sage.Compression.LightZhl/README.md)
  - [Osm.Sage.Compression.Nox](./Sources/Compression/Osm.Sage.Compression.Nox/README.md)

---

## Tests

This project has multiple tests to ensure the implementation of the libraries and tools ar as close as possible to the
original SAGE implementation.

The objective of these tests is to ensure compatibility with the original files used in the SAGE engine, as well as
modded files used by the community.

To run them, execute the following command:

```shell
dotnet test
```
