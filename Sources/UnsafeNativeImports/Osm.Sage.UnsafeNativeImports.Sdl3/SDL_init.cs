using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Osm.Sage.UnsafeNativeImports.Sdl3.CustomMarshallers;

namespace Osm.Sage.UnsafeNativeImports.Sdl3;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "SDL naming conventions.")]
public static partial class SDL3
{
    /// <summary>
    /// Represents initialization flags used with SDL functionalities.
    /// These flags are typically used to specify which subsystems of SDL should
    /// be initialized or manipulated during the application's runtime.
    /// </summary>
    /// <remarks>
    /// This type encapsulates a 32-bit unsigned integer value, allowing the use of
    /// bitwise operations to combine or compare multiple flags in an efficient way.
    /// </remarks>
    public record struct SDL_InitFlags(uint Value)
    {
        /// <summary>
        /// Performs a bitwise OR operation between two <see cref="SDL_InitFlags"/> instances.
        /// </summary>
        /// <param name="left">The first operand.</param>
        /// <param name="right">The second operand.</param>
        /// <returns>
        /// A new <see cref="SDL_InitFlags"/> instance whose value is the result of the bitwise OR operation.
        /// </returns>
        public static SDL_InitFlags operator |(SDL_InitFlags left, SDL_InitFlags right) =>
            new(left.Value | right.Value);

        /// <summary>
        /// Performs a bitwise AND operation between two <see cref="SDL_InitFlags"/> instances.
        /// </summary>
        /// <param name="left">The first operand.</param>
        /// <param name="right">The second operand.</param>
        /// <returns>
        /// A new <see cref="SDL_InitFlags"/> instance whose value is the result of the bitwise AND operation.
        /// </returns>
        public static SDL_InitFlags operator &(SDL_InitFlags left, SDL_InitFlags right) =>
            new(left.Value & right.Value);

        /// <summary>
        /// Performs a bitwise exclusive OR (XOR) operation between two <see cref="SDL_InitFlags"/> instances.
        /// </summary>
        /// <param name="left">The first operand.</param>
        /// <param name="right">The second operand.</param>
        /// <returns>
        /// A new <see cref="SDL_InitFlags"/> instance whose value is the result of the bitwise XOR operation.
        /// </returns>
        public static SDL_InitFlags operator ^(SDL_InitFlags left, SDL_InitFlags right) =>
            new(left.Value ^ right.Value);

        /// <summary>
        /// Performs a bitwise NOT operation on the specified <see cref="SDL_InitFlags"/> instance.
        /// </summary>
        /// <param name="value">The value to negate.</param>
        /// <returns>
        /// A new <see cref="SDL_InitFlags"/> instance whose value is the result of the bitwise NOT operation.
        /// </returns>
        public static SDL_InitFlags operator ~(SDL_InitFlags value) => new(~value.Value);
    }

    /// <summary>
    /// Represents the initialization flag for the SDL audio subsystem.
    /// </summary>
    /// <remarks>
    /// This flag can be used to initialize or query the state of the audio subsystem
    /// in an SDL-based application. By combining this flag with other initialization flags,
    /// developers can selectively activate specific SDL subsystems during runtime.
    /// </remarks>
    public static SDL_InitFlags SDL_INIT_AUDIO => new(0x00000010U);

    /// <summary>
    /// Represents the initialization flag for the SDL video subsystem.
    /// </summary>
    /// <remarks>
    /// This flag can be used to initialize or query the state of the video subsystem
    /// in an SDL-based application. It allows developers to activate functionalities
    /// related to rendering, window management, and other video-related features.
    /// </remarks>
    public static SDL_InitFlags SDL_INIT_VIDEO => new(0x00000020U);

    /// <summary>
    /// Represents the initialization flag for the SDL joystick subsystem.
    /// </summary>
    /// <remarks>
    /// This flag can be used to initialize or query the state of the joystick subsystem
    /// in an SDL-based application. By using this flag in combination with others,
    /// developers can customize the initialization of specific SDL subsystems as needed.
    /// </remarks>
    public static SDL_InitFlags SDL_INIT_JOYSTICK => new(0x00000200U);

