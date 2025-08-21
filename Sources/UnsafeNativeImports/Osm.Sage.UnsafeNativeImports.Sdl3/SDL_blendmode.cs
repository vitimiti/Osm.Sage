using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Osm.Sage.UnsafeNativeImports.Sdl3;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "SDL naming conventions.")]
public static partial class SDL3
{
    public record struct SDL_BlendMode(uint Value);

    public static SDL_BlendMode SDL_BLENDMODE_NONE => new(0x00000000U);
    public static SDL_BlendMode SDL_BLENDMODE_BLEND => new(0x00000001U);
    public static SDL_BlendMode SDL_BLENDMODE_BLEND_PREMULTIPLIED => new(0x00000010U);
    public static SDL_BlendMode SDL_BLENDMODE_ADD => new(0x00000002U);
    public static SDL_BlendMode SDL_BLENDMODE_ADD_PREMULTIPLIED => new(0x00000020U);
    public static SDL_BlendMode SDL_BLENDMODE_MOD => new(0x00000004U);
    public static SDL_BlendMode SDL_BLENDMODE_MUL => new(0x00000008U);
    public static SDL_BlendMode SDL_BLENDMODE_INVALID => new(0x7FFFFFFFU);

    public enum SDL_BlendOperation;

    public static SDL_BlendOperation SDL_BLENDOPERATION_ADD => (SDL_BlendOperation)0x1;
    public static SDL_BlendOperation SDL_BLENDOPERATION_SUBTRACT => (SDL_BlendOperation)0x2;
    public static SDL_BlendOperation SDL_BLENDOPERATION_REV_SUBTRACT => (SDL_BlendOperation)0x3;
    public static SDL_BlendOperation SDL_BLENDOPERATION_MINIMUM => (SDL_BlendOperation)0x4;
    public static SDL_BlendOperation SDL_BLENDOPERATION_MAXIMUM => (SDL_BlendOperation)0x5;

    public enum SDL_BlendFactor;

    public static SDL_BlendFactor SDL_BLENDFACTOR_ZERO => (SDL_BlendFactor)0x1;
    public static SDL_BlendFactor SDL_BLENDFACTOR_ONE => (SDL_BlendFactor)0x2;
    public static SDL_BlendFactor SDL_BLENDFACTOR_SRC_COLOR => (SDL_BlendFactor)0x3;
    public static SDL_BlendFactor SDL_BLENDFACTOR_ONE_MINUS_SRC_COLOR => (SDL_BlendFactor)0x4;
    public static SDL_BlendFactor SDL_BLENDFACTOR_SRC_ALPHA => (SDL_BlendFactor)0x5;
    public static SDL_BlendFactor SDL_BLENDFACTOR_ONE_MINUS_SRC_ALPHA => (SDL_BlendFactor)0x6;
    public static SDL_BlendFactor SDL_BLENDFACTOR_DST_COLOR => (SDL_BlendFactor)0x7;
    public static SDL_BlendFactor SDL_BLENDFACTOR_ONE_MINUS_DST_COLOR => (SDL_BlendFactor)0x8;
    public static SDL_BlendFactor SDL_BLENDFACTOR_DST_ALPHA => (SDL_BlendFactor)0x9;
    public static SDL_BlendFactor SDL_BLENDFACTOR_ONE_MINUS_DST_ALPHA => (SDL_BlendFactor)0xA;

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ComposeCustomBlendMode))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_BlendMode SDL_ComposeCustomBlendMode(
        SDL_BlendFactor srcColorFactor,
        SDL_BlendFactor dstColorFactor,
        SDL_BlendOperation colorOperation,
        SDL_BlendFactor srcAlphaFactor,
        SDL_BlendFactor dstAlphaFactor,
        SDL_BlendOperation alphaOperation
    );
}
