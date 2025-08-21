using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Osm.Sage.UnsafeNativeImports.Sdl3;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "SDL naming conventions.")]
public static partial class SDL3
{
    public enum SDL_PowerState;

    public static SDL_PowerState SDL_POWERSTATE_ERROR => (SDL_PowerState)(-1);
    public static SDL_PowerState SDL_POWERSTATE_UNKNOWN => 0;
    public static SDL_PowerState SDL_POWERSTATE_ON_BATTERY => (SDL_PowerState)1;
    public static SDL_PowerState SDL_POWERSTATE_NO_BATTERY => (SDL_PowerState)2;
    public static SDL_PowerState SDL_POWERSTATE_CHARGING => (SDL_PowerState)3;
    public static SDL_PowerState SDL_POWERSTATE_CHARGED => (SDL_PowerState)4;

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_GetPowerInfo))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_PowerState SDL_GetPowerInfo(out int seconds, out int percent);
}
