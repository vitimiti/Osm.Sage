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

    public bool IsValid(ReadOnlySpan<byte> compressedData) =>
        compressedData.Length switch
        {
            < 2 => false,
            _ => compressedData.GetBigEndianValue(2) is 0x10FB or 0x11FB or 0x90FB or 0x91FB,
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

        var packType = compressedData.GetBigEndianValue(2);
        var byteCount = (packType & 0x8000) != 0 ? 4 : 3;
        var offset = (packType & 0x0100) != 0 ? 2 + byteCount : 2;

        ArgumentOutOfRangeException.ThrowIfLessThan(compressedData.Length, offset + byteCount);

        return (int)compressedData[offset..].GetBigEndianValue(byteCount);
    }

    public ICollection<byte> Encode(ReadOnlySpan<byte> uncompressedData)
    {
        if (uncompressedData.IsEmpty)
        {
            throw new ArgumentException("The data to encode is empty", nameof(uncompressedData));
        }

        var context = new EncodeContext { Source = uncompressedData.ToArray() };

        WriteHeader(ref context);
        CompressData(ref context);

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

        try
        {
            var context = new DecodeContext
            {
                Source = compressedData.ToArray(),
                ExpectedOutputSize = ExtractSize(compressedData),
            };

            InitializeDecodeContext(ref context);
            ProcessCompressionStream(ref context);

            return context.Destination;
        }
        catch (Exception exception)
            when (exception is ArgumentException or ArgumentOutOfRangeException)
        {
            throw new ArgumentException(
                $"The data is not a valid {nameof(RefpackCodex)}",
                nameof(compressedData),
                exception
            );
        }
    }

    #region Encoding Support

    private struct EncodeContext()
    {
        private const int TableSize = 65536;

        public byte[] Source { get; init; }
        public List<byte> Destination { get; } = [];
        public int InputPosition { get; set; }
        public int OutputPosition { get; set; }
        public int LiteralRunStart { get; set; }
        public int LiteralRunLength { get; set; }
        public int[] HashTable { get; } = new int[TableSize];
        public int[] LinkTable { get; } = new int[TableSize];
    }

    private ref struct MatchInfo
    {
        public int Distance { get; init; }
        public int Length { get; init; }
        public int Cost { get; init; }
    }

    private static void WriteHeader(ref EncodeContext context)
    {
        var sourceSize = context.Source.Length;
        if (sourceSize > 0xFFFFFF)
        {
            // 4-byte size header
            context.Destination.AppendBigEndianValue(0x90FB, 2);
            context.Destination.AppendBigEndianValue((uint)sourceSize, 4);
            context.InputPosition = 6;
        }
        else
        {
            // 3-byte size header
            context.Destination.AppendBigEndianValue(0x10FB, 2);
            context.Destination.AppendBigEndianValue((uint)sourceSize, 3);
            context.InputPosition = 5;
        }
    }

    private static int CalculateHash(ReadOnlySpan<byte> data, int position)
    {
        if (position + 2 >= data.Length)
        {
            return 0;
        }

        return (data[position] << 8 | data[position + 2]) ^ (data[position + 1] << 4);
    }

    private static void InitializeHashTables(ref EncodeContext context)
    {
        Array.Fill(context.HashTable, -1);
        Array.Fill(context.LinkTable, -1);
    }

    private static void AddToHashTable(ref EncodeContext context, int position)
    {
        if (position + 2 >= context.Source.Length)
        {
            return;
        }

        var hash = CalculateHash(context.Source, position);
        var hashIndex = hash & (context.HashTable.Length - 1);
        var linkIndex = position & (context.LinkTable.Length - 1);

        context.LinkTable[linkIndex] = context.HashTable[hashIndex];
        context.HashTable[hashIndex] = position;
    }

    private static MatchInfo FindBestMatch(ref EncodeContext context)
    {
        if (context.InputPosition + 2 >= context.Source.Length)
        {
            return new MatchInfo
            {
                Distance = 0,
                Length = 0,
                Cost = 0,
            };
        }

        var hash = CalculateHash(context.Source, context.InputPosition);
        var hashIndex = hash & (context.HashTable.Length - 1);
        var hashPosition = context.HashTable[hashIndex];

        var minPosition = context.InputPosition >= 131071 ? context.InputPosition - 131071 : 0;
        var bestMatch = new MatchInfo
        {
            Distance = 0,
            Length = 0,
            Cost = 0,
        };

        var maxMatchLength = int.Min(context.Source.Length - context.InputPosition, 1028);

        while (hashPosition >= minPosition && hashPosition >= 0)
        {
            var matchPosition = hashPosition;
            var rawDistance = (context.InputPosition - 1) - matchPosition;
            var matchLength = CalculateMatchLength(ref context, matchPosition, maxMatchLength);
            if (matchLength > bestMatch.Length)
            {
                var cost = CalculateCommandCost(rawDistance, matchLength);
                var benefit = matchLength - cost + 4;
                if (benefit > bestMatch.Length - bestMatch.Cost + 4)
                {
                    bestMatch = new MatchInfo
                    {
                        Distance = rawDistance,
                        Length = matchLength,
                        Cost = cost,
                    };

                    if (matchLength >= 1028)
                    {
                        break;
                    }
                }
            }

            hashPosition = context.LinkTable[matchPosition & (context.LinkTable.Length - 1)];
        }

        return bestMatch;
    }

    private static int CalculateMatchLength(
        ref EncodeContext context,
        int matchPosition,
        int maxMatchLength
    )
    {
        var currentPosition = context.InputPosition;
        var length = 0;
        while (
            length < maxMatchLength
            && context.Source[currentPosition + length] == context.Source[matchPosition + length]
        )
        {
            ++length;
        }

        return length;
    }

    private static int CalculateCommandCost(int rawDistance, int matchLength) =>
        rawDistance switch
        {
            < 1024 when matchLength <= 10 => 2, // Short form
            < 16384 when matchLength <= 67 => 3, // Intermediate form
            _ => 4, // Long form
        };

    private static bool ShouldUseMatch(MatchInfo match) =>
        match.Length >= 3 && match.Cost < match.Length;

    private static void FlushLiteralRuns(ref EncodeContext context)
    {
        while (context.LiteralRunLength > 3)
        {
            var chunkSize = int.Min(context.LiteralRunLength & ~3, 112);
            WriteLiteralCommand(ref context, chunkSize);
            context.LiteralRunLength -= chunkSize;
            context.LiteralRunStart += chunkSize;
        }
    }

    private static void WriteLiteralCommand(ref EncodeContext context, int length)
    {
        var command = unchecked((byte)(0xE0 + (length >> 2) - 1));
        context.Destination.Add(command);
        context.OutputPosition++;

        var literalData = context.Source.AsSpan().Slice(context.LiteralRunStart, length);
        context.Destination.AddRange(literalData);
        context.OutputPosition += length;
    }

    private static void EncodeMatchCommand(ref EncodeContext context, MatchInfo match)
    {
        var embeddedLiterals = context.LiteralRunLength;
        switch (match.Cost)
        {
            case 2:
                WriteShortMatch(ref context, match, embeddedLiterals);
                break;
            case 3:
                WriteIntermediateMatch(ref context, match, embeddedLiterals);
                break;
            default:
                WriteLongMatch(ref context, match, embeddedLiterals);
                break;
        }

        // Copy embedded literal bytes
        if (embeddedLiterals > 0)
        {
            var literalData = context
                .Source.AsSpan()
                .Slice(context.LiteralRunStart, embeddedLiterals);

            context.Destination.AddRange(literalData);
            context.OutputPosition += embeddedLiterals;
        }

        context.LiteralRunLength = 0;
    }

    private static void WriteShortMatch(
        ref EncodeContext context,
        MatchInfo match,
        int literalCount
    )
    {
        var command = (byte)(
            ((match.Distance >> 8) << 5) + ((match.Length - 3) << 2) + literalCount
        );
        var secondByte = (byte)match.Distance;

        context.Destination.Add(command);
        context.Destination.Add(secondByte);
        context.OutputPosition += 2;
    }

    private static void WriteIntermediateMatch(
        ref EncodeContext context,
        MatchInfo match,
        int literalCount
    )
    {
        var command = (byte)(0x80 + (match.Length - 4));
        var secondByte = (byte)((literalCount << 6) + (match.Distance >> 8));
        var thirdByte = (byte)match.Distance;

        context.Destination.Add(command);
        context.Destination.Add(secondByte);
        context.Destination.Add(thirdByte);
        context.OutputPosition += 3;
    }

    private static void WriteLongMatch(ref EncodeContext context, MatchInfo match, int literalCount)
    {
        var lengthPruned = match.Length - 5;
        var command = (byte)(
            0xC0 + ((match.Distance >> 16) << 4) + ((lengthPruned >> 8) << 2) + literalCount
        );

        var secondByte = (byte)(match.Distance >> 8);
        var thirdByte = (byte)(match.Distance);
        var fourthByte = (byte)(lengthPruned);

        context.Destination.Add(command);
        context.Destination.Add(secondByte);
        context.Destination.Add(thirdByte);
        context.Destination.Add(fourthByte);
        context.OutputPosition += 4;
    }

    private static void AdvanceAfterMatch(ref EncodeContext context, MatchInfo match)
    {
        // Add all positions in the match to the hash table for better compression
        for (var i = 0; i < match.Length; ++i)
        {
            AddToHashTable(ref context, context.InputPosition + i);
        }

        context.InputPosition += match.Length;
        context.LiteralRunStart = context.InputPosition;
    }

    private static void WriteEndOfStream(ref EncodeContext context)
    {
        var finalLiterals = context.LiteralRunLength;
        var command = (byte)(0xFC + finalLiterals);
        context.Destination.Add(command);
        context.OutputPosition++;

        if (finalLiterals <= 0)
        {
            return;
        }

        var literalData = context.Source.AsSpan().Slice(context.LiteralRunStart, finalLiterals);
        context.Destination.AddRange(literalData);
        context.OutputPosition += finalLiterals;
    }

    private static void CompressData(ref EncodeContext context)
    {
        InitializeHashTables(ref context);

        context.InputPosition = 0;
        context.LiteralRunStart = 0;
        context.LiteralRunLength = 0;

        var maxPosition = context.Source.Length >= 4 ? context.Source.Length - 4 : 0;
        while (context.InputPosition < maxPosition)
        {
            var bestMatch = FindBestMatch(ref context);
            if (ShouldUseMatch(bestMatch))
            {
                FlushLiteralRuns(ref context);
                EncodeMatchCommand(ref context, bestMatch);
                AdvanceAfterMatch(ref context, bestMatch);
            }
            else
            {
                AddToHashTable(ref context, context.InputPosition);
                context.InputPosition++;
                context.LiteralRunLength++;
            }
        }

        // Handle remaining data as literals
        context.LiteralRunLength += context.Source.Length - context.InputPosition;
        FlushLiteralRuns(ref context);
        WriteEndOfStream(ref context);
    }

    #endregion

    #region Decoding Support

    private struct DecodeContext()
    {
        public byte[] Source { get; init; }
        public List<byte> Destination { get; } = [];
        public int ExpectedOutputSize { get; init; }
        public int InputPosition { get; set; }
        public int OutputPosition { get; set; }
        public byte CurrentCommand { get; set; }
    }

    private static void InitializeDecodeContext(ref DecodeContext context)
    {
        var headerType = context.Source.AsSpan().GetBigEndianValue(2);
        var uses4ByteSize = (headerType & 0x8000) != 0;
        var hasSkipField = (headerType & 0x0100) != 0;
        var sizeFieldBytes = uses4ByteSize ? 4 : 3;
        var skipFieldBytes = hasSkipField ? sizeFieldBytes : 0;

        context.InputPosition = 2 + skipFieldBytes + sizeFieldBytes;
        context.OutputPosition = 0;

        ArgumentOutOfRangeException.ThrowIfLessThan(context.Source.Length, context.InputPosition);

        // It will probably be this large if nothing goes wrong.
        context.Destination.Capacity = context.ExpectedOutputSize;
    }

    private static void CopyLiteralBytes(ref DecodeContext context, int byteCount)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            context.InputPosition + byteCount,
            context.Source.Length
        );

        var sourceData = context.Source.AsSpan().Slice(context.InputPosition, byteCount);
        context.Destination.AddRange(sourceData);
        context.InputPosition += byteCount;
        context.OutputPosition += byteCount;
    }

    private static void CopyMatchBytes(ref DecodeContext context, int rawDistance, int matchLength)
    {
        ArgumentOutOfRangeException.ThrowIfZero(context.OutputPosition);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(context.OutputPosition, rawDistance);

        var referencePosition = context.OutputPosition - 1 - rawDistance;
        var sourceData = context.Source.AsSpan().Slice(referencePosition, matchLength);
        context.Destination.AddRange(sourceData);
        context.OutputPosition += matchLength;
    }

    private static void ProcessShortCommand(ref DecodeContext context)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            context.InputPosition + 1,
            context.Source.Length
        );

        var secondByte = context.Source[context.InputPosition++];
        var literalCount = context.CurrentCommand & 0x03;
        var rawDistance = ((context.CurrentCommand & 0x60) << 3) + secondByte;
        var matchLength = ((context.CurrentCommand & 0x1C) >> 2) + 3;

        CopyLiteralBytes(ref context, literalCount);
        CopyMatchBytes(ref context, rawDistance, matchLength);
    }

    private static void ProcessIntermediateCommand(ref DecodeContext context)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            context.InputPosition + 2,
            context.Source.Length
        );

        var secondByte = context.Source[context.InputPosition++];
        var thirdByte = context.Source[context.InputPosition++];
        var literalCount = secondByte >> 6;
        var rawDistance = ((secondByte & 0x3F) << 8) + thirdByte;
        var matchLength = (context.CurrentCommand & 0x3F) + 4;

        CopyLiteralBytes(ref context, literalCount);
        CopyMatchBytes(ref context, rawDistance, matchLength);
    }

    private static void ProcessLongCommand(ref DecodeContext context)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            context.InputPosition + 3,
            context.Source.Length
        );

        var secondByte = context.Source[context.InputPosition++];
        var thirdByte = context.Source[context.InputPosition++];
        var fourthByte = context.Source[context.InputPosition++];
        var literalCount = context.CurrentCommand & 0x03;
        var rawDistance =
            (((context.CurrentCommand & 0x10) >> 4) << 16) + (secondByte << 8) + thirdByte;

        var matchLength = (((context.CurrentCommand & 0x0C) >> 2) << 8) + fourthByte + 5;

        CopyLiteralBytes(ref context, literalCount);
        CopyMatchBytes(ref context, rawDistance, matchLength);
    }

    private static bool ProcessLiteralCommand(ref DecodeContext context)
    {
        var runLength = ((context.CurrentCommand & 0x1F) << 2) + 4;
        if (runLength <= 112)
        {
            // Regular literal block
            CopyLiteralBytes(ref context, runLength);
            return false; // Continue processing
        }

        // End of stream with optional literal bytes
        var finalLiterals = context.CurrentCommand & 0x03;
        if (finalLiterals > 0)
        {
            CopyLiteralBytes(ref context, finalLiterals);
        }

        return true; // End of stream reached
    }

    private static void ProcessCompressionStream(ref DecodeContext context)
    {
        while (context.InputPosition < context.Source.Length)
        {
            context.CurrentCommand = context.Source[context.InputPosition++];
            if ((context.CurrentCommand & 0x80) == 0)
            {
                ProcessShortCommand(ref context);
            }
            else if ((context.CurrentCommand & 0x40) == 0)
            {
                ProcessIntermediateCommand(ref context);
            }
            else if ((context.CurrentCommand & 0x20) == 0)
            {
                ProcessLongCommand(ref context);
            }
            else
            {
                if (ProcessLiteralCommand(ref context))
                {
                    break; // End of stream reached
                }
            }
        }
    }

    #endregion
}
