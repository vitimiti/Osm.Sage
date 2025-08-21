using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Osm.Sage.UnsafeNativeImports.Sdl3;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "SDL naming conventions.")]
public static unsafe partial class SDL3
{
    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_SetClipboardText),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool SDL_SetClipboardText(string text);

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_GetClipboardText),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial string SDL_GetClipboardText();

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_HasClipboardText))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool SDL_HasClipboardText();

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_SetPrimarySelectionText),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool SDL_SetPrimarySelectionText(string text);

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_GetPrimarySelectionText),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial string SDL_GetPrimarySelectionText();

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_HasPrimarySelectionText))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool SDL_HasPrimarySelectionText();
}
