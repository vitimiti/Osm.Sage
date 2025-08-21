using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Osm.Sage.UnsafeNativeImports.Sdl3.CustomMarshallers;

namespace Osm.Sage.UnsafeNativeImports.Sdl3;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "SDL naming conventions.")]
public static partial class SDL3
{
    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_SetError),
        StringMarshalling = StringMarshalling.Custom,
        StringMarshallingCustomType = typeof(Utf8StdioCompatibleStringMarshaller)
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_SetError(string message);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_OutOfMemory))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_OutOfMemory();

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_GetError),
        StringMarshalling = StringMarshalling.Custom,
        StringMarshallingCustomType = typeof(Utf8UnownedStringMarshaller)
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial string SDL_GetError();

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ClearError))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ClearError();
}
