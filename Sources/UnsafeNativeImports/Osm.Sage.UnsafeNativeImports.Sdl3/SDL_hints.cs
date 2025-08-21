using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Osm.Sage.UnsafeNativeImports.Sdl3.CustomMarshallers;

namespace Osm.Sage.UnsafeNativeImports.Sdl3;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "SDL naming conventions.")]
public static unsafe partial class SDL3
{
    public const string SDL_HINT_ALLOW_ALT_TAB_WHILE_GRABBED = "SDL_ALLOW_ALT_TAB_WHILE_GRABBED";
    public const string SDL_HINT_ANDROID_ALLOW_RECREATE_ACTIVITY =
        "SDL_ANDROID_ALLOW_RECREATE_ACTIVITY";

    public const string SDL_HINT_ANDROID_BLOCK_ON_PAUSE = "SDL_ANDROID_BLOCK_ON_PAUSE";
    public const string SDL_HINT_ANDROID_LOW_LATENCY_AUDIO = "SDL_ANDROID_LOW_LATENCY_AUDIO";
    public const string SDL_HINT_ANDROID_TRAP_BACK_BUTTON = "SDL_ANDROID_TRAP_BACK_BUTTON";
    public const string SDL_HINT_APP_ID = "SDL_APP_ID";
    public const string SDL_HINT_APP_NAME = "SDL_APP_NAME";
    public const string SDL_HINT_APPLE_TV_CONTROLLER_UI_EVENTS =
        "SDL_APPLE_TV_CONTROLLER_UI_EVENTS";

    public const string SDL_HINT_APPLE_TV_REMOTE_ALLOW_ROTATION =
        "SDL_APPLE_TV_REMOTE_ALLOW_ROTATION";

    public const string SDL_HINT_AUDIO_ALSA_DEFAULT_DEVICE = "SDL_AUDIO_ALSA_DEFAULT_DEVICE";
    public const string SDL_HINT_AUDIO_ALSA_DEFAULT_PLAYBACK_DEVICE =
        "SDL_AUDIO_ALSA_DEFAULT_PLAYBACK_DEVICE";

    public const string SDL_HINT_AUDIO_ALSA_DEFAULT_RECORDING_DEVICE =
        "SDL_AUDIO_ALSA_DEFAULT_RECORDING_DEVICE";

    public const string SDL_HINT_AUDIO_CATEGORY = "SDL_AUDIO_CATEGORY";
    public const string SDL_HINT_AUDIO_CHANNELS = "SDL_AUDIO_CHANNELS";
    public const string SDL_HINT_AUDIO_DEVICE_APP_ICON_NAME = "SDL_AUDIO_DEVICE_APP_ICON_NAME";
    public const string SDL_HINT_AUDIO_DEVICE_SAMPLE_FRAMES = "SDL_AUDIO_DEVICE_SAMPLE_FRAMES";
    public const string SDL_HINT_AUDIO_DEVICE_STREAM_NAME = "SDL_AUDIO_DEVICE_STREAM_NAME";
    public const string SDL_HINT_AUDIO_DEVICE_STREAM_ROLE = "SDL_AUDIO_DEVICE_STREAM_ROLE";
    public const string SDL_HINT_AUDIO_DISK_INPUT_FILE = "SDL_AUDIO_DISK_INPUT_FILE";
    public const string SDL_HINT_AUDIO_DISK_OUTPUT_FILE = "SDL_AUDIO_DISK_OUTPUT_FILE";
    public const string SDL_HINT_AUDIO_DISK_TIMESCALE = "SDL_AUDIO_DISK_TIMESCALE";
    public const string SDL_HINT_AUDIO_DRIVER = "SDL_AUDIO_DRIVER";
    public const string SDL_HINT_AUDIO_DUMMY_TIMESCALE = "SDL_AUDIO_DUMMY_TIMESCALE";
    public const string SDL_HINT_AUDIO_FORMAT = "SDL_AUDIO_FORMAT";
    public const string SDL_HINT_AUDIO_FREQUENCY = "SDL_AUDIO_FREQUENCY";
    public const string SDL_HINT_AUDIO_INCLUDE_MONITORS = "SDL_AUDIO_INCLUDE_MONITORS";
    public const string SDL_HINT_AUTO_UPDATE_JOYSTICKS = "SDL_AUTO_UPDATE_JOYSTICKS";
    public const string SDL_HINT_AUTO_UPDATE_SENSORS = "SDL_AUTO_UPDATE_SENSORS";
    public const string SDL_HINT_BMP_SAVE_LEGACY_FORMAT = "SDL_BMP_SAVE_LEGACY_FORMAT";
    public const string SDL_HINT_CAMERA_DRIVER = "SDL_CAMERA_DRIVER";
    public const string SDL_HINT_CPU_FEATURE_MASK = "SDL_CPU_FEATURE_MASK";
    public const string SDL_HINT_JOYSTICK_DIRECTINPUT = "SDL_JOYSTICK_DIRECTINPUT";
    public const string SDL_HINT_FILE_DIALOG_DRIVER = "SDL_FILE_DIALOG_DRIVER";
    public const string SDL_HINT_DISPLAY_USABLE_BOUNDS = "SDL_DISPLAY_USABLE_BOUNDS";
    public const string SDL_HINT_EMSCRIPTEN_ASYNCIFY = "SDL_EMSCRIPTEN_ASYNCIFY";
    public const string SDL_HINT_EMSCRIPTEN_CANVAS_SELECTOR = "SDL_EMSCRIPTEN_CANVAS_SELECTOR";
    public const string SDL_HINT_EMSCRIPTEN_KEYBOARD_ELEMENT = "SDL_EMSCRIPTEN_KEYBOARD_ELEMENT";
    public const string SDL_HINT_ENABLE_SCREEN_KEYBOARD = "SDL_ENABLE_SCREEN_KEYBOARD";
    public const string SDL_HINT_EVDEV_DEVICES = "SDL_EVDEV_DEVICES";
    public const string SDL_HINT_EVENT_LOGGING = "SDL_EVENT_LOGGING";
    public const string SDL_HINT_FORCE_RAISEWINDOW = "SDL_FORCE_RAISEWINDOW";
    public const string SDL_HINT_FRAMEBUFFER_ACCELERATION = "SDL_FRAMEBUFFER_ACCELERATION";
    public const string SDL_HINT_GAMECONTROLLERCONFIG = "SDL_GAMECONTROLLERCONFIG";
    public const string SDL_HINT_GAMECONTROLLERCONFIG_FILE = "SDL_GAMECONTROLLERCONFIG_FILE";
    public const string SDL_HINT_GAMECONTROLLERTYPE = "SDL_GAMECONTROLLERTYPE";
    public const string SDL_HINT_GAMECONTROLLER_IGNORE_DEVICES =
        "SDL_GAMECONTROLLER_IGNORE_DEVICES";