    /// <summary>
    /// Represents the initialization flag for the SDL haptic subsystem.
    /// </summary>
    /// <remarks>
    /// This flag enables the initialization or control of the haptic (force feedback) subsystem
    /// in an SDL-based application. It can be combined with other initialization flags
    /// to selectively enable specific SDL subsystems during runtime.
    /// </remarks>
    public static SDL_InitFlags SDL_INIT_HAPTIC => new(0x00001000U);

    /// <summary>
    /// Represents the initialization flag for the SDL gamepad subsystem.
    /// </summary>
    /// <remarks>
    /// This flag is used to initialize or query the state of the gamepad subsystem
    /// in an SDL-based application. By incorporating this flag with other initialization flags,
    /// developers can configure specific functionalities related to gamepad support during runtime.
    /// </remarks>
    public static SDL_InitFlags SDL_INIT_GAMEPAD => new(0x00002000U);

    /// <summary>
    /// Represents the initialization flag for the SDL events subsystem.
    /// </summary>
    /// <remarks>
    /// This flag allows the application to manage and process events in the SDL environment.
    /// By including this flag during SDL initialization, developers can enable the handling of
    /// event-related functionalities such as input processing and event queuing.
    /// </remarks>
    public static SDL_InitFlags SDL_INIT_EVENTS => new(0x00004000U);

    /// <summary>
    /// Represents the initialization flag for the SDL sensor subsystem.
    /// </summary>
    /// <remarks>
    /// This flag can be used to initialize or query the state of the sensor subsystem
    /// in an SDL-based application. By combining this flag with other initialization flags,
    /// developers can selectively activate specific SDL subsystems during runtime.
    /// </remarks>
    public static SDL_InitFlags SDL_INIT_SENSOR => new(0x00008000U);

    /// <summary>
    /// Represents the initialization flag for the SDL camera subsystem.
    /// </summary>
    /// <remarks>
    /// This flag can be used to initialize or query the state of the camera subsystem
    /// in an SDL-based application. It is typically combined with other initialization flags
    /// to selectively activate subsystems required by the application during runtime.
    /// </remarks>
    public static SDL_InitFlags SDL_INIT_CAMERA => new(0x00010000U);

