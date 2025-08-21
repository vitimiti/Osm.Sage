using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Osm.Sage.UnsafeNativeImports.Sdl3;

[PublicAPI]
[SuppressMessage(
    "Interoperability",
    "CA1401:P/Invokes should not be visible",
    Justification = "SDL direct imports."
)]
[SuppressMessage(
    "csharpsquid",
    "S101:Types should be named in PascalCase",
    Justification = "SDL naming conventions."
)]
[SuppressMessage(
    "csharpsquid",
    "S2342:Enumeration types should comply with a naming convention",
    Justification = "SDL naming conventions."
)]
[SuppressMessage(
    "csharpsquid",
    "S4200:Native methods should be wrapped",
    Justification = "SDL direct imports."
)]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "SDL naming conventions.")]
public static partial class SDL3
{
    private static KeyValuePair<string, bool>[] LibraryNames =>
        [
            new("SDL3.dll", OperatingSystem.IsWindows()),
            new("libSDL3.dylib", OperatingSystem.IsMacOS()),
            new("libSDL3.so", OperatingSystem.IsLinux()),
        ];

    static SDL3() =>
        NativeLibrary.SetDllImportResolver(
            typeof(SDL3).Assembly,
            (name, assembly, path) =>
                NativeLibrary.Load(
                    name switch
                    {
                        nameof(SDL3) => LibraryNames.FirstOrDefault(pair => pair.Value).Key ?? name,
                        _ => name,
                    },
                    assembly,
                    path
                )
        );
}