    public const string SDL_HINT_GAMECONTROLLER_IGNORE_DEVICES_EXCEPT =
        "SDL_GAMECONTROLLER_IGNORE_DEVICES_EXCEPT";

    public const string SDL_HINT_GAMECONTROLLER_SENSOR_FUSION = "SDL_GAMECONTROLLER_SENSOR_FUSION";
    public const string SDL_HINT_GDK_TEXTINPUT_DEFAULT_TEXT = "SDL_GDK_TEXTINPUT_DEFAULT_TEXT";
    public const string SDL_HINT_GDK_TEXTINPUT_DESCRIPTION = "SDL_GDK_TEXTINPUT_DESCRIPTION";
    public const string SDL_HINT_GDK_TEXTINPUT_MAX_LENGTH = "SDL_GDK_TEXTINPUT_MAX_LENGTH";
    public const string SDL_HINT_GDK_TEXTINPUT_SCOPE = "SDL_GDK_TEXTINPUT_SCOPE";
    public const string SDL_HINT_GDK_TEXTINPUT_TITLE = "SDL_GDK_TEXTINPUT_TITLE";
    public const string SDL_HINT_HIDAPI_LIBUSB = "SDL_HIDAPI_LIBUSB";
    public const string SDL_HINT_HIDAPI_LIBUSB_WHITELIST = "SDL_HIDAPI_LIBUSB_WHITELIST";
    public const string SDL_HINT_HIDAPI_UDEV = "SDL_HIDAPI_UDEV";
    public const string SDL_HINT_GPU_DRIVER = "SDL_GPU_DRIVER";
    public const string SDL_HINT_HIDAPI_ENUMERATE_ONLY_CONTROLLERS =
        "SDL_HIDAPI_ENUMERATE_ONLY_CONTROLLERS";

    public const string SDL_HINT_HIDAPI_IGNORE_DEVICES = "SDL_HIDAPI_IGNORE_DEVICES";
    public const string SDL_HINT_IME_IMPLEMENTED_UI = "SDL_IME_IMPLEMENTED_UI";
    public const string SDL_HINT_IOS_HIDE_HOME_INDICATOR = "SDL_IOS_HIDE_HOME_INDICATOR";
    public const string SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS =
        "SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS";

    public const string SDL_HINT_JOYSTICK_ARCADESTICK_DEVICES = "SDL_JOYSTICK_ARCADESTICK_DEVICES";
    public const string SDL_HINT_JOYSTICK_ARCADESTICK_DEVICES_EXCLUDED =
        "SDL_JOYSTICK_ARCADESTICK_DEVICES_EXCLUDED";

