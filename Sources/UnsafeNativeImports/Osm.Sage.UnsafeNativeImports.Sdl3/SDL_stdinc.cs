using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Osm.Sage.UnsafeNativeImports.Sdl3;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "SDL naming conventions.")]
public static unsafe partial class SDL3
{
    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_free))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void SDL_free(void* mem);
}
