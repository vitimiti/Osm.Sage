using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Osm.Sage.Compression.LightZhl.Exceptions;

/// <summary>
/// Represents an exception that occurs during the decoding process in the compression algorithm.
/// </summary>
/// <remarks>
/// This exception provides detailed context regarding the decoding failure, including the specific
/// stage of the process, bit-level details, and buffer positions. It is primarily used to propagate
/// errors discovered during decompression workflows.
/// </remarks>
[PublicAPI]
public class DecodingException : InvalidDataContractException
{
    public DecodingException(DecodingExceptionData data, string? message, Exception? inner = null)
        : base(
            $"{message} (stage={data.Stage}, srcIndex={data.SourceIndex}, nBits={data.BitCount}, bits=0x{data.BitBuffer:X8}, bufPos={data.BufferPosition}, lastGroup={data.LastGroup}, lastSymbol={data.LastSymbol})",
            inner
        ) { }
}