    public const string SDL_HINT_JOYSTICK_BLACKLIST_DEVICES = "SDL_JOYSTICK_BLACKLIST_DEVICES";
    public const string SDL_HINT_JOYSTICK_BLACKLIST_DEVICES_EXCLUDED =
        "SDL_JOYSTICK_BLACKLIST_DEVICES_EXCLUDED";

    public const string SDL_HINT_JOYSTICK_DEVICE = "SDL_JOYSTICK_DEVICE";
    public const string SDL_HINT_JOYSTICK_ENHANCED_REPORTS = "SDL_JOYSTICK_ENHANCED_REPORTS";
    public const string SDL_HINT_JOYSTICK_FLIGHTSTICK_DEVICES = "SDL_JOYSTICK_FLIGHTSTICK_DEVICES";
    public const string SDL_HINT_JOYSTICK_FLIGHTSTICK_DEVICES_EXCLUDED =
        "SDL_JOYSTICK_FLIGHTSTICK_DEVICES_EXCLUDED";

    public const string SDL_HINT_JOYSTICK_GAMEINPUT = "SDL_JOYSTICK_GAMEINPUT";
    public const string SDL_HINT_JOYSTICK_GAMECUBE_DEVICES = "SDL_JOYSTICK_GAMECUBE_DEVICES";
    public const string SDL_HINT_JOYSTICK_GAMECUBE_DEVICES_EXCLUDED =
        "SDL_JOYSTICK_GAMECUBE_DEVICES_EXCLUDED";

    public const string SDL_HINT_JOYSTICK_HIDAPI = "SDL_JOYSTICK_HIDAPI";
    public const string SDL_HINT_JOYSTICK_HIDAPI_COMBINE_JOY_CONS =
        "SDL_JOYSTICK_HIDAPI_COMBINE_JOY_CONS";

    public const string SDL_HINT_JOYSTICK_HIDAPI_GAMECUBE = "SDL_JOYSTICK_HIDAPI_GAMECUBE";
    public const string SDL_HINT_JOYSTICK_HIDAPI_GAMECUBE_RUMBLE_BRAKE =
        "SDL_JOYSTICK_HIDAPI_GAMECUBE_RUMBLE_BRAKE";

    public const string SDL_HINT_JOYSTICK_HIDAPI_JOY_CONS = "SDL_JOYSTICK_HIDAPI_JOY_CONS";
    public const string SDL_HINT_JOYSTICK_HIDAPI_JOYCON_HOME_LED =
        "SDL_JOYSTICK_HIDAPI_JOYCON_HOME_LED";

    public const string SDL_HINT_JOYSTICK_HIDAPI_LUNA = "SDL_JOYSTICK_HIDAPI_LUNA";
    public const string SDL_HINT_JOYSTICK_HIDAPI_NINTENDO_CLASSIC =
        "SDL_JOYSTICK_HIDAPI_NINTENDO_CLASSIC";

    public const string SDL_HINT_JOYSTICK_HIDAPI_PS3 = "SDL_JOYSTICK_HIDAPI_PS3";
    public const string SDL_HINT_JOYSTICK_HIDAPI_PS3_SIXAXIS_DRIVER =
        "SDL_JOYSTICK_HIDAPI_PS3_SIXAXIS_DRIVER";

    public const string SDL_HINT_JOYSTICK_HIDAPI_PS4 = "SDL_JOYSTICK_HIDAPI_PS4";
    public const string SDL_HINT_JOYSTICK_HIDAPI_PS4_REPORT_INTERVAL =
        "SDL_JOYSTICK_HIDAPI_PS4_REPORT_INTERVAL";

    public const string SDL_HINT_JOYSTICK_HIDAPI_PS5 = "SDL_JOYSTICK_HIDAPI_PS5";
    public const string SDL_HINT_JOYSTICK_HIDAPI_PS5_PLAYER_LED =
        "SDL_JOYSTICK_HIDAPI_PS5_PLAYER_LED";

    public const string SDL_HINT_JOYSTICK_HIDAPI_SHIELD = "SDL_JOYSTICK_HIDAPI_SHIELD";
    public const string SDL_HINT_JOYSTICK_HIDAPI_STADIA = "SDL_JOYSTICK_HIDAPI_STADIA";
    public const string SDL_HINT_JOYSTICK_HIDAPI_STEAM = "SDL_JOYSTICK_HIDAPI_STEAM";
    public const string SDL_HINT_JOYSTICK_HIDAPI_STEAM_HOME_LED =
        "SDL_JOYSTICK_HIDAPI_STEAM_HOME_LED";

