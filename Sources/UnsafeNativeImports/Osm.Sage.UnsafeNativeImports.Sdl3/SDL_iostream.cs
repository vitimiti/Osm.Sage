using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Osm.Sage.UnsafeNativeImports.Sdl3.CustomMarshallers;

namespace Osm.Sage.UnsafeNativeImports.Sdl3;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "SDL naming conventions.")]
public static unsafe partial class SDL3
{
    public enum SDL_IOStatus;

    public static SDL_IOStatus SDL_IO_STATUS_READY => 0;
    public static SDL_IOStatus SDL_IO_STATUS_ERROR => (SDL_IOStatus)1;
    public static SDL_IOStatus SDL_IO_STATUS_EOF => (SDL_IOStatus)2;
    public static SDL_IOStatus SDL_IO_STATUS_NOT_READY => (SDL_IOStatus)3;
    public static SDL_IOStatus SDL_IO_STATUS_READONLY => (SDL_IOStatus)4;
    public static SDL_IOStatus SDL_IO_STATUS_WRITEONLY => (SDL_IOStatus)5;

    public enum SDL_IOWhence;

    public static SDL_IOWhence SDL_IO_SEEK_SET => 0;
    public static SDL_IOWhence SDL_IO_SEEK_CUR => (SDL_IOWhence)1;
    public static SDL_IOWhence SDL_IO_SEEK_END => (SDL_IOWhence)2;

    [NativeMarshalling(typeof(SafeHandleMarshaller<SDL_IOStream>))]
    public sealed class SDL_IOStream : SafeHandle
    {
        public override bool IsInvalid => handle == nint.Zero;

        public SDL_IOStream()
            : base(invalidHandleValue: nint.Zero, ownsHandle: true) => SetHandle(nint.Zero);

