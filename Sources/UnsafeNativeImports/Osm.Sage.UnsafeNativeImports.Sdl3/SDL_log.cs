using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Osm.Sage.UnsafeNativeImports.Sdl3;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "SDL naming conventions.")]
public static unsafe partial class SDL3
{
    /// <summary>
    /// The predefined log categories.
    /// </summary>
    /// <remarks>
    /// By default the application and gpu categories are enabled at the INFO
    /// level, the assert category is enabled at the WARN level, test is enabled at
    /// the VERBOSE level and all other categories are enabled at the ERROR level.
    /// </remarks>
    public enum SDL_LogCategory;

    /// <summary>
    /// Represents the predefined log category for application-specific logging in SDL.
    /// </summary>
    /// <remarks>
    /// Categories in SDL allow filtering and organization of log messages.
    /// The <c>SDL_LOG_CATEGORY_APPLICATION</c> is typically used for general application messages.
    /// By default, the application category is enabled at the INFO log priority level.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_APPLICATION => 0;

    /// <summary>
    /// Represents the predefined log category for error-specific logging in SDL.
    /// </summary>
    /// <remarks>
    /// The <c>SDL_LOG_CATEGORY_ERROR</c> is typically used for logging error messages.
    /// This category helps identify and separate error-related log entries from other log categories.
    /// By default, the error category is enabled at the ERROR log priority level.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_ERROR => (SDL_LogCategory)1;

    /// <summary>
    /// Represents the predefined log category for assert-related logging in SDL.
    /// </summary>
    /// <remarks>
    /// This category is specifically used for logging messages related to assertion failures.
    /// By default, the <c>SDL_LOG_CATEGORY_ASSERT</c> is enabled at the WARN log priority level,
    /// allowing it to highlight potential issues during application development.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_ASSERT => (SDL_LogCategory)2;

    /// <summary>
    /// Represents the predefined log category for system-related logging in SDL.
    /// </summary>
    /// <remarks>
    /// This log category is specifically used for system-level messages, such as
    /// those related to hardware or operating system interactions.
    /// By default, the <c>SDL_LOG_CATEGORY_SYSTEM</c> is enabled at the ERROR log priority level.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_SYSTEM => (SDL_LogCategory)3;

    /// <summary>
    /// Represents the predefined log category for audio-related logging in SDL.
    /// </summary>
    /// <remarks>
    /// Categories in SDL allow filtering and organization of log messages.
    /// The <c>SDL_LOG_CATEGORY_AUDIO</c> is typically used for messages related to the audio subsystem.
    /// By default, the audio category is enabled at the ERROR log priority level.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_AUDIO => (SDL_LogCategory)4;

    /// <summary>
    /// Represents the predefined log category for video-related logging in SDL.
    /// </summary>
    /// <remarks>
    /// This category is used to log information specifically related to video subsystems.
    /// By default, the <c>SDL_LOG_CATEGORY_VIDEO</c> is enabled at the ERROR log priority level.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_VIDEO => (SDL_LogCategory)5;

    /// <summary>
    /// Represents the predefined log category for render-related logging in SDL.
    /// </summary>
    /// <remarks>
    /// This category is used to log messages related to rendering operations within SDL.
    /// By default, the render category is enabled at the ERROR log priority level.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_RENDER => (SDL_LogCategory)6;

    /// <summary>
    /// Represents the predefined log category for input-related logging in SDL.
    /// </summary>
    /// <remarks>
    /// This category is used for logging messages related to input handling, such as
    /// keyboard, mouse, or other input device events.
    /// By default, the input category is enabled at the ERROR log priority level.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_INPUT => (SDL_LogCategory)7;

    /// <summary>
    /// Represents the predefined log category for test-specific logging in SDL.
    /// </summary>
    /// <remarks>
    /// The <c>SDL_LOG_CATEGORY_TEST</c> category is typically used for test-related log messages.
    /// By default, this category is enabled at the VERBOSE log priority level, providing detailed
    /// information primarily useful for debugging or testing purposes.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_TEST => (SDL_LogCategory)8;

    /// <summary>
    /// Represents the predefined log category for GPU-related logging in SDL.
    /// </summary>
    /// <remarks>
    /// This category is specifically used for logging messages related to GPU operations or activities.
    /// By default, the <c>SDL_LOG_CATEGORY_GPU</c> is enabled at the INFO log priority level.
    /// It helps in isolating and organizing log messages associated with GPU interactions.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_GPU => (SDL_LogCategory)9;

    /// <summary>
    /// Represents a reserved log category in SDL, identified as Reserved2.
    /// </summary>
    /// <remarks>
    /// Reserved log categories are predefined placeholders in SDL for potential future use or custom extensions.
    /// The <c>SDL_LOG_CATEGORY_RESERVED2</c> does not have a specific purpose by default and is not actively used by SDL's core functionality.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_RESERVED2 => (SDL_LogCategory)10;