    public const string SDL_HINT_JOYSTICK_HIDAPI_STEAMDECK = "SDL_JOYSTICK_HIDAPI_STEAMDECK";
    public const string SDL_HINT_JOYSTICK_HIDAPI_STEAM_HORI = "SDL_JOYSTICK_HIDAPI_STEAM_HORI";
    public const string SDL_HINT_JOYSTICK_HIDAPI_SWITCH = "SDL_JOYSTICK_HIDAPI_SWITCH";
    public const string SDL_HINT_JOYSTICK_HIDAPI_SWITCH_HOME_LED =
        "SDL_JOYSTICK_HIDAPI_SWITCH_HOME_LED";

    public const string SDL_HINT_JOYSTICK_HIDAPI_SWITCH_PLAYER_LED =
        "SDL_JOYSTICK_HIDAPI_SWITCH_PLAYER_LED";

    public const string SDL_HINT_JOYSTICK_HIDAPI_VERTICAL_JOY_CONS =
        "SDL_JOYSTICK_HIDAPI_VERTICAL_JOY_CONS";

    public const string SDL_HINT_JOYSTICK_HIDAPI_WII = "SDL_JOYSTICK_HIDAPI_WII";
    public const string SDL_HINT_JOYSTICK_HIDAPI_WII_PLAYER_LED =
        "SDL_JOYSTICK_HIDAPI_WII_PLAYER_LED";

    public const string SDL_HINT_JOYSTICK_HIDAPI_XBOX = "SDL_JOYSTICK_HIDAPI_XBOX";
    public const string SDL_HINT_JOYSTICK_HIDAPI_XBOX_360 = "SDL_JOYSTICK_HIDAPI_XBOX_360";
    public const string SDL_HINT_JOYSTICK_HIDAPI_XBOX_360_PLAYER_LED =
        "SDL_JOYSTICK_HIDAPI_XBOX_360_PLAYER_LED";

    public const string SDL_HINT_JOYSTICK_HIDAPI_XBOX_360_WIRELESS =
        "SDL_JOYSTICK_HIDAPI_XBOX_360_WIRELESS";

    public const string SDL_HINT_JOYSTICK_HIDAPI_XBOX_ONE = "SDL_JOYSTICK_HIDAPI_XBOX_ONE";
    public const string SDL_HINT_JOYSTICK_HIDAPI_XBOX_ONE_HOME_LED =
        "SDL_JOYSTICK_HIDAPI_XBOX_ONE_HOME_LED";

    public const string SDL_HINT_JOYSTICK_IOKIT = "SDL_JOYSTICK_IOKIT";
    public const string SDL_HINT_JOYSTICK_LINUX_CLASSIC = "SDL_JOYSTICK_LINUX_CLASSIC";
    public const string SDL_HINT_JOYSTICK_LINUX_DEADZONES = "SDL_JOYSTICK_LINUX_DEADZONES";
    public const string SDL_HINT_JOYSTICK_LINUX_DIGITAL_HATS = "SDL_JOYSTICK_LINUX_DIGITAL_HATS";
    public const string SDL_HINT_JOYSTICK_LINUX_HAT_DEADZONES = "SDL_JOYSTICK_LINUX_HAT_DEADZONES";
    public const string SDL_HINT_JOYSTICK_MFI = "SDL_JOYSTICK_MFI";
    public const string SDL_HINT_JOYSTICK_RAWINPUT = "SDL_JOYSTICK_RAWINPUT";
    public const string SDL_HINT_JOYSTICK_RAWINPUT_CORRELATE_XINPUT =
        "SDL_JOYSTICK_RAWINPUT_CORRELATE_XINPUT";

    public const string SDL_HINT_JOYSTICK_ROG_CHAKRAM = "SDL_JOYSTICK_ROG_CHAKRAM";
    public const string SDL_HINT_JOYSTICK_THREAD = "SDL_JOYSTICK_THREAD";
    public const string SDL_HINT_JOYSTICK_THROTTLE_DEVICES = "SDL_JOYSTICK_THROTTLE_DEVICES";
    public const string SDL_HINT_JOYSTICK_THROTTLE_DEVICES_EXCLUDED =
        "SDL_JOYSTICK_THROTTLE_DEVICES_EXCLUDED";

    public const string SDL_HINT_JOYSTICK_WGI = "SDL_JOYSTICK_WGI";
    public const string SDL_HINT_JOYSTICK_WHEEL_DEVICES = "SDL_JOYSTICK_WHEEL_DEVICES";
    public const string SDL_HINT_JOYSTICK_WHEEL_DEVICES_EXCLUDED =
        "SDL_JOYSTICK_WHEEL_DEVICES_EXCLUDED";

    public const string SDL_HINT_JOYSTICK_ZERO_CENTERED_DEVICES =
        "SDL_JOYSTICK_ZERO_CENTERED_DEVICES";

