using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Osm.Sage.UnsafeNativeImports.Sdl3;

/// <summary>
/// Represents a static class providing the interface for SDL3 (Simple DirectMedia Layer version 3) functionalities.
/// SDL3 is a library used for low-level access to audio, keyboard, mouse, joystick, and graphics hardware
/// via OpenGL and Direct3D. This class encapsulates functionalities related to SDL3, serving as a layer to interact
/// with the library in a managed environment.
/// </summary>
/// <remarks>
/// SDL3 is used in scenarios requiring multimedia and graphical access and provides a range of APIs for managing
/// audio, graphics rendering, input devices, timing, threads, and other system-level resources. The class adheres
/// to the naming conventions established by the SDL library.
/// This class is decorated with attributes to ensure code consistency and suppression of style warnings
/// for compatibility-specific naming schemes in SDL.
/// </remarks>
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
