using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Osm.Sage.UnsafeNativeImports.Sdl3.CustomMarshallers;

namespace Osm.Sage.UnsafeNativeImports.Sdl3;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "SDL naming conventions.")]
public static partial class SDL3
{
    public record struct SDL_InitFlags(uint Value)
    {
        public static SDL_InitFlags operator |(SDL_InitFlags left, SDL_InitFlags right) =>
            new(left.Value | right.Value);

        public static SDL_InitFlags operator &(SDL_InitFlags left, SDL_InitFlags right) =>
            new(left.Value & right.Value);

        public static SDL_InitFlags operator ^(SDL_InitFlags left, SDL_InitFlags right) =>
            new(left.Value ^ right.Value);

        public static SDL_InitFlags operator ~(SDL_InitFlags value) => new(~value.Value);
    }

    public static SDL_InitFlags SDL_INIT_AUDIO => new(0x00000010U);
    public static SDL_InitFlags SDL_INIT_VIDEO => new(0x00000020U);
    public static SDL_InitFlags SDL_INIT_JOYSTICK => new(0x00000200U);
    public static SDL_InitFlags SDL_INIT_HAPTIC => new(0x00001000U);
    public static SDL_InitFlags SDL_INIT_GAMEPAD => new(0x00002000U);
    public static SDL_InitFlags SDL_INIT_EVENTS => new(0x00004000U);
    public static SDL_InitFlags SDL_INIT_SENSOR => new(0x00008000U);
    public static SDL_InitFlags SDL_INIT_CAMERA => new(0x00010000U);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_Init))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_Init(SDL_InitFlags flags);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_InitSubSystem))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_InitSubSystem(SDL_InitFlags flags);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_QuitSubSystem))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_QuitSubSystem(SDL_InitFlags flags);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_WasInit))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_InitFlags SDL_WasInit(SDL_InitFlags flags);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_Quit))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_Quit();

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_SetAppMetadata),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_SetAppMetadata(
        string? appname,
        string? appversion,
        string? appidentifier
    );

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_SetAppMetadataProperty),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_SetAppMetadataProperty(string name, string? value);

    public const string SDL_PROP_APP_METADATA_NAME_STRING = "SDL.app.metadata.name";
    public const string SDL_PROP_APP_METADATA_VERSION_STRING = "SDL.app.metadata.version";
    public const string SDL_PROP_APP_METADATA_IDENTIFIER_STRING = "SDL.app.metadata.identifier";
    public const string SDL_PROP_APP_METADATA_CREATOR_STRING = "SDL.app.metadata.creator";
    public const string SDL_PROP_APP_METADATA_COPYRIGHT_STRING = "SDL.app.metadata.copyright";
    public const string SDL_PROP_APP_METADATA_URL_STRING = "SDL.app.metadata.url";
    public const string SDL_PROP_APP_METADATA_TYPE_STRING = "SDL.app.metadata.type";

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_GetAppMetadataProperty),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalUsing(typeof(Utf8UnownedStringMarshaller))]
    public static partial string? SDL_GetAppMetadataProperty(string name);
}
