using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Osm.Sage.Compression.LightZhl.Exceptions;

[PublicAPI]
public class DecodingException : InvalidDataContractException
{
    public DecodingException(DecodingExceptionData data, string? message, Exception? inner = null)
        : base(
            $"{message} (stage={data.Stage}, srcIndex={data.SourceIndex}, nBits={data.BitCount}, bits=0x{data.BitBuffer:X8}, bufPos={data.BufferPosition}, lastGroup={data.LastGroup}, lastSymbol={data.LastSymbol})",
            inner
        ) { }
}