    public const string SDL_HINT_JOYSTICK_HAPTIC_AXES = "SDL_JOYSTICK_HAPTIC_AXES";
    public const string SDL_HINT_KEYCODE_OPTIONS = "SDL_KEYCODE_OPTIONS";
    public const string SDL_HINT_KMSDRM_DEVICE_INDEX = "SDL_KMSDRM_DEVICE_INDEX";
    public const string SDL_HINT_KMSDRM_REQUIRE_DRM_MASTER = "SDL_KMSDRM_REQUIRE_DRM_MASTER";
    public const string SDL_HINT_LOGGING = "SDL_LOGGING";
    public const string SDL_HINT_MAC_BACKGROUND_APP = "SDL_MAC_BACKGROUND_APP";
    public const string SDL_HINT_MAC_CTRL_CLICK_EMULATE_RIGHT_CLICK =
        "SDL_MAC_CTRL_CLICK_EMULATE_RIGHT_CLICK";

    public const string SDL_HINT_MAC_OPENGL_ASYNC_DISPATCH = "SDL_MAC_OPENGL_ASYNC_DISPATCH";
    public const string SDL_HINT_MAC_OPTION_AS_ALT = "SDL_MAC_OPTION_AS_ALT";
    public const string SDL_HINT_MAC_SCROLL_MOMENTUM = "SDL_MAC_SCROLL_MOMENTUM";
    public const string SDL_HINT_MAIN_CALLBACK_RATE = "SDL_MAIN_CALLBACK_RATE";
    public const string SDL_HINT_MOUSE_AUTO_CAPTURE = "SDL_MOUSE_AUTO_CAPTURE";
    public const string SDL_HINT_MOUSE_DOUBLE_CLICK_RADIUS = "SDL_MOUSE_DOUBLE_CLICK_RADIUS";
    public const string SDL_HINT_MOUSE_DOUBLE_CLICK_TIME = "SDL_MOUSE_DOUBLE_CLICK_TIME";
    public const string SDL_HINT_MOUSE_DEFAULT_SYSTEM_CURSOR = "SDL_MOUSE_DEFAULT_SYSTEM_CURSOR";
    public const string SDL_HINT_MOUSE_EMULATE_WARP_WITH_RELATIVE =
        "SDL_MOUSE_EMULATE_WARP_WITH_RELATIVE";

    public const string SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH = "SDL_MOUSE_FOCUS_CLICKTHROUGH";
    public const string SDL_HINT_MOUSE_NORMAL_SPEED_SCALE = "SDL_MOUSE_NORMAL_SPEED_SCALE";
    public const string SDL_HINT_MOUSE_RELATIVE_MODE_CENTER = "SDL_MOUSE_RELATIVE_MODE_CENTER";
    public const string SDL_HINT_MOUSE_RELATIVE_SPEED_SCALE = "SDL_MOUSE_RELATIVE_SPEED_SCALE";
    public const string SDL_HINT_MOUSE_RELATIVE_SYSTEM_SCALE = "SDL_MOUSE_RELATIVE_SYSTEM_SCALE";
    public const string SDL_HINT_MOUSE_RELATIVE_WARP_MOTION = "SDL_MOUSE_RELATIVE_WARP_MOTION";
    public const string SDL_HINT_MOUSE_RELATIVE_CURSOR_VISIBLE =
        "SDL_MOUSE_RELATIVE_CURSOR_VISIBLE";

    public const string SDL_HINT_MOUSE_TOUCH_EVENTS = "SDL_MOUSE_TOUCH_EVENTS";
    public const string SDL_HINT_MUTE_CONSOLE_KEYBOARD = "SDL_MUTE_CONSOLE_KEYBOARD";
    public const string SDL_HINT_NO_SIGNAL_HANDLERS = "SDL_NO_SIGNAL_HANDLERS";
    public const string SDL_HINT_OPENGL_LIBRARY = "SDL_OPENGL_LIBRARY";
    public const string SDL_HINT_EGL_LIBRARY = "SDL_EGL_LIBRARY";
    public const string SDL_HINT_OPENGL_ES_DRIVER = "SDL_OPENGL_ES_DRIVER";
    public const string SDL_HINT_OPENVR_LIBRARY = "SDL_OPENVR_LIBRARY";
    public const string SDL_HINT_ORIENTATIONS = "SDL_ORIENTATIONS";
    public const string SDL_HINT_POLL_SENTINEL = "SDL_POLL_SENTINEL";
    public const string SDL_HINT_PREFERRED_LOCALES = "SDL_PREFERRED_LOCALES";
    public const string SDL_HINT_QUIT_ON_LAST_WINDOW_CLOSE = "SDL_QUIT_ON_LAST_WINDOW_CLOSE";
    public const string SDL_HINT_RENDER_DIRECT3D_THREADSAFE = "SDL_RENDER_DIRECT3D_THREADSAFE";
    public const string SDL_HINT_RENDER_DIRECT3D11_DEBUG = "SDL_RENDER_DIRECT3D11_DEBUG";
    public const string SDL_HINT_RENDER_VULKAN_DEBUG = "SDL_RENDER_VULKAN_DEBUG";
    public const string SDL_HINT_RENDER_GPU_DEBUG = "SDL_RENDER_GPU_DEBUG";
    public const string SDL_HINT_RENDER_GPU_LOW_POWER = "SDL_RENDER_GPU_LOW_POWER";
    public const string SDL_HINT_RENDER_DRIVER = "SDL_RENDER_DRIVER";
    public const string SDL_HINT_RENDER_LINE_METHOD = "SDL_RENDER_LINE_METHOD";
    public const string SDL_HINT_RENDER_METAL_PREFER_LOW_POWER_DEVICE =
        "SDL_RENDER_METAL_PREFER_LOW_POWER_DEVICE";

