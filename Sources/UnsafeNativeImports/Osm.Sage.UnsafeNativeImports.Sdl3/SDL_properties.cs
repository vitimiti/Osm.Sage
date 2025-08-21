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
    /// <summary>
    /// Represents a record struct for SDL Property Identifiers.
    /// </summary>
    /// <remarks>
    /// The SDL_PropertiesID structure is used to encapsulate an unsigned integer value
    /// that uniquely identifies SDL properties within the context of SDL operations.
    /// This struct follows SDL naming conventions and may integrate with SDL3.
    /// </remarks>
    /// <param name="Value">
    /// The unsigned integer value that represents the identifier for an SDL property.
    /// </param>
    [PublicAPI]
    public record struct SDL_PropertiesID(uint Value)
    {
        /// <summary>
        /// Represents an invalid identifier for SDL properties.
        /// </summary>
        /// <remarks>
        /// The <c>Invalid</c> property serves as a default or uninitialized state
        /// for an <c>SDL_PropertiesID</c>. It holds a value of zero, signifying
        /// the absence of a valid SDL property identifier.
        /// </remarks>
        public static SDL_PropertiesID Invalid => new(0);
    }

    /// <summary>
    /// Defines the various types of SDL properties.
    /// </summary>
    /// <remarks>
    /// The SDL_PropertyType enumeration categorizes the types of properties
    /// that can be associated with SDL structures or objects.
    /// These property types are used to specify the expected data type
    /// for the property values and facilitate their interpretation within SDL operations.
    /// </remarks>
    public enum SDL_PropertyType;

    /// <summary>
    /// Represents an invalid or uninitialized SDL property type.
    /// </summary>
    /// <remarks>
    /// SDL_PROPERTY_TYPE_INVALID is a constant used to signify that a property type
    /// is invalid or not properly initialized. This value can be used as a placeholder
    /// to indicate the absence of a valid property type or to detect errors when processing
    /// SDL property types.
    /// </remarks>
    public static SDL_PropertyType SDL_PROPERTY_TYPE_INVALID => 0;

    /// <summary>
    /// Represents a pointer type property in the SDL property system.
    /// </summary>
    /// <remarks>
    /// SDL_PROPERTY_TYPE_POINTER signifies that the property value is a pointer.
    /// This type is used when the property data is expected to reference memory
    /// or an object, allowing for dynamic associations and advanced operations
    /// within the SDL property system. Ensure proper handling to avoid memory
    /// mismanagement issues when working with this type.
    /// </remarks>
    public static SDL_PropertyType SDL_PROPERTY_TYPE_POINTER => (SDL_PropertyType)1;

    /// <summary>
    /// Represents a property type that stores string data in SDL.
    /// </summary>
    /// <remarks>
    /// SDL_PROPERTY_TYPE_STRING denotes an SDL property type designed to handle string values.
    /// This type is used when the property contains textual data and allows for proper handling
    /// and interpretation of string-based information within SDL operations.
    /// </remarks>
    public static SDL_PropertyType SDL_PROPERTY_TYPE_STRING => (SDL_PropertyType)2;

    /// <summary>
    /// Represents the numeric SDL property type.
    /// </summary>
    /// <remarks>
    /// SDL_PROPERTY_TYPE_NUMBER is used to indicate that the associated property expects
    /// a numeric value. This type can be used in SDL operations where integer-based data
    /// representation is required, allowing consistent handling and interpretation of numeric properties.
    /// </remarks>
    public static SDL_PropertyType SDL_PROPERTY_TYPE_NUMBER => (SDL_PropertyType)3;

    /// <summary>
    /// Represents an SDL property type for floating-point numeric values.
    /// </summary>
    /// <remarks>
    /// SDL_PROPERTY_TYPE_FLOAT is a constant used to identify properties that store
    /// floating-point numbers. This property type is typically used in scenarios where
    /// precise decimal or real number representation is required within SDL operations.
    /// </remarks>
    public static SDL_PropertyType SDL_PROPERTY_TYPE_FLOAT => (SDL_PropertyType)4;

    /// <summary>
    /// Represents a boolean SDL property type.
    /// </summary>
    /// <remarks>
    /// SDL_PROPERTY_TYPE_BOOLEAN is a constant used to signify that the property is expected
    /// to hold a value of boolean type, typically representing true or false states.
    /// This property type is commonly used to express binary conditions or toggles
    /// in SDL operations.
    /// </remarks>
    public static SDL_PropertyType SDL_PROPERTY_TYPE_BOOLEAN => (SDL_PropertyType)5;

    /// <summary>
    /// Retrieves global properties of the SDL system.
    /// </summary>
    /// <returns>
    /// A value of type <c>SDL_PropertiesID</c>, representing the global properties of the current SDL context.
    /// </returns>
    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_GetGlobalProperties))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_PropertiesID SDL_GetGlobalProperties();

    /// <summary>
    /// Creates a new set of properties for the SDL system.
    /// </summary>
    /// <returns>
    /// A value of type <c>SDL_PropertiesID</c>, representing the identifier of the newly created properties set.
    /// </returns>
    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_CreateProperties))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_PropertiesID SDL_CreateProperties();

    /// <summary>
    /// Copies properties from one SDL properties identifier to another.
    /// </summary>
    /// <param name="src">The source properties identifier from which properties will be copied.</param>
    /// <param name="dst">The destination properties identifier to which properties will be copied.</param>
    /// <returns>
    /// A boolean value indicating whether the properties were copied successfully. Returns <c>true</c> if the copy operation succeeded; otherwise, <c>false</c>.
    /// </returns>
    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_CopyProperties))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_CopyProperties(SDL_PropertiesID src, SDL_PropertiesID dst);

    /// <summary>
    /// Locks the specified set of properties in the SDL system, ensuring exclusive access.
    /// </summary>
    /// <param name="props">The ID of the properties to be locked.</param>
    /// <returns>True if the properties are successfully locked; otherwise, false.</returns>
    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_LockProperties))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_LockProperties(SDL_PropertiesID props);

    /// <summary>
    /// Unlocks properties that were previously locked within the SDL system, restoring their availability for modification or access.
    /// </summary>
    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_UnlockProperties))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_UnlockProperties();

    /// <summary>
    /// Represents a delegate that defines a callback for cleaning up property values in the SDL system.
    /// </summary>
    /// <param name="value">
    /// A pointer to the property value that needs to be cleaned up. The implementation of this callback
    /// is responsible for handling the cleanup appropriately.
    /// </param>
    /// <remarks>
    /// This delegate is used in scenarios where properties in SDL require custom resource disposal or
    /// cleanup logic. It is designed to manage unmanaged resources or any other cleanup mechanisms.
    /// </remarks>
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

    /// <summary>
    /// Assigns a pointer property to the specified SDL properties with an associated cleanup callback.
    /// </summary>
    /// <param name="props">The SDL_PropertiesID to which the pointer property will be assigned.</param>
    /// <param name="name">The name of the property.</param>
    /// <param name="value">The pointer value to be assigned to the property.</param>
    /// <param name="cleanup">
    /// A callback delegate invoked when the property is cleaned up, allowing for custom resource management.
    /// </param>
    /// <returns>
    /// A boolean value indicating success (<c>true</c>) or failure (<c>false</c>) of the operation.
    /// </returns>
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

    /// <summary>
    /// Sets a pointer-type property within the specified SDL properties context.
    /// </summary>
    /// <param name="props">An identifier of type <c>SDL_PropertiesID</c> representing the context in which to set the property.</param>
    /// <param name="name">The name of the property to set.</param>
    /// <param name="value">The pointer value to assign to the property.</param>
    /// <returns>
    /// <c>true</c> if the property was successfully set; otherwise, <c>false</c>.
    /// </returns>
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

    /// <summary>
    /// Sets a string property for the specified SDL properties object.
    /// </summary>
    /// <param name="props">The SDL properties object to modify.</param>
    /// <param name="name">The name of the property to set.</param>
    /// <param name="value">The value to assign to the property. Pass <c>null</c> to clear the property.</param>
    /// <returns>
    /// A boolean value indicating whether the property was successfully set.
    /// </returns>
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

    /// <summary>
    /// Sets a numeric property for the specified SDL properties object.
    /// </summary>
    /// <param name="props">The SDL properties object where the property will be set.</param>
    /// <param name="name">The name of the property to set.</param>
    /// <param name="value">The numeric value to assign to the specified property.</param>
    /// <returns>
    /// A boolean value indicating whether the operation was successful.
    /// </returns>
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

    /// <summary>
    /// Sets a float property for a given SDL property ID and name.
    /// </summary>
    /// <param name="props">The SDL property ID for which the float property is to be set.</param>
    /// <param name="name">The name of the property to be set.</param>
    /// <param name="value">The float value to assign to the property.</param>
    /// <returns>
    /// A boolean indicating whether the property was successfully set.
    /// </returns>
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

    /// <summary>
    /// Sets a boolean property in the SDL properties context.
    /// </summary>
    /// <param name="props">
    /// The <c>SDL_PropertiesID</c> identifier representing the properties object to modify.
    /// </param>
    /// <param name="name">
    /// The name of the property to set.
    /// </param>
    /// <param name="value">
    /// The boolean value to assign to the specified property.
    /// </param>
    /// <returns>
    /// <c>true</c> if the property was successfully set; otherwise, <c>false</c>.
    /// </returns>
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

    /// <summary>
    /// Checks if a specific property exists within the given SDL properties context.
    /// </summary>
    /// <param name="props">The set of SDL properties to search within.</param>
    /// <param name="name">The name of the property to check for.</param>
    /// <returns>
    /// A boolean value indicating whether the specified property exists.
    /// </returns>
    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_HasProperty),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_HasProperty(SDL_PropertiesID props, string name);

    /// <summary>
    /// Retrieves the type of the specified property from the SDL system.
    /// </summary>
    /// <param name="props">
    /// The ID representing the property set to query.
    /// </param>
    /// <param name="name">
    /// The name of the property whose type is to be retrieved.
    /// </param>
    /// <returns>
    /// A value of type <c>SDL_PropertyType</c>, representing the type of the specified property.
    /// </returns>
    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_GetPropertyType),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_PropertyType SDL_GetPropertyType(SDL_PropertiesID props, string name);

    /// <summary>
    /// Retrieves a pointer property from the specified SDL properties object.
    /// </summary>
    /// <param name="props">An SDL properties object identifier from which the property value will be retrieved.</param>
    /// <param name="name">The name of the property to look up.</param>
    /// <param name="default_value">A default value to return if the property does not exist.</param>
    /// <returns>
    /// A pointer to the property value, or the specified default value if the property does not exist.
    /// </returns>
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

    /// <summary>
    /// Retrieves the value of a string property from the specified SDL properties context.
    /// </summary>
    /// <param name="props">The identifier of the SDL properties context.</param>
    /// <param name="name">The name of the property to retrieve.</param>
    /// <param name="default_value">The default value to return if the property does not exist.</param>
    /// <returns>
    /// The value of the specified property as a string if it exists; otherwise, the provided default value.
    /// </returns>
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

    /// <summary>
    /// Retrieves a numeric property value from the specified SDL properties object.
    /// </summary>
    /// <param name="props">The SDL properties object from which to retrieve the property.</param>
    /// <param name="name">The name of the property to retrieve.</param>
    /// <param name="default_value">The default value to return if the property is not found.</param>
    /// <returns>
    /// The numeric value of the specified property, or the default value if the property is not found.
    /// </returns>
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

    /// <summary>
    /// Retrieves the value of a float property from the specified SDL properties context.
    /// </summary>
    /// <param name="props">The SDL properties context from which the property will be retrieved.</param>
    /// <param name="name">The name of the property to retrieve.</param>
    /// <param name="default_value">The default value to return if the specified property does not exist.</param>
    /// <returns>
    /// The value of the float property if it exists; otherwise, the specified default value.
    /// </returns>
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

    /// <summary>
    /// Retrieves a boolean property value from the specified SDL properties.
    /// </summary>
    /// <param name="props">The SDL properties object from which the boolean property will be retrieved.</param>
    /// <param name="name">The name of the property to retrieve.</param>
    /// <param name="default_value">The default value to return if the specified property is not found.</param>
    /// <returns>
    /// A boolean value indicating the value of the requested property, or the specified default value if the property is not found.
    /// </returns>
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

    /// <summary>
    /// Clears a specific property from the given SDL properties set.
    /// </summary>
    /// <param name="props">The identifier of the SDL properties set.</param>
    /// <param name="name">The name of the property to clear.</param>
    /// <returns>
    /// A boolean value indicating whether the property was successfully cleared.
    /// </returns>
    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_ClearProperty),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_ClearProperty(SDL_PropertiesID props, string name);

    /// <summary>
    /// Represents a callback delegate used to enumerate through SDL properties.
    /// </summary>
    /// <remarks>
    /// The SDL_EnumeratePropertiesCallback delegate is utilized in the context of property enumeration,
    /// allowing operations to be performed on each property within a given SDL_PropertiesID instance.
    /// The callback provides both the property identifier and the associated property name to the user-defined logic.
    /// </remarks>
    /// <param name="props">
    /// The SDL_PropertiesID instance that uniquely identifies the properties being enumerated.
    /// </param>
    /// <param name="name">
    /// The name of the property being processed during the enumeration.
    /// </param>
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

    /// <summary>
    /// Enumerates all properties within the specified SDL properties context by invoking a callback for each property.
    /// </summary>
    /// <param name="props">The SDL properties context to enumerate.</param>
    /// <param name="callback">The callback to be invoked for each property, providing the property ID and name.</param>
    /// <returns>
    /// A boolean value indicating whether the enumeration succeeded.
    /// </returns>
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

    /// <summary>
    /// Destroys and releases resources associated with the provided SDL properties.
    /// </summary>
    /// <param name="props">
    /// A value of type <c>SDL_PropertiesID</c>, representing the properties to be destroyed.
    /// </param>
    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_DestroyProperties))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_DestroyProperties(SDL_PropertiesID props);
}