        protected override bool ReleaseHandle() => SDL_CloseIO(this);
    }

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_IOFromFile),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_IOStream SDL_IOFromFile(string file, string mode);

    public const string SDL_PROP_IOSTREAM_WINDOWS_HANDLE_POINTER = "SDL.iostream.windows.handle";
    public const string SDL_PROP_IOSTREAM_STDIO_FILE_POINTER = "SDL.iostream.stdio.file";
    public const string SDL_PROP_IOSTREAM_FILE_DESCRIPTOR_NUMBER = "SDL.iostream.file_descriptor";
    public const string SDL_PROP_IOSTREAM_ANDROID_AASSET_POINTER = "SDL.iostream.android.aasset";

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_IOFromMem))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial SDL_IOStream INTERNAL_SDL_IOFromMem(void* mem, CULong size);

    public static SDL_IOStream SDL_IOFromMem(Span<byte> mem)
    {
        fixed (byte* ptr = mem)
        {
            return INTERNAL_SDL_IOFromMem(ptr, new CULong((uint)mem.Length));
        }
    }

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_IOFromConstMem))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial SDL_IOStream INTERNAL_SDL_IOFromConstMem(void* mem, CULong size);

    public static SDL_IOStream SDL_IOFromConstMem(ReadOnlySpan<byte> mem)
    {
        fixed (byte* ptr = mem)
        {
            return INTERNAL_SDL_IOFromConstMem(ptr, new CULong((uint)mem.Length));
        }
    }

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_IOFromDynamicMem))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_IOStream SDL_IOFromDynamicMem();

    public const string SDL_PROP_IOSTREAM_DYNAMIC_MEMORY_POINTER = "SDL.iostream.dynamic.memory";
    public const string SDL_PROP_IOSTREAM_DYNAMIC_CHUNKSIZE_NUMBER =
        "SDL.iostream.dynamic.chunksize";

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_CloseIO))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool SDL_CloseIO(SDL_IOStream context);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_GetIOProperties))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_PropertiesID SDL_GetIOProperties(SDL_IOStream context);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_GetIOStatus))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_IOStatus SDL_GetIOStatus(SDL_IOStream context);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_GetIOSize))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial long SDL_GetIOSize(SDL_IOStream context);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_SeekIO))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial long SDL_SeekIO(SDL_IOStream context, long offset, SDL_IOWhence whence);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_TellIO))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial long SDL_TellIO(SDL_IOStream context);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ReadIO))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial CULong INTERNAL_SDL_ReadIO(SDL_IOStream context, void* ptr, CULong size);

    public static bool SDL_ReadIO(SDL_IOStream context, Span<byte> buffer)
    {
        fixed (byte* ptr = buffer)
        {
            return INTERNAL_SDL_ReadIO(context, ptr, new CULong((uint)buffer.Length)).Value > 0;
        }
    }

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_WriteIO))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial CULong INTERNAL_SDL_WriteIO(
        SDL_IOStream context,
        void* ptr,
        CULong size
    );

    public static bool SDL_WriteIO(SDL_IOStream context, ReadOnlySpan<byte> buffer)
    {
        fixed (byte* ptr = buffer)
        {
            return INTERNAL_SDL_WriteIO(context, ptr, new CULong((uint)buffer.Length)).Value
                == (uint)buffer.Length;
        }
    }

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_IOprintf),
        StringMarshalling = StringMarshalling.Custom,
        StringMarshallingCustomType = typeof(Utf8StdioCompatibleStringMarshaller)
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial CULong SDL_IOprintf(SDL_IOStream context, string text);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_FlushIO))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_FlushIO(SDL_IOStream context);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_LoadFile_IO))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void* INTERNAL_SDL_LoadFile_IO(
        SDL_IOStream src,
        out CULong datasize,
        [MarshalAs(UnmanagedType.U1)] bool closeio
    );

    public static byte[]? SDL_LoadFile_IO(SDL_IOStream src)
    {
        // Don't close it, use the `using` system from C#
        var data = INTERNAL_SDL_LoadFile_IO(src, out var datasize, closeio: false);
        if (data is null)
        {
            return null;
        }

        try
        {
            return new Span<byte>(data, (int)datasize.Value).ToArray();
        }
        finally
        {
            SDL_free(data);
        }
    }

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_LoadFile),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void* INTERNAL_SDL_LoadFile(string file, out CULong datasize);

    public static byte[]? SDL_LoadFile(string file)
    {
        var data = INTERNAL_SDL_LoadFile(file, out var datasize);
        if (data is null)
        {
            return null;
        }

        try
        {
            return new Span<byte>(data, (int)datasize.Value).ToArray();
        }
        finally
        {
            SDL_free(data);
        }
    }

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_SaveFile_IO))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool INTERNAL_SDL_SaveFile_IO(
        SDL_IOStream src,
        void* data,
        CULong datasize,
        [MarshalAs(UnmanagedType.U1)] bool closeio
    );

    public static bool SDL_SaveFile_IO(SDL_IOStream src, ReadOnlySpan<byte> data)
    {
        fixed (byte* ptr = data)
        {
            // Don't close it, use the `using` system from C#
            return INTERNAL_SDL_SaveFile_IO(
                src,
                ptr,
                new CULong((uint)data.Length),
                closeio: false
            );
        }
    }

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_SaveFile),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool INTERNAL_SDL_SaveFile(string file, void* data, CULong datasize);

    public static bool SDL_SaveFile(string file, ReadOnlySpan<byte> data)
    {
        fixed (byte* ptr = data)
        {
            return INTERNAL_SDL_SaveFile(file, ptr, new CULong((uint)data.Length));
        }
    }

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ReadU8))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ReadU8(SDL_IOStream src, out byte value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ReadS8))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ReadS8(SDL_IOStream src, out sbyte value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ReadU16LE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ReadU16LE(SDL_IOStream src, out ushort value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ReadS16LE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ReadS16LE(SDL_IOStream src, out short value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ReadU16BE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ReadU16BE(SDL_IOStream src, out ushort value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ReadS16BE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ReadS16BE(SDL_IOStream src, out short value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ReadU32LE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ReadU32LE(SDL_IOStream src, out uint value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ReadS32LE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ReadS32LE(SDL_IOStream src, out int value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ReadU32BE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ReadU32BE(SDL_IOStream src, out uint value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ReadS32BE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ReadS32BE(SDL_IOStream src, out int value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ReadU64LE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ReadU64LE(SDL_IOStream src, out ulong value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ReadS64LE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ReadS64LE(SDL_IOStream src, out long value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ReadU64BE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ReadU64BE(SDL_IOStream src, out ulong value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ReadS64BE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ReadS64BE(SDL_IOStream src, out long value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_WriteU8))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_WriteU8(SDL_IOStream dst, byte value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_WriteS8))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_WriteS8(SDL_IOStream dst, sbyte value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_WriteU16LE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_WriteU16LE(SDL_IOStream dst, ushort value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_WriteS16LE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_WriteS16LE(SDL_IOStream dst, short value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_WriteU16BE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_WriteU16BE(SDL_IOStream dst, ushort value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_WriteS16BE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_WriteS16BE(SDL_IOStream dst, short value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_WriteU32LE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_WriteU32LE(SDL_IOStream dst, uint value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_WriteS32LE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_WriteS32LE(SDL_IOStream dst, int value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_WriteU32BE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_WriteU32BE(SDL_IOStream dst, uint value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_WriteS32BE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_WriteS32BE(SDL_IOStream dst, int value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_WriteU64LE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_WriteU64LE(SDL_IOStream dst, ulong value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_WriteS64LE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_WriteS64LE(SDL_IOStream dst, long value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_WriteU64BE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_WriteU64BE(SDL_IOStream dst, ulong value);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_WriteS64BE))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_WriteS64BE(SDL_IOStream dst, long value);
}