    public const string SDL_HINT_RENDER_VSYNC = "SDL_RENDER_VSYNC";
    public const string SDL_HINT_RETURN_KEY_HIDES_IME = "SDL_RETURN_KEY_HIDES_IME";
    public const string SDL_HINT_ROG_GAMEPAD_MICE = "SDL_ROG_GAMEPAD_MICE";
    public const string SDL_HINT_ROG_GAMEPAD_MICE_EXCLUDED = "SDL_ROG_GAMEPAD_MICE_EXCLUDED";
    public const string SDL_HINT_RPI_VIDEO_LAYER = "SDL_RPI_VIDEO_LAYER";
    public const string SDL_HINT_SCREENSAVER_INHIBIT_ACTIVITY_NAME =
        "SDL_SCREENSAVER_INHIBIT_ACTIVITY_NAME";

    public const string SDL_HINT_SHUTDOWN_DBUS_ON_QUIT = "SDL_SHUTDOWN_DBUS_ON_QUIT";
    public const string SDL_HINT_STORAGE_TITLE_DRIVER = "SDL_STORAGE_TITLE_DRIVER";
    public const string SDL_HINT_STORAGE_USER_DRIVER = "SDL_STORAGE_USER_DRIVER";
    public const string SDL_HINT_THREAD_FORCE_REALTIME_TIME_CRITICAL =
        "SDL_THREAD_FORCE_REALTIME_TIME_CRITICAL";

    public const string SDL_HINT_THREAD_PRIORITY_POLICY = "SDL_THREAD_PRIORITY_POLICY";
    public const string SDL_HINT_TIMER_RESOLUTION = "SDL_TIMER_RESOLUTION";
    public const string SDL_HINT_TOUCH_MOUSE_EVENTS = "SDL_TOUCH_MOUSE_EVENTS";
    public const string SDL_HINT_TRACKPAD_IS_TOUCH_ONLY = "SDL_TRACKPAD_IS_TOUCH_ONLY";
    public const string SDL_HINT_TV_REMOTE_AS_JOYSTICK = "SDL_TV_REMOTE_AS_JOYSTICK";
    public const string SDL_HINT_VIDEO_ALLOW_SCREENSAVER = "SDL_VIDEO_ALLOW_SCREENSAVER";
    public const string SDL_HINT_VIDEO_DISPLAY_PRIORITY = "SDL_VIDEO_DISPLAY_PRIORITY";
    public const string SDL_HINT_VIDEO_DOUBLE_BUFFER = "SDL_VIDEO_DOUBLE_BUFFER";
    public const string SDL_HINT_VIDEO_DRIVER = "SDL_VIDEO_DRIVER";
    public const string SDL_HINT_VIDEO_DUMMY_SAVE_FRAMES = "SDL_VIDEO_DUMMY_SAVE_FRAMES";
    public const string SDL_HINT_VIDEO_EGL_ALLOW_GETDISPLAY_FALLBACK =
        "SDL_VIDEO_EGL_ALLOW_GETDISPLAY_FALLBACK";

    public const string SDL_HINT_VIDEO_FORCE_EGL = "SDL_VIDEO_FORCE_EGL";
    public const string SDL_HINT_VIDEO_MAC_FULLSCREEN_SPACES = "SDL_VIDEO_MAC_FULLSCREEN_SPACES";
    public const string SDL_HINT_VIDEO_MAC_FULLSCREEN_MENU_VISIBILITY =
        "SDL_VIDEO_MAC_FULLSCREEN_MENU_VISIBILITY";

