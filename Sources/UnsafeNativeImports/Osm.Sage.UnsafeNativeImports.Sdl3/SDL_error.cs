using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Osm.Sage.UnsafeNativeImports.Sdl3.CustomMarshallers;

namespace Osm.Sage.UnsafeNativeImports.Sdl3;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "SDL naming conventions.")]
public static partial class SDL3
{
    /// <summary>
    /// Sets the SDL error message, which can be retrieved with the <see cref="SDL_GetError"/> function.
    /// </summary>
    /// <param name="message">A string containing the error message.</param>
    /// <returns>Always returns <c>false</c>.</returns>
    /// <seealso cref="SDL_ClearError"/>
    /// <seealso cref="SDL_GetError"/>
    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_SetError),
        StringMarshalling = StringMarshalling.Custom,
        StringMarshallingCustomType = typeof(Utf8StdioCompatibleStringMarshaller)
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_SetError(string message);

    /// <summary>
    /// Set an error indicating that memory allocation failed.
    /// </summary>
    /// <returns>Always <c>false</c>.</returns>
    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_OutOfMemory))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_OutOfMemory();

    /// <summary>
    /// Retrieve a message about the last error that occurred on the current thread.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is possible for multiple errors to occur before calling <see cref="SDL_GetError"/>.
    /// Only the last error is returned.
    /// </para>
    /// <para>
    /// The message is only applicable when an SDL function has signaled an error.
    /// You must check the return values of SDL function calls to determine when to appropriately call <see cref="SDL_GetError"/>.
    /// You should <b>not</b> use the results of <see cref="SDL_GetError"/> to decide if an error has occurred!
    /// Sometimes SDL will set an error string even when reporting success.
    /// </para>
    /// <para>
    /// SDL will <b>not</b> clear the error string for successful API calls.
    /// You <b>must</b> check return values for failure cases before you can assume the error string applies.
    /// </para>
    /// <para>
    /// Error strings are set per-thread, so an error set in a different thread will not interfere with the current thread's operation.
    /// </para>
    /// <para>
    /// The returned value is a thread-local string which will remain valid until the current thread's error string is changed.
    /// The caller should make a copy if the value is needed after the next SDL API call.
    /// </para>
    /// </remarks>
    /// <returns>
    /// A message with information about the specific error that occurred, or an empty string if there hasn't been an error message set
    /// since the last call to <see cref="SDL_ClearError"/>.
    /// </returns>
    /// <seealso cref="SDL_ClearError"/>
    /// <seealso cref="SDL_SetError"/>
    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_GetError),
        StringMarshalling = StringMarshalling.Custom,
        StringMarshallingCustomType = typeof(Utf8UnownedStringMarshaller)
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial string SDL_GetError();

    /// <summary>
    /// Clear any previous error message for this thread.
    /// </summary>
    /// <returns>Always <c>true</c>.</returns>
    /// <seealso cref="SDL_GetError"/>
    /// <seealso cref="SDL_SetError"/>
    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ClearError))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ClearError();
}
