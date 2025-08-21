using System.Runtime.InteropServices.Marshalling;

namespace Osm.Sage.UnsafeNativeImports.Sdl3.CustomMarshallers;

[CustomMarshaller(typeof(string), MarshalMode.ManagedToUnmanagedOut, typeof(ManagedToUnmanagedOut))]
internal static unsafe class Utf8UnownedStringMarshaller
{
    public ref struct ManagedToUnmanagedOut
    {
        private string? _managed;

        public void FromUnmanaged(byte* unmanaged)
        {
            if (unmanaged is null)
            {
                _managed = null;
                return;
            }

            _managed = Utf8StringMarshaller.ConvertToManaged(unmanaged);
        }

        public string? ToManaged() => _managed;

        public void Free()
        {
            // Nothing to do here.
        }
    }
}