    public const string SDL_HINT_VIDEO_MINIMIZE_ON_FOCUS_LOSS = "SDL_VIDEO_MINIMIZE_ON_FOCUS_LOSS";
    public const string SDL_HINT_VIDEO_OFFSCREEN_SAVE_FRAMES = "SDL_VIDEO_OFFSCREEN_SAVE_FRAMES";
    public const string SDL_HINT_VIDEO_SYNC_WINDOW_OPERATIONS = "SDL_VIDEO_SYNC_WINDOW_OPERATIONS";
    public const string SDL_HINT_VIDEO_WAYLAND_ALLOW_LIBDECOR = "SDL_VIDEO_WAYLAND_ALLOW_LIBDECOR";
    public const string SDL_HINT_VIDEO_WAYLAND_MODE_EMULATION = "SDL_VIDEO_WAYLAND_MODE_EMULATION";
    public const string SDL_HINT_VIDEO_WAYLAND_MODE_SCALING = "SDL_VIDEO_WAYLAND_MODE_SCALING";
    public const string SDL_HINT_VIDEO_WAYLAND_PREFER_LIBDECOR =
        "SDL_VIDEO_WAYLAND_PREFER_LIBDECOR";

    public const string SDL_HINT_VIDEO_WAYLAND_SCALE_TO_DISPLAY =
        "SDL_VIDEO_WAYLAND_SCALE_TO_DISPLAY";

    public const string SDL_HINT_VIDEO_WIN_D3DCOMPILER = "SDL_VIDEO_WIN_D3DCOMPILER";
    public const string SDL_HINT_VIDEO_X11_EXTERNAL_WINDOW_INPUT =
        "SDL_VIDEO_X11_EXTERNAL_WINDOW_INPUT";

    public const string SDL_HINT_VIDEO_X11_NET_WM_BYPASS_COMPOSITOR =
        "SDL_VIDEO_X11_NET_WM_BYPASS_COMPOSITOR";

    public const string SDL_HINT_VIDEO_X11_NET_WM_PING = "SDL_VIDEO_X11_NET_WM_PING";
    public const string SDL_HINT_VIDEO_X11_NODIRECTCOLOR = "SDL_VIDEO_X11_NODIRECTCOLOR";
    public const string SDL_HINT_VIDEO_X11_SCALING_FACTOR = "SDL_VIDEO_X11_SCALING_FACTOR";
    public const string SDL_HINT_VIDEO_X11_VISUALID = "SDL_VIDEO_X11_VISUALID";
    public const string SDL_HINT_VIDEO_X11_WINDOW_VISUALID = "SDL_VIDEO_X11_WINDOW_VISUALID";
    public const string SDL_HINT_VIDEO_X11_XRANDR = "SDL_VIDEO_X11_XRANDR";
    public const string SDL_HINT_VITA_ENABLE_BACK_TOUCH = "SDL_VITA_ENABLE_BACK_TOUCH";
    public const string SDL_HINT_VITA_ENABLE_FRONT_TOUCH = "SDL_VITA_ENABLE_FRONT_TOUCH";
    public const string SDL_HINT_VITA_MODULE_PATH = "SDL_VITA_MODULE_PATH";
    public const string SDL_HINT_VITA_PVR_INIT = "SDL_VITA_PVR_INIT";
    public const string SDL_HINT_VITA_RESOLUTION = "SDL_VITA_RESOLUTION";
    public const string SDL_HINT_VITA_PVR_OPENGL = "SDL_VITA_PVR_OPENGL";
    public const string SDL_HINT_VITA_TOUCH_MOUSE_DEVICE = "SDL_VITA_TOUCH_MOUSE_DEVICE";
    public const string SDL_HINT_VULKAN_DISPLAY = "SDL_VULKAN_DISPLAY";
    public const string SDL_HINT_VULKAN_LIBRARY = "SDL_VULKAN_LIBRARY";
    public const string SDL_HINT_WAVE_FACT_CHUNK = "SDL_WAVE_FACT_CHUNK";
    public const string SDL_HINT_WAVE_CHUNK_LIMIT = "SDL_WAVE_CHUNK_LIMIT";
    public const string SDL_HINT_WAVE_RIFF_CHUNK_SIZE = "SDL_WAVE_RIFF_CHUNK_SIZE";
    public const string SDL_HINT_WAVE_TRUNCATION = "SDL_WAVE_TRUNCATION";
    public const string SDL_HINT_WINDOW_ACTIVATE_WHEN_RAISED = "SDL_WINDOW_ACTIVATE_WHEN_RAISED";
    public const string SDL_HINT_WINDOW_ACTIVATE_WHEN_SHOWN = "SDL_WINDOW_ACTIVATE_WHEN_SHOWN";
    public const string SDL_HINT_WINDOW_ALLOW_TOPMOST = "SDL_WINDOW_ALLOW_TOPMOST";
    public const string SDL_HINT_WINDOW_FRAME_USABLE_WHILE_CURSOR_HIDDEN =
        "SDL_WINDOW_FRAME_USABLE_WHILE_CURSOR_HIDDEN";

    public const string SDL_HINT_WINDOWS_CLOSE_ON_ALT_F4 = "SDL_WINDOWS_CLOSE_ON_ALT_F4";
    public const string SDL_HINT_WINDOWS_ENABLE_MENU_MNEMONICS =
        "SDL_WINDOWS_ENABLE_MENU_MNEMONICS";

