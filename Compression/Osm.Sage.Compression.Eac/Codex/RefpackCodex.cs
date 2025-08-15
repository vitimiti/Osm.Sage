using System.Buffers.Binary;
using JetBrains.Annotations;
using Osm.Sage.Gimex;

namespace Osm.Sage.Compression.Eac.Codex;

[PublicAPI]
public class RefpackCodex : ICodex
{
    public CodexInformation About =>
        new()
        {
            Signature = new Signature("REF"),
            Capabilities = new CodexCapabilities
            {
                CanDecode = true,
                CanEncode = true,
                Supports32BitFields = true,
            },
            Version = new Version(1, 1),
            ShortType = "ref",
            LongType = "Refpack",
        };

    public bool QuickEncoding { get; set; }

    public bool IsValid(ReadOnlySpan<byte> compressedData) =>
        compressedData.Length switch
        {
            < 2 => false,
            _ => BinaryPrimitives.ReadUInt16BigEndian(compressedData)
                is 0x10FB
                    or 0x11FB
                    or 0x90FB
                    or 0x91FB,
        };

    public int ExtractSize(ReadOnlySpan<byte> compressedData)
    {
        if (!IsValid(compressedData))
        {
            throw new ArgumentException(
                $"The data is not a valid {nameof(RefpackCodex)}",
                nameof(compressedData)
            );
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(compressedData.Length, 2);

        var packType = BinaryPrimitives.ReadUInt16BigEndian(compressedData);
        var byteCount = (packType & 0x8000) != 0 ? 4 : 3;
        var offset = (packType & 0x0100) != 0 ? 2 + byteCount : 2;

        ArgumentOutOfRangeException.ThrowIfLessThan(compressedData.Length, offset + byteCount);

        // 3 or 4 bytes
        return byteCount switch
        {
            4 => (int)BinaryPrimitives.ReadUInt32BigEndian(compressedData[offset..]),
            _ => compressedData[offset] << 16
                | BinaryPrimitives.ReadUInt16BigEndian(compressedData[(offset + 1)..]),
        };
    }

    public ICollection<byte> Encode(ReadOnlySpan<byte> uncompressedData)
    {
        EncodingContext context = new()
        {
            Source = uncompressedData.IsEmpty ? [] : uncompressedData.ToArray(),
        };

        PopulateSize(ref context);

        context.LoopLength = (uncompressedData.IsEmpty ? 0 : context.Source.Length) - 4;
        context.CurrentIndex = 0;
        context.ReferenceIndex = 0;
        context.Run = 0;

        Array.Fill(context.HashTable, -1);

        TraverseFile(ref context);

        return context.Destination;
    }

    public ICollection<byte> Decode(ReadOnlySpan<byte> compressedData)
    {
        if (!IsValid(compressedData))
        {
            throw new ArgumentException(
                $"The data is not a valid {nameof(RefpackCodex)}",
                nameof(compressedData)
            );
        }

        DecodingContext context = new()
        {
            Source = compressedData.IsEmpty ? [] : compressedData.ToArray(),
        };

        PopulateDestinationSize(ref context);
        TraverseFile(ref context);

        return context.Destination;
    }

    #region Encoding Utilities

    struct EncodingContext()
    {
        public byte[] Source { get; init; }
        public List<byte> Destination { get; } = [];
        public int[] HashTable { get; } = new int[65536];
        public int[] Link { get; } = new int[131072];
        public int Run { get; set; }
        public int MaxBack { get; } = 131071;
        public int CurrentIndex { get; set; }
        public int ReferenceIndex { get; set; }
        public int LoopLength { get; set; }
        public int BinaryOffset { get; set; }
        public int BinaryLength { get; set; }
        public int BinaryCost { get; set; }
        public int MaxLength { get; set; }
        public int HashValue { get; set; }
        public int HashOffset { get; set; }
        public int MinimumHashOffset { get; set; }
    }

    private static int Hash(ReadOnlySpan<byte> data, int offset)
    {
        if (offset + 2 >= data.Length)
        {
            return 0;
        }

        return ((data[offset] << 8) | data[offset + 2]) ^ (data[offset + 1] << 4);
    }

    private static int MatchLen(
        byte[] source,
        int sourceOffset,
        byte[] dest,
        int destOffset,
        int maxMatch
    )
    {
        int current = 0;

        while (
            current < maxMatch
            && sourceOffset + current < source.Length
            && destOffset + current < dest.Length
            && source[sourceOffset + current] == dest[destOffset + current]
        )
        {
            current++;
        }

        return current;
    }

    private static void PopulateSize(ref EncodingContext context)
    {
        var header = (ushort)(context.Source.Length > 0xFFFFFF ? 0x90FB : 0x10FB);
        Span<byte> headerBytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(headerBytes, header);
        context.Destination.AddRange(headerBytes);

        Span<byte> size =
            context.Source.Length > 0xFFFFFF ? stackalloc byte[4] : stackalloc byte[3];

        if (context.Source.Length > 0xFFFFFF)
        {
            BinaryPrimitives.WriteUInt32BigEndian(size, (uint)context.Source.Length);
        }
        else
        {
            size[0] = (byte)(context.Source.Length >> 16);
            BinaryPrimitives.WriteUInt16BigEndian(size[1..], (ushort)context.Source.Length);
        }

        context.Destination.AddRange(size);
    }

    private static void InitializeTraversal(ref EncodingContext context)
    {
        context.BinaryOffset = 0;
        context.BinaryLength = 2;
        context.BinaryCost = 2;
        context.MaxLength = int.Min(context.LoopLength, 1028);
        context.HashValue = Hash(context.Source, context.CurrentIndex);
        context.HashOffset = context.HashTable[context.HashValue];
        context.MinimumHashOffset = int.Max(context.CurrentIndex - context.MaxBack, 0);
    }

    private static void ProcessLargeHashOffset(ref EncodingContext context)
    {
        if (context.HashOffset < context.MinimumHashOffset)
        {
            return;
        }

        do
        {
            if (
                context.CurrentIndex + context.BinaryLength >= context.Source.Length
                || context.HashOffset + context.BinaryLength >= context.Source.Length
                || context.Source[context.CurrentIndex + context.BinaryLength]
                    != context.Source[context.HashOffset + context.BinaryLength]
            )
            {
                continue;
            }

            var tempLength = MatchLen(
                context.Source,
                context.CurrentIndex,
                context.Source,
                context.HashOffset,
                context.MaxLength
            );

            if (tempLength <= context.BinaryLength)
            {
                continue;
            }

            int tempOffset = (context.CurrentIndex - 1) - context.HashOffset;

            int tempCost = tempOffset switch
            {
                < 1024 when tempLength <= 10 => 2, // 2-byte int form
                < 16384 when tempLength <= 67 => 3, // 3-byte int form
                _ => 4, // 4-byte very int form
            };

            if (tempLength - tempCost + 4 <= context.BinaryLength - context.BinaryCost + 4)
            {
                continue;
            }

            context.BinaryLength = tempLength;
            context.BinaryCost = tempCost;
            context.BinaryOffset = tempOffset;
            if (context.BinaryLength >= 1028)
            {
                break;
            }
        } while (
            (context.HashOffset = context.Link[context.HashOffset & 131071])
            >= context.MinimumHashOffset
        );
    }

    private static void ProcessLargeCost(ref EncodingContext context)
    {
        context.HashOffset = context.CurrentIndex;
        context.Link[context.HashOffset & 131071] = context.HashTable[context.HashValue];
        context.HashTable[context.HashValue] = context.HashOffset;

        context.Run++;
        context.CurrentIndex++;
        context.LoopLength--;
    }

    private static void ProcessCostRelatedLiteralData(ref EncodingContext context)
    {
        while (context.Run > 3) // literal block of data
        {
            var tempLength = int.Min(112, context.Run & ~3);
            context.Run -= tempLength;
            context.Destination.Add((byte)(0xe0 + (tempLength >> 2) - 1));

            for (int i = 0; i < tempLength; i++)
            {
                context.Destination.Add(context.Source[context.ReferenceIndex + i]);
            }

            context.ReferenceIndex += tempLength;
        }
    }

    private static void ProcessCostRelated2ByteIntForm(ref EncodingContext context)
    {
        context.Destination.Add(
            (byte)(
                ((context.BinaryOffset >> 8) << 5) + ((context.BinaryLength - 3) << 2) + context.Run
            )
        );

        context.Destination.Add((byte)context.BinaryOffset);
    }

    private static void ProcessCostRelated3ByteIntForm(ref EncodingContext context)
    {
        context.Destination.Add((byte)(0x80 + (context.BinaryLength - 4)));
        context.Destination.Add((byte)((context.Run << 6) + (context.BinaryOffset >> 8)));
        context.Destination.Add((byte)context.BinaryOffset);
    }

    private static void ProcessCostRelated4ByteVeryIntForm(ref EncodingContext context)
    {
        context.Destination.Add(
            (byte)(
                0xc0
                + ((context.BinaryOffset >> 16) << 4)
                + (((context.BinaryLength - 5) >> 8) << 2)
                + context.Run
            )
        );

        context.Destination.Add((byte)(context.BinaryOffset >> 8));
        context.Destination.Add((byte)(context.BinaryOffset));
        context.Destination.Add((byte)(context.BinaryLength - 5));
    }

    private static void ProcessCostRelatedFormData(ref EncodingContext context)
    {
        switch (context.BinaryCost)
        {
            // 2-byte int form
            case 2:
                ProcessCostRelated2ByteIntForm(ref context);
                break;
            // 3-byte int form
            case 3:
                ProcessCostRelated3ByteIntForm(ref context);
                break;
            // 4-byte very int form
            default:
                ProcessCostRelated4ByteVeryIntForm(ref context);
                break;
        }
    }

    private static void ProcessCostRelatedLiteralRun(ref EncodingContext context)
    {
        for (int i = 0; i < context.Run; i++)
        {
            context.Destination.Add(context.Source[context.ReferenceIndex + i]);
        }

        context.Run = 0;
    }

    private static void ProcessQuickEncoding(ref EncodingContext context)
    {
        context.HashOffset = context.CurrentIndex;
        context.Link[context.HashOffset & 131071] = context.HashTable[context.HashValue];
        context.HashTable[context.HashValue] = context.HashOffset;
    }

    private static void ProcessSlowEncoding(ref EncodingContext context)
    {
        for (int i = 0; i < context.BinaryLength; i++)
        {
            if (context.CurrentIndex + i >= context.Source.Length - 2)
            {
                continue;
            }

            context.HashValue = Hash(context.Source, context.CurrentIndex + i);
            context.HashOffset = context.CurrentIndex + i;
            context.Link[context.HashOffset & 131071] = context.HashTable[context.HashValue];
            context.HashTable[context.HashValue] = context.HashOffset;
        }
    }

    private static void TraverseSecondaryLoop(ref EncodingContext context)
    {
        context.LoopLength += 4;
        context.Run += context.LoopLength;

        while (context.Run > 3) // No match at the end, use literal
        {
            var tempLength = int.Min(112, context.Run & ~3);
            context.Run -= tempLength;
            context.Destination.Add((byte)(0xE0 + (tempLength >> 2) - 1));

            for (int i = 0; i < tempLength; i++)
            {
                context.Destination.Add(context.Source[context.ReferenceIndex + i]);
            }

            context.ReferenceIndex += tempLength;
        }
    }

    private static void ProcessEndOfFile(ref EncodingContext context)
    {
        context.Destination.Add((byte)(0xFC + context.Run)); // End-of-stream command + 0..3 literal

        if (context.Run <= 0)
        {
            return;
        }

        for (int i = 0; i < context.Run; i++)
        {
            context.Destination.Add(context.Source[context.ReferenceIndex + i]);
        }
    }

    private void TraverseMainLoop(ref EncodingContext context)
    {
        while (context.LoopLength >= 0)
        {
            InitializeTraversal(ref context);
            ProcessLargeHashOffset(ref context);
            if (context.BinaryCost >= context.BinaryLength || context.LoopLength < 4)
            {
                ProcessLargeCost(ref context);
            }
            else
            {
                ProcessCostRelatedLiteralData(ref context);
                ProcessCostRelatedFormData(ref context);
                if (context.Run > 0)
                {
                    ProcessCostRelatedLiteralRun(ref context);
                }

                if (QuickEncoding)
                {
                    ProcessQuickEncoding(ref context);
                }
                else
                {
                    ProcessSlowEncoding(ref context);
                }

                context.CurrentIndex += context.BinaryLength;
                context.ReferenceIndex = context.CurrentIndex;
                context.LoopLength -= context.BinaryLength;
            }
        }
    }

    private void TraverseFile(ref EncodingContext context)
    {
        TraverseMainLoop(ref context);
        TraverseSecondaryLoop(ref context);
        ProcessEndOfFile(ref context);
    }

    #endregion

    #region Decoding Utilities

    private struct DecodingContext()
    {
        public byte[] Source { get; init; }
        public int SourceIndex { get; set; }
        public List<byte> Destination { get; } = [];
        public byte First { get; set; }
        public byte Second { get; set; }
        public byte Third { get; set; }
        public byte Fourth { get; set; }
        public uint Run { get; set; }
    }

    private static void PopulateDestinationSize(ref DecodingContext context)
    {
        int unpackedLength;
        uint type = context.Source[context.SourceIndex++];
        type = (type << 8) + context.Source[context.SourceIndex++];

        if ((type & 0x8000) != 0)
        {
            if ((type & 0x100) != 0)
            {
                context.SourceIndex += 4;
            }

            unpackedLength = context.Source[context.SourceIndex++];
            unpackedLength = (unpackedLength << 8) + context.Source[context.SourceIndex++];
        }
        else
        {
            if ((type & 0x100) != 0)
            {
                context.SourceIndex += 3;
            }

            unpackedLength = context.Source[context.SourceIndex++];
        }

        unpackedLength = (unpackedLength << 8) + context.Source[context.SourceIndex++];
        unpackedLength = (unpackedLength << 8) + context.Source[context.SourceIndex++];

        context.Destination.Capacity = unpackedLength;
    }

    private static bool ProcessShortForm(ref DecodingContext context)
    {
        if ((context.First & 0x80) != 0)
        {
            return false;
        }

        context.Second = context.Source[context.SourceIndex++];
        context.Run = (uint)(context.First & 3);
        while (context.Run-- != 0)
        {
            context.Destination.Add(context.Source[context.SourceIndex++]);
        }

        var referenceOffset =
            context.Destination.Count - 1 - (((context.First & 0x60) << 3) + context.Second);

        context.Run = (uint)(((context.First & 0x1C) >> 2) + 3 - 1);

        for (var i = 0U; i <= context.Run; i++)
        {
            context.Destination.Add(context.Destination[(int)(referenceOffset + i)]);
        }

        return true;
    }

    private static bool ProcessIntForm(ref DecodingContext context)
    {
        if ((context.First & 0x40) != 0)
        {
            return false;
        }

        context.Second = context.Source[context.SourceIndex++];
        context.Third = context.Source[context.SourceIndex++];
        context.Run = (uint)(context.Second >> 6);
        while (context.Run-- != 0)
        {
            context.Destination.Add(context.Source[context.SourceIndex++]);
        }

        var referenceOffset =
            context.Destination.Count - 1 - (((context.Second & 0x3F) << 8) + context.Third);

        context.Run = (uint)((context.First & 0x3F) + 4 - 1);

        for (var i = 0U; i <= context.Run; i++)
        {
            context.Destination.Add(context.Destination[(int)(referenceOffset + i)]);
        }

        return true;
    }

    private static bool ProcessVeryIntForm(ref DecodingContext context)
    {
        if ((context.First & 0x20) != 0)
        {
            return false;
        }

        context.Second = context.Source[context.SourceIndex++];
        context.Third = context.Source[context.SourceIndex++];
        context.Fourth = context.Source[context.SourceIndex++];
        context.Run = (uint)(context.First & 3);
        while (context.Run-- != 0)
        {
            context.Destination.Add(context.Source[context.SourceIndex++]);
        }

        var referenceOffset =
            context.Destination.Count
            - 1
            - (((context.First & 0x10) >> 4 << 16) + (context.Second << 8) + context.Third);

        context.Run = (uint)(((context.First & 0x0C) >> 2 << 8) + context.Fourth + 5 - 1);

        for (var i = 0U; i <= context.Run; i++)
        {
            context.Destination.Add(context.Destination[(int)(referenceOffset + i)]);
        }

        return true;
    }

    private static bool ProcessLiteral(ref DecodingContext context)
    {
        context.Run = (uint)(((context.First & 0x1F) << 2) + 4);
        if (context.Run > 112)
        {
            return false;
        }

        while (context.Run-- != 0)
        {
            context.Destination.Add(context.Source[context.SourceIndex++]);
        }

        return true;
    }

    private static void ProcessEofLiteral(ref DecodingContext context)
    {
        context.Run = (uint)(context.First & 3);
        while (context.Run-- != 0)
        {
            context.Destination.Add(context.Source[context.SourceIndex++]);
        }
    }

    private static void TraverseFile(ref DecodingContext context)
    {
        while (true)
        {
            context.First = context.Source[context.SourceIndex++];
            if (ProcessShortForm(ref context))
            {
                continue;
            }

            if (ProcessIntForm(ref context))
            {
                continue;
            }

            if (ProcessVeryIntForm(ref context))
            {
                continue;
            }

            if (ProcessLiteral(ref context))
            {
                continue;
            }

            ProcessEofLiteral(ref context);
            break;
        }
    }

    #endregion
}
