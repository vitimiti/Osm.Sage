# Osm.Sage

The OSM-SAGE (Open Source and Modernized SAGE) engine implementation.

> <span style="color: yellow;">Note:</span>
>
> This project is under construction.

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

- [Osm.Sage.Gimex](./Osm.Sage.Gimex/README.md) - No dependencies
- [Osm.Sage.Compression.Eac](./Compression/Osm.Sage.Compression.Eac/README.md) - Depends on:
    - [Osm.Sage.Gimex](./Osm.Sage.Gimex/README.md)
- [Osm.Sage.Compression.LightZhl](./Compression/Osm.Sage.Compression.LightZhl/README.md) - No dependencies

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