    public const string SDL_HINT_WINDOWS_ENABLE_MESSAGELOOP = "SDL_WINDOWS_ENABLE_MESSAGELOOP";
    public const string SDL_HINT_WINDOWS_GAMEINPUT = "SDL_WINDOWS_GAMEINPUT";
    public const string SDL_HINT_WINDOWS_RAW_KEYBOARD = "SDL_WINDOWS_RAW_KEYBOARD";
    public const string SDL_HINT_WINDOWS_FORCE_SEMAPHORE_KERNEL =
        "SDL_WINDOWS_FORCE_SEMAPHORE_KERNEL";

    public const string SDL_HINT_WINDOWS_INTRESOURCE_ICON = "SDL_WINDOWS_INTRESOURCE_ICON";
    public const string SDL_HINT_WINDOWS_INTRESOURCE_ICON_SMALL =
        "SDL_WINDOWS_INTRESOURCE_ICON_SMALL";

    public const string SDL_HINT_WINDOWS_USE_D3D9EX = "SDL_WINDOWS_USE_D3D9EX";
    public const string SDL_HINT_WINDOWS_ERASE_BACKGROUND_MODE =
        "SDL_WINDOWS_ERASE_BACKGROUND_MODE";

    public const string SDL_HINT_X11_FORCE_OVERRIDE_REDIRECT = "SDL_X11_FORCE_OVERRIDE_REDIRECT";
    public const string SDL_HINT_X11_WINDOW_TYPE = "SDL_X11_WINDOW_TYPE";
    public const string SDL_HINT_X11_XCB_LIBRARY = "SDL_X11_XCB_LIBRARY";
    public const string SDL_HINT_XINPUT_ENABLED = "SDL_XINPUT_ENABLED";
    public const string SDL_HINT_ASSERT = "SDL_ASSERT";
    public const string SDL_HINT_PEN_MOUSE_EVENTS = "SDL_PEN_MOUSE_EVENTS";
    public const string SDL_HINT_PEN_TOUCH_EVENTS = "SDL_PEN_TOUCH_EVENTS";

    public enum SDL_HintPriority;

    public static SDL_HintPriority SDL_HINT_DEFAULT => 0;
    public static SDL_HintPriority SDL_HINT_NORMAL => (SDL_HintPriority)1;
    public static SDL_HintPriority SDL_HINT_OVERRIDE => (SDL_HintPriority)2;

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_SetHintWithPriority),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_SetHintWithPriority(
        string name,
        string? value,
        SDL_HintPriority priority
    );

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_SetHint),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_SetHint(string name, string? value);

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_ResetHint),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ResetHint(string name);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_ResetHints))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_ResetHints();

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_GetHint),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalUsing(typeof(Utf8UnownedStringMarshaller))]
    public static partial string? SDL_GetHint(string name);

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_GetHintBoolean),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_GetHintBoolean(
        string name,
        [MarshalAs(UnmanagedType.U1)] bool default_value
    );

    public delegate void SDL_HintCallback(string name, string? oldValue, string? newValue);

    private static delegate* unmanaged[Cdecl]<nint, byte*, byte*, byte*, void> SDL_HintCallbackPtr =
        &SDL_HintCallbackImpl;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void SDL_HintCallbackImpl(
        nint userdata,
        byte* name,
        byte* oldValue,
        byte* newValue
    )
    {
        var callback = GCHandle.FromIntPtr(userdata).Target as SDL_HintCallback;
        callback?.Invoke(
            Utf8StringMarshaller.ConvertToManaged(name)!,
            Utf8StringMarshaller.ConvertToManaged(oldValue),
            Utf8StringMarshaller.ConvertToManaged(newValue)
        );
    }

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_AddHintCallback),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool INTERNAL_SDL_AddHintCallback(
        string name,
        delegate* unmanaged[Cdecl]<nint, byte*, byte*, byte*, void> callback,
        nint userdata
    );

    public static bool SDL_AddHintCallback(string name, SDL_HintCallback callback)
    {
        var handle = GCHandle.Alloc(callback);
        try
        {
            return INTERNAL_SDL_AddHintCallback(
                name,
                SDL_HintCallbackPtr,
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
        EntryPoint = nameof(SDL_RemoveHintCallback),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void INTERNAL_SDL_RemoveHintCallback(
        string name,
        delegate* unmanaged[Cdecl]<nint, byte*, byte*, byte*, void> callback,
        nint userdata
    );

    public static void SDL_RemoveHintCallback(string name, SDL_HintCallback callback)
    {
        var handle = GCHandle.Alloc(callback);
        try
        {
            INTERNAL_SDL_RemoveHintCallback(name, SDL_HintCallbackPtr, GCHandle.ToIntPtr(handle));
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