    /// <summary>
    /// Represents a reserved log category in SDL, identified as Reserved2.
    /// </summary>
    /// <remarks>
    /// Reserved log categories are predefined placeholders in SDL for potential future use or custom extensions.
    /// The <c>SDL_LOG_CATEGORY_RESERVED3</c> does not have a specific purpose by default and is not actively used by SDL's core functionality.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_RESERVED3 => (SDL_LogCategory)11;

    /// <summary>
    /// Represents a reserved log category in SDL, identified as Reserved2.
    /// </summary>
    /// <remarks>
    /// Reserved log categories are predefined placeholders in SDL for potential future use or custom extensions.
    /// The <c>SDL_LOG_CATEGORY_RESERVED4</c> does not have a specific purpose by default and is not actively used by SDL's core functionality.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_RESERVED4 => (SDL_LogCategory)12;

    /// <summary>
    /// Represents a reserved log category in SDL, identified as Reserved2.
    /// </summary>
    /// <remarks>
    /// Reserved log categories are predefined placeholders in SDL for potential future use or custom extensions.
    /// The <c>SDL_LOG_CATEGORY_RESERVED5</c> does not have a specific purpose by default and is not actively used by SDL's core functionality.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_RESERVED5 => (SDL_LogCategory)13;

    /// <summary>
    /// Represents a reserved log category in SDL, identified as Reserved2.
    /// </summary>
    /// <remarks>
    /// Reserved log categories are predefined placeholders in SDL for potential future use or custom extensions.
    /// The <c>SDL_LOG_CATEGORY_RESERVED6</c> does not have a specific purpose by default and is not actively used by SDL's core functionality.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_RESERVED6 => (SDL_LogCategory)14;

    /// <summary>
    /// Represents a reserved log category in SDL, identified as Reserved2.
    /// </summary>
    /// <remarks>
    /// Reserved log categories are predefined placeholders in SDL for potential future use or custom extensions.
    /// The <c>SDL_LOG_CATEGORY_RESERVED7</c> does not have a specific purpose by default and is not actively used by SDL's core functionality.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_RESERVED7 => (SDL_LogCategory)15;

    /// <summary>
    /// Represents a reserved log category in SDL, identified as Reserved2.
    /// </summary>
    /// <remarks>
    /// Reserved log categories are predefined placeholders in SDL for potential future use or custom extensions.
    /// The <c>SDL_LOG_CATEGORY_RESERVED8</c> does not have a specific purpose by default and is not actively used by SDL's core functionality.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_RESERVED8 => (SDL_LogCategory)16;

    /// <summary>
    /// Represents a reserved log category in SDL, identified as Reserved2.
    /// </summary>
    /// <remarks>
    /// Reserved log categories are predefined placeholders in SDL for potential future use or custom extensions.
    /// The <c>SDL_LOG_CATEGORY_RESERVED9</c> does not have a specific purpose by default and is not actively used by SDL's core functionality.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_RESERVED9 => (SDL_LogCategory)17;

    /// <summary>
    /// Represents a reserved log category in SDL, identified as Reserved2.
    /// </summary>
    /// <remarks>
    /// Reserved log categories are predefined placeholders in SDL for potential future use or custom extensions.
    /// The <c>SDL_LOG_CATEGORY_RESERVED10</c> does not have a specific purpose by default and is not actively used by SDL's core functionality.
    /// </remarks>
    public static SDL_LogCategory SDL_LOG_CATEGORY_RESERVED10 => (SDL_LogCategory)18;

    /// <summary>
    /// Represents a custom log category defined by the user in SDL.
    /// </summary>
    /// <remarks>
    /// The <c>SDL_LOG_CATEGORY_CUSTOM</c> category is intended for user-defined logging purposes.
    /// It allows applications to define their own specific logging behaviors and separate those
    /// logs from predefined categories.
    /// By default, this category is enabled at the ERROR log priority level.
    /// </remarks>
    /// <example>
    /// <code>
    /// public enum MyLogCategory
    /// {
    ///     Category1 = SDL_LOG_CATEGORY_CUSTOM,
    ///     Category2,
    ///     Category3,
    /// }
    /// </code>
    /// </example>
    public static SDL_LogCategory SDL_LOG_CATEGORY_CUSTOM => (SDL_LogCategory)19;

    /// <summary>
    /// The predefined log priorities.
    /// </summary>
    /// <remarks>
    /// Represents the priorities used for logging in SDL. Each priority level
    /// serves to classify the severity or importance of log messages, ranging
    /// from TRACE (detailed debug information) to CRITICAL (critical errors
    /// requiring immediate attention). The priorities allow for customization and
    /// filtering of log output based on application needs.
    /// </remarks>
    public enum SDL_LogPriority;

    /// <summary>
    /// Represents an invalid or uninitialized log priority in SDL.
    /// </summary>
    /// <remarks>
    /// The <c>SDL_LOG_PRIORITY_INVALID</c> is a predefined constant used to indicate
    /// that a log priority is not valid or has not been set. It is the default value
    /// for uninitialized priorities and should not be used to log messages.
    /// </remarks>
    public static SDL_LogPriority SDL_LOG_PRIORITY_INVALID => 0;

