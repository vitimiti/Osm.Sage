using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Osm.Sage.UnsafeNativeImports.Sdl3;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "SDL naming conventions.")]
public static unsafe partial class SDL3
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_Point
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_FPoint
    {
        public float x;
        public float y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_Rect
    {
        public int x;
        public int y;
        public int w;
        public int h;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_FRect
    {
        public float x;
        public float y;
        public float w;
        public float h;
    }

    public static void SDL_RectToFRect(in SDL_Rect rect, out SDL_FRect frect)
    {
        frect = new SDL_FRect
        {
            x = rect.x,
            y = rect.y,
            w = rect.w,
            h = rect.h,
        };
    }

    public static bool SDL_PointInRect(in SDL_Point p, in SDL_Rect r) =>
        p.x >= r.x && p.x < (r.x + r.w) && p.y >= r.y && p.y < (r.y + r.h);

    public static bool SDL_RectEmpty(in SDL_Rect r) => r.w <= 0 || r.h <= 0;

    public static bool SDL_RectsEqual(in SDL_Rect a, in SDL_Rect b) =>
        a.x == b.x && a.y == b.y && a.w == b.w && a.h == b.h;

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_HasRectIntersection))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool SDL_HasRectIntersection(in SDL_Rect A, in SDL_Rect B);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_GetRectIntersection))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool SDL_GetRectIntersection(
        in SDL_Rect A,
        in SDL_Rect B,
        out SDL_Rect result
    );

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_GetRectUnion))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool SDL_GetRectUnion(in SDL_Rect A, in SDL_Rect B, out SDL_Rect result);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_GetRectEnclosingPoints))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    private static partial bool INTERNAL_SDL_GetRectEnclosingPoints(
        [In]
        [MarshalUsing(
            typeof(ArrayMarshaller<SDL_Point, SDL_Point>),
            CountElementName = nameof(count)
        )]
            SDL_Point[] points,
        int count,
        SDL_Rect* clip,
        out SDL_Rect result
    );

    public static bool SDL_GetRectEnclosingPoints(
        ReadOnlySpan<SDL_Point> points,
        SDL_Rect? clip,
        out SDL_Rect result
    )
    {
        SDL_Rect* clipPtr = null;
        if (clip is not null)
        {
            clipPtr = (SDL_Rect*)
                GCHandle.Alloc(clip, GCHandleType.Pinned).AddrOfPinnedObject().ToPointer();
        }

        try
        {
            return INTERNAL_SDL_GetRectEnclosingPoints(
                points.ToArray(),
                points.Length,
                clipPtr,
                out result
            );
        }
        finally
        {
            if (clipPtr is not null)
            {
                GCHandle.FromIntPtr(new IntPtr(clipPtr)).Free();
            }
        }
    }

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_GetRectAndLineIntersection))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool SDL_GetRectAndLineIntersection(
        in SDL_Rect rect,
        ref int X1,
        ref int Y1,
        ref int X2,
        ref int Y2
    );

    public static bool SDL_PointInRectFloat(in SDL_FPoint p, in SDL_FRect r) =>
        p.x >= r.x && p.x < (r.x + r.w) && p.y >= r.y && p.y < (r.y + r.h);

    public static bool SDL_RectEmptyFloat(in SDL_FRect r) => r.w <= 0 || r.h <= 0;

    public static bool SDL_RectsEqualEpsilon(in SDL_FRect a, in SDL_FRect b, float epsilon) =>
        float.Abs(a.x - b.x) <= epsilon
        && float.Abs(a.y - b.y) <= epsilon
        && float.Abs(a.w - b.w) <= epsilon
        && float.Abs(a.h - b.h) <= epsilon;

    public static bool SDL_RectsEqualFloat(in SDL_FRect a, SDL_FRect b) =>
        SDL_RectsEqualEpsilon(a, b, float.Epsilon);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_HasRectIntersectionFloat))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool SDL_HasRectIntersectionFloat(in SDL_FRect A, in SDL_FRect B);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_GetRectIntersectionFloat))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool SDL_GetRectIntersectionFloat(
        in SDL_FRect A,
        in SDL_FRect B,
        out SDL_FRect result
    );

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_GetRectUnionFloat))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool SDL_GetRectUnionFloat(
        in SDL_FRect A,
        in SDL_FRect B,
        out SDL_FRect result
    );

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_GetRectEnclosingPointsFloat))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    private static partial bool INTERNAL_SDL_GetRectEnclosingPointsFloat(
        [In]
        [MarshalUsing(
            typeof(ArrayMarshaller<SDL_FPoint, SDL_FPoint>),
            CountElementName = nameof(count)
        )]
            SDL_FPoint[] points,
        int count,
        SDL_FRect* clip,
        out SDL_FRect result
    );

    public static bool SDL_GetRectEnclosingPointsFloat(
        ReadOnlySpan<SDL_FPoint> points,
        SDL_FRect? clip,
        out SDL_FRect result
    )
    {
        SDL_FRect* clipPtr = null;
        if (clip is not null)
        {
            clipPtr = (SDL_FRect*)
                GCHandle.Alloc(clip, GCHandleType.Pinned).AddrOfPinnedObject().ToPointer();
        }

        try
        {
            return INTERNAL_SDL_GetRectEnclosingPointsFloat(
                points.ToArray(),
                points.Length,
                clipPtr,
                out result
            );
        }
        finally
        {
            if (clipPtr is not null)
            {
                GCHandle.FromIntPtr(new IntPtr(clipPtr)).Free();
            }
        }
    }

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_GetRectAndLineIntersectionFloat))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial bool SDL_GetRectAndLineIntersectionFloat(
        in SDL_FRect rect,
        ref float X1,
        ref float Y1,
        ref float X2,
        ref float Y2
    );
}
