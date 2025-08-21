using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using JetBrains.Annotations;
using Osm.Sage.UnsafeNativeImports.Sdl3.CustomMarshallers;

namespace Osm.Sage.UnsafeNativeImports.Sdl3;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "SDL naming conventions.")]
public static unsafe partial class SDL3
{
    [PublicAPI]
    public record struct SDL_PropertiesID(uint Value)
    {
        public static SDL_PropertiesID Invalid => new(0);
    }

    public enum SDL_PropertyType;

    public static SDL_PropertyType SDL_PROPERTY_TYPE_INVALID => 0;
    public static SDL_PropertyType SDL_PROPERTY_TYPE_POINTER => (SDL_PropertyType)1;
    public static SDL_PropertyType SDL_PROPERTY_TYPE_STRING => (SDL_PropertyType)2;
    public static SDL_PropertyType SDL_PROPERTY_TYPE_NUMBER => (SDL_PropertyType)3;
    public static SDL_PropertyType SDL_PROPERTY_TYPE_FLOAT => (SDL_PropertyType)4;
    public static SDL_PropertyType SDL_PROPERTY_TYPE_BOOLEAN => (SDL_PropertyType)5;

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_GetGlobalProperties))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_PropertiesID SDL_GetGlobalProperties();

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_CreateProperties))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_PropertiesID SDL_CreateProperties();

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_CopyProperties))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_CopyProperties(SDL_PropertiesID src, SDL_PropertiesID dst);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_LockProperties))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_LockProperties(SDL_PropertiesID props);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_UnlockProperties))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_UnlockProperties();

    public delegate void SDL_CleanupPropertyCallback(nint value);

    private static delegate* unmanaged[Cdecl]<nint, nint, void> SDL_CleanupPropertyCallbackPtr =
        &SDL_CleanupPropertyCallbackImpl;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void SDL_CleanupPropertyCallbackImpl(nint value, nint userdata)
    {
        var callback =
            GCHandle.FromIntPtr(new IntPtr(userdata)).Target as SDL_CleanupPropertyCallback;

        callback?.Invoke(value);
    }

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_SetPointerPropertyWithCleanup),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool INTERNAL_SDL_SetPointerPropertyWithCleanup(
        SDL_PropertiesID props,
        string name,
        nint value,
        delegate* unmanaged[Cdecl]<nint, nint, void> cleanup,
        nint userdata
    );

    public static bool SDL_SetPointerPropertyWithCleanup(
        SDL_PropertiesID props,
        string name,
        nint value,
        SDL_CleanupPropertyCallback cleanup
    )
    {
        var handle = GCHandle.Alloc(cleanup);
        try
        {
            return INTERNAL_SDL_SetPointerPropertyWithCleanup(
                props,
                name,
                value,
                SDL_CleanupPropertyCallbackPtr,
                GCHandle.ToIntPtr(handle)
            );
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_SetPointerProperty),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_SetPointerProperty(
        SDL_PropertiesID props,
        string name,
        nint value
    );

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_SetStringProperty),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_SetStringProperty(
        SDL_PropertiesID props,
        string name,
        string? value
    );

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_SetNumberProperty),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_SetNumberProperty(
        SDL_PropertiesID props,
        string name,
        long value
    );

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_SetFloatProperty),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_SetFloatProperty(
        SDL_PropertiesID props,
        string name,
        float value
    );

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_SetBooleanProperty),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_SetBooleanProperty(
        SDL_PropertiesID props,
        string name,
        [MarshalAs(UnmanagedType.U1)] bool value
    );

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_HasProperty),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_HasProperty(SDL_PropertiesID props, string name);

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_GetPropertyType),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_PropertyType SDL_GetPropertyType(SDL_PropertiesID props, string name);

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_GetPointerProperty),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nint SDL_GetPointerProperty(
        SDL_PropertiesID props,
        string name,
        nint default_value
    );

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_GetStringProperty),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalUsing(typeof(Utf8UnownedStringMarshaller))]
    public static partial string? SDL_GetStringProperty(
        SDL_PropertiesID props,
        string name,
        string? default_value
    );

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_GetNumberProperty),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial long SDL_GetNumberProperty(
        SDL_PropertiesID props,
        string name,
        long default_value
    );

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_GetFloatProperty),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial float SDL_GetFloatProperty(
        SDL_PropertiesID props,
        string name,
        float default_value
    );

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_GetBooleanProperty),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_GetBooleanProperty(
        SDL_PropertiesID props,
        string name,
        [MarshalAs(UnmanagedType.U1)] bool default_value
    );

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_ClearProperty),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ClearProperty(SDL_PropertiesID props, string name);

    public delegate void SDL_EnumeratePropertiesCallback(SDL_PropertiesID props, string name);

    private static delegate* unmanaged[Cdecl]<
        nint,
        SDL_PropertiesID,
        byte*,
        void> SDL_EnumeratePropertiesCallbackPtr = &SDL_EnumeratePropertiesCallbackImpl;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void SDL_EnumeratePropertiesCallbackImpl(
        nint userdata,
        SDL_PropertiesID props,
        byte* name
    )
    {
        var callback =
            GCHandle.FromIntPtr(new IntPtr(userdata)).Target as SDL_EnumeratePropertiesCallback;

        callback?.Invoke(props, Utf8StringMarshaller.ConvertToManaged(name)!);
    }

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_EnumerateProperties))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool INTERNAL_SDL_EnumerateProperties(
        SDL_PropertiesID props,
        delegate* unmanaged[Cdecl]<nint, SDL_PropertiesID, byte*, void> callback,
        nint userdata
    );

    public static bool SDL_EnumerateProperties(
        SDL_PropertiesID props,
        SDL_EnumeratePropertiesCallback callback
    )
    {
        var handle = GCHandle.Alloc(callback);
        try
        {
            return INTERNAL_SDL_EnumerateProperties(
                props,
                SDL_EnumeratePropertiesCallbackPtr,
                GCHandle.ToIntPtr(handle)
            );
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_DestroyProperties))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_DestroyProperties(SDL_PropertiesID props);
}