    /// <summary>
    /// Represents the TRACE log priority level in SDL logging.
    /// </summary>
    /// <remarks>
    /// The <c>SDL_LOG_PRIORITY_TRACE</c> value is used for highly detailed and fine-grained logging.
    /// It is typically enabled to capture extensive debug or trace information, often used during
    /// development or troubleshooting. Logging at the TRACE priority level generates a large amount of data
    /// and is generally disabled in production environments for performance reasons.
    /// </remarks>
    public static SDL_LogPriority SDL_LOG_PRIORITY_TRACE => (SDL_LogPriority)1;

    /// <summary>
    /// Represents the verbose log priority level in SDL.
    /// </summary>
    /// <remarks>
    /// The <c>SDL_LOG_PRIORITY_VERBOSE</c> is the second-lowest log priority level, used for detailed logging information.
    /// It is typically utilized for messages that provide fine-grained diagnostic details to help with debugging.
    /// This log level is more detailed than <c>DEBUG</c> but less detailed than <c>TRACE</c>.
    /// </remarks>
    public static SDL_LogPriority SDL_LOG_PRIORITY_VERBOSE => (SDL_LogPriority)2;

    /// <summary>
    /// Represents the debug priority level for logging in SDL.
    /// </summary>
    /// <remarks>
    /// The <c>SDL_LOG_PRIORITY_DEBUG</c> is used to log messages intended for debugging purposes.
    /// Debug priority enables developers to capture detailed diagnostic information during
    /// development or troubleshooting. This level is typically more verbose than higher priority levels such as INFO or WARN.
    /// </remarks>
    public static SDL_LogPriority SDL_LOG_PRIORITY_DEBUG => (SDL_LogPriority)3;

    /// <summary>
    /// Represents the log priority level for informational messages in SDL.
    /// </summary>
    /// <remarks>
    /// The <c>SDL_LOG_PRIORITY_INFO</c> priority level is intended for general informational messages
    /// that are not error-related. This level allows the application to communicate details
    /// about its operation or state without indicating a problem or warning.
    /// </remarks>
    public static SDL_LogPriority SDL_LOG_PRIORITY_INFO => (SDL_LogPriority)4;

    /// <summary>
    /// Represents the warning log priority level in SDL logging system.
    /// </summary>
    /// <remarks>
    /// Log messages with the <c>SDL_LOG_PRIORITY_WARN</c> priority indicate the presence of
    /// non-critical issues that may require attention.
    /// This priority is typically used for warnings that don't prevent the program from functioning
    /// but may signify potential issues or unexpected behavior.
    /// </remarks>
    public static SDL_LogPriority SDL_LOG_PRIORITY_WARN => (SDL_LogPriority)5;

    /// <summary>
    /// Represents the log priority level for error messages in SDL.
    /// </summary>
    /// <remarks>
    /// The <c>SDL_LOG_PRIORITY_ERROR</c> is used to indicate critical issues or
    /// unexpected runtime errors that require immediate attention. Messages at this
    /// priority level typically signify a failure in program operations or logic.
    /// This log priority provides a mechanism for isolating and addressing errors in the application.
    /// </remarks>
    public static SDL_LogPriority SDL_LOG_PRIORITY_ERROR => (SDL_LogPriority)6;

    /// <summary>
    /// Represents the predefined log priority level for critical messages in SDL.
    /// </summary>
    /// <remarks>
    /// The <c>SDL_LOG_PRIORITY_CRITICAL</c> is used for logging messages that indicate
    /// severe issues requiring immediate attention, such as fatal errors that may
    /// cause the application to terminate. It is the highest level of severity in SDL log priorities.
    /// </remarks>
    public static SDL_LogPriority SDL_LOG_PRIORITY_CRITICAL => (SDL_LogPriority)7;

    /// <summary>
    /// Set the priority of all log categories.
    /// </summary>
    /// <param name="priority">The <see cref="SDL_LogPriority"/> to assign.</param>
    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_SetLogPriorities))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_SetLogPriorities(SDL_LogPriority priority);

    /// <summary>
    /// The prototype for the log output callback function.
    /// </summary>
    /// <param name="logCategory">The category of the message.</param>
    /// <param name="priority">The priority of the message.</param>
    /// <param name="message">The message being output.</param>
    /// <remarks>
    /// This function is called by SDL when there is new text to be logged. A mutex
    /// is held so that this function is never called by more than one thread at
    /// once.
    /// </remarks>
    public delegate void SDL_LogOutputFunction(
        int logCategory,
        SDL_LogPriority priority,
        string message
    );

    private static delegate* unmanaged[Cdecl]<
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

    /// <summary>
    /// Sets a function to handle log output.
    /// </summary>
    /// <param name="callback">The callback function to handle log messages. This function is invoked with the log category, priority, and message.</param>
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
