using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Osm.Sage.UnsafeNativeImports.Sdl3;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "SDL naming conventions.")]
public static unsafe partial class SDL3
{
    public enum SDL_LogCategory;

    public static SDL_LogCategory SDL_LOG_CATEGORY_APPLICATION => 0;
    public static SDL_LogCategory SDL_LOG_CATEGORY_ERROR => (SDL_LogCategory)1;
    public static SDL_LogCategory SDL_LOG_CATEGORY_ASSERT => (SDL_LogCategory)2;
    public static SDL_LogCategory SDL_LOG_CATEGORY_SYSTEM => (SDL_LogCategory)3;
    public static SDL_LogCategory SDL_LOG_CATEGORY_AUDIO => (SDL_LogCategory)4;
    public static SDL_LogCategory SDL_LOG_CATEGORY_VIDEO => (SDL_LogCategory)5;
    public static SDL_LogCategory SDL_LOG_CATEGORY_RENDER => (SDL_LogCategory)6;
    public static SDL_LogCategory SDL_LOG_CATEGORY_INPUT => (SDL_LogCategory)7;
    public static SDL_LogCategory SDL_LOG_CATEGORY_TEST => (SDL_LogCategory)8;
    public static SDL_LogCategory SDL_LOG_CATEGORY_GPU => (SDL_LogCategory)9;
    public static SDL_LogCategory SDL_LOG_CATEGORY_RESERVED2 => (SDL_LogCategory)10;
    public static SDL_LogCategory SDL_LOG_CATEGORY_RESERVED3 => (SDL_LogCategory)11;
    public static SDL_LogCategory SDL_LOG_CATEGORY_RESERVED4 => (SDL_LogCategory)12;
    public static SDL_LogCategory SDL_LOG_CATEGORY_RESERVED5 => (SDL_LogCategory)13;
    public static SDL_LogCategory SDL_LOG_CATEGORY_RESERVED6 => (SDL_LogCategory)14;
    public static SDL_LogCategory SDL_LOG_CATEGORY_RESERVED7 => (SDL_LogCategory)15;
    public static SDL_LogCategory SDL_LOG_CATEGORY_RESERVED8 => (SDL_LogCategory)16;
    public static SDL_LogCategory SDL_LOG_CATEGORY_RESERVED9 => (SDL_LogCategory)17;
    public static SDL_LogCategory SDL_LOG_CATEGORY_RESERVED10 => (SDL_LogCategory)18;
    public static SDL_LogCategory SDL_LOG_CATEGORY_CUSTOM => (SDL_LogCategory)19;

    public enum SDL_LogPriority;

    public static SDL_LogPriority SDL_LOG_PRIORITY_INVALID => 0;
    public static SDL_LogPriority SDL_LOG_PRIORITY_TRACE => (SDL_LogPriority)1;
    public static SDL_LogPriority SDL_LOG_PRIORITY_VERBOSE => (SDL_LogPriority)2;
    public static SDL_LogPriority SDL_LOG_PRIORITY_DEBUG => (SDL_LogPriority)3;
    public static SDL_LogPriority SDL_LOG_PRIORITY_INFO => (SDL_LogPriority)4;
    public static SDL_LogPriority SDL_LOG_PRIORITY_WARN => (SDL_LogPriority)5;
    public static SDL_LogPriority SDL_LOG_PRIORITY_ERROR => (SDL_LogPriority)6;
    public static SDL_LogPriority SDL_LOG_PRIORITY_CRITICAL => (SDL_LogPriority)7;

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_SetLogPriorities))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_SetLogPriorities(SDL_LogPriority priority);

    public delegate void SDL_LogOutputFunction(
        int logCategory,
        SDL_LogPriority priority,
        string message
    );

    private static readonly delegate* unmanaged[Cdecl]<
        nint,
        int,
        SDL_LogPriority,
        byte*,
        void> SDL_LogOutputFunctionPtr = &SDL_LogOutputFunctionImpl;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void SDL_LogOutputFunctionImpl(
        nint userdata,
        int category,
        SDL_LogPriority priority,
        byte* message
    )
    {
        var callback = GCHandle.FromIntPtr(new IntPtr(userdata)).Target as SDL_LogOutputFunction;
        callback?.Invoke(category, priority, Utf8StringMarshaller.ConvertToManaged(message)!);
    }

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_SetLogOutputFunction))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void INTERNAL_SDL_SetLogOutputFunction(
        delegate* unmanaged[Cdecl]<nint, int, SDL_LogPriority, byte*, void> callback,
        nint userdata
    );

    public static void SDL_SetLogOutputFunction(SDL_LogOutputFunction callback)
    {
        var handle = GCHandle.Alloc(callback);
        try
        {
            INTERNAL_SDL_SetLogOutputFunction(SDL_LogOutputFunctionPtr, GCHandle.ToIntPtr(handle));
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }
}