    /// <summary>
    /// Initializes the SDL library.
    /// </summary>
    /// <param name="flags">The initialization flags that specify which SDL subsystems to initialize.</param>
    /// <returns>
    /// A boolean value indicating whether the initialization was successful.
    /// </returns>
    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_Init))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_Init(SDL_InitFlags flags);

    /// <summary>
    /// Initializes specific SDL subsystems with the given initialization flags.
    /// </summary>
    /// <param name="flags">The initialization flags that specify which subsystems to initialize.</param>
    /// <returns>
    /// A boolean value indicating whether the initialization was successful.
    /// </returns>
    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_InitSubSystem))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_InitSubSystem(SDL_InitFlags flags);

    /// <summary>
    /// Shuts down a subsystem previously initialized with <see cref="SDL_Init"/> or <see cref="SDL_InitSubSystem"/>.
    /// </summary>
    /// <param name="flags">A bitmask of the subsystems to shut down, specified using <see cref="SDL_InitFlags"/>.</param>
    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_QuitSubSystem))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_QuitSubSystem(SDL_InitFlags flags);

    /// <summary>
    /// Checks which subsystems have been initialized using the given initialization flags.
    /// </summary>
    /// <param name="flags">The initialization flags to check, represented by <see cref="SDL_InitFlags"/>.</param>
    /// <returns>
    /// A <see cref="SDL_InitFlags"/> value indicating which subsystems corresponding to the provided flags have been successfully initialized.
    /// </returns>
    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_WasInit))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_InitFlags SDL_WasInit(SDL_InitFlags flags);

    /// <summary>
    /// Cleans up all initialized SDL subsystems and releases any resources allocated by SDL.
    /// </summary>
    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_Quit))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_Quit();

    /// <summary>
    /// Sets the metadata for the application, including name, version, and identifier.
    /// </summary>
    /// <param name="appname">The name of the application.</param>
    /// <param name="appversion">The version of the application.</param>
    /// <param name="appidentifier">The identifier of the application.</param>
    /// <returns>
    /// A boolean value indicating whether the metadata was successfully set.
    /// </returns>
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

    /// <summary>
    /// Sets an application metadata property with the specified name and value.
    /// </summary>
    /// <param name="name">The name of the metadata property to set.</param>
    /// <param name="value">The value to assign to the metadata property. If null, the property will be cleared.</param>
    /// <returns>
    /// A boolean value indicating whether the operation was successful.
    /// </returns>
    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_SetAppMetadataProperty),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_SetAppMetadataProperty(string name, string? value);

    /// <summary>
    /// Represents the property key for setting or retrieving the application name
    /// in SDL metadata.
    /// </summary>
    /// <remarks>
    /// This property key can be used with the <c>SDL_SetAppMetadataProperty</c> function
    /// to define or access the name of the application in the app metadata.
    /// It is part of the metadata properties supported by SDL for describing application details.
    /// </remarks>
    public const string SDL_PROP_APP_METADATA_NAME_STRING = "SDL.app.metadata.name";

    /// <summary>
    /// Represents the metadata property name for the application's version string in SDL.
    /// </summary>
    /// <remarks>
    /// This constant is used with SDL metadata functions such as <c>SDL_SetAppMetadataProperty</c>
    /// to specify or query the version string of an application. The version string typically
    /// provides versioning information about the application set by the developer.
    /// </remarks>
    public const string SDL_PROP_APP_METADATA_VERSION_STRING = "SDL.app.metadata.version";

    /// <summary>
    /// Represents the identifier metadata property for an SDL application.
    /// </summary>
    /// <remarks>
    /// This constant is used as the key for setting or querying the unique identifier
    /// of the application in the SDL metadata system. The identifier is typically
    /// a string that uniquely distinguishes the application, such as a bundle identifier
    /// or a package name.
    /// </remarks>
    public const string SDL_PROP_APP_METADATA_IDENTIFIER_STRING = "SDL.app.metadata.identifier";

    /// <summary>
    /// Represents the property key used to specify the creator of the application in application metadata.
    /// </summary>
    /// <remarks>
    /// This property key is utilized in conjunction with the `SDL_SetAppMetadataProperty` function to provide
    /// information about the creator of the application. It is typically used for descriptive or organizational
    /// purposes in metadata associated with the application.
    /// </remarks>
    public const string SDL_PROP_APP_METADATA_CREATOR_STRING = "SDL.app.metadata.creator";

    /// <summary>
    /// Represents the metadata property identifier for an application's copyright information.
    /// </summary>
    /// <remarks>
    /// This constant is used to specify or retrieve the copyright details associated
    /// with an application in the SDL environment. It can be utilized in conjunction
    /// with metadata functions such as setting or querying application properties.
    /// </remarks>
    public const string SDL_PROP_APP_METADATA_COPYRIGHT_STRING = "SDL.app.metadata.copyright";

    /// <summary>
    /// Represents the property name for application metadata URL in SDL.
    /// </summary>
    /// <remarks>
    /// This property can be used in conjunction with the SDL_SetAppMetadataProperty function
    /// to assign or retrieve the URL associated with an application. It is a predefined
    /// constant used to uniquely identify the metadata field for the application's URL.
    /// </remarks>
    public const string SDL_PROP_APP_METADATA_URL_STRING = "SDL.app.metadata.url";

    /// <summary>
    /// Represents the property name for specifying the type of application metadata in SDL.
    /// </summary>
    /// <remarks>
    /// This constant is used as a key when setting or retrieving the type information
    /// of an application's metadata through SDL's metadata management functions. It provides
    /// a standardized way to handle metadata properties related to the application type.
    /// </remarks>
    public const string SDL_PROP_APP_METADATA_TYPE_STRING = "SDL.app.metadata.type";

    /// <summary>
    /// Retrieves a metadata property of the application associated with the specified name.
    /// </summary>
    /// <param name="name">The name of the metadata property to retrieve.</param>
    /// <returns>
    /// A string representing the value of the metadata property if it exists; otherwise, null.
    /// </returns>
    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_GetAppMetadataProperty),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalUsing(typeof(Utf8UnownedStringMarshaller))]
    public static partial string? SDL_GetAppMetadataProperty(string name);
}
