using System.Runtime.InteropServices.Marshalling;

namespace Osm.Sage.UnsafeNativeImports.Sdl3.CustomMarshallers;

[CustomMarshaller(typeof(string), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanagedIn))]
internal static unsafe class Utf8StdioCompatibleStringMarshaller
{
    public ref struct ManagedToUnmanagedIn
    {
        private Utf8StringMarshaller.ManagedToUnmanagedIn _marshaller;

        public byte* ToUnmanaged() => _marshaller.ToUnmanaged();

        public void FromManaged(string? managed)
        {
            if (managed is null)
            {
                return;
            }

            _marshaller = new Utf8StringMarshaller.ManagedToUnmanagedIn();

            // Ensure '%' is treated as literal by doubling it; allow the built-in marshaller
            // to allocate by passing the default scratch buffer (safe for null too).
            _marshaller.FromManaged(managed.Replace("%", "%%"), default);
        }

        public void Free() => _marshaller.Free();
    }
}
