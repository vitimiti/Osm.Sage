using System.Buffers.Binary;

namespace Osm.Sage.Compression.Eac.Codex;

public partial class RefPackCodex
{
    /// <summary>
    /// Safe wrapper for hash table operations used during compression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The hash table maps 3-byte sequences to their most recent position in the input.
    /// This enables the fast lookup of potential back-reference matches during compression.
    /// </para>
    /// <para>
    /// Uses a fixed size of 65,536 entries (16-bit hash space) with -1 indicating
    /// an unused slot. The table is initialized with all "-1" values.
    /// </para>
    /// </remarks>
    private sealed class HashTable : IDisposable
    {
        private readonly int[] _table;
        private bool _disposed;

        /// <summary>
        /// Initializes a new hash table with the specified size.
        /// </summary>
        /// <param name="size">Number of hash table entries (typically 65536).</param>
        public HashTable(int size)
        {
            _table = new int[size];
            Array.Fill(_table, -1);
        }

        /// <summary>
        /// Gets or sets the position stored at the specified hash index.
        /// </summary>
        /// <param name="index">Hash index to access.</param>
        /// <returns>The position stored at this index, or -1 if unused.</returns>
        public int this[int index]
        {
            get => _table[index];
            set => _table[index] = value;
        }

        /// <summary>
        /// Releases the hash table resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Safe wrapper for link table operations used during compression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The link table implements hash collision chaining by storing the previous
    /// position that had the same hash value. This creates linked chains of positions
    /// with identical 3-byte hash values.
    /// </para>
    /// <para>
    /// Uses a fixed size of 131,072 entries matching the maximum back-reference distance.
    /// Each entry stores the previous position in the hash chain for that slot.
    /// </para>
    /// </remarks>
    private sealed class LinkTable(int size) : IDisposable
    {
        private readonly int[] _table = new int[size];
        private bool _disposed;

        /// <summary>
        /// Gets or sets the previous chain position at the specified index.
        /// </summary>
        /// <param name="index">Position index (typically masked with 131,071).</param>
        /// <returns>The previous position in the hash chain.</returns>
        public int this[int index]
        {
            get => _table[index];
            set => _table[index] = value;
        }

        /// <summary>
        /// Releases the link table resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Represents the best back-reference match found during compression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contains all information needed to encode a back-reference, including the offset
    /// to the matching data, the length of the match, and the encoding cost in bytes.
    /// </para>
    /// <para>
    /// The encoding cost depends on the offset and length values:
    /// <list type="bullet">
    /// <item>Cost 2: Short form encoding (2 bytes total)</item>
    /// <item>Cost 3: Medium form encoding (3 bytes total)</item>
    /// <item>Cost 4: Long form encoding (4 bytes total)</item>
    /// </list>
    /// </para>
    /// </remarks>
    private readonly struct MatchResult
    {
        /// <summary>
        /// Gets the back-reference offset (distance to the matching data).
        /// </summary>
        /// <value>The offset in bytes from the current position to the match.</value>
        public uint Offset { get; init; }

        /// <summary>
        /// Gets the length of the matching sequence in bytes.
        /// </summary>
        /// <value>The number of bytes that match at the back-reference position.</value>
        public uint Length { get; init; }

        /// <summary>
        /// Gets the encoding cost in bytes for this back-reference.
        /// </summary>
        /// <value>The number of bytes required to encode this match (2, 3, or 4).</value>
        public uint Cost { get; init; }

        /// <summary>
        /// Determines whether this match is valid (long enough to be worth encoding).
        /// </summary>
        /// <value><c>true</c> if the match length exceeds 2 bytes; otherwise <c>false</c>.</value>
        public bool IsValid => Length > 2;

        /// <summary>
        /// Determines whether this match provides better compression than another.
        /// </summary>
        /// <param name="currentBest">Length of the current best match.</param>
        /// <param name="currentCost">Encoding cost of the current best match.</param>
        /// <returns><c>true</c> if this match is more efficient; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// The comparison uses a heuristic that accounts for both the compressed size
        /// (match length minus encoding cost) and adds a bias factor of 4 bytes.
        /// </remarks>
        public bool IsWorthwhile(uint currentBest, uint currentCost) =>
            Length - Cost + 4 > currentBest - currentCost + 4;
    }

    /// <summary>
    /// Context for RefPack compression operations with enhanced functionality.
    /// </summary>
    /// <param name="source">The source data to compress.</param>
    /// <param name="destination">The destination buffer for compressed data.</param>
    /// <remarks>
    /// <para>
    /// Encapsulates all state needed during compression including source and destination
    /// buffers, current positions, and literal run tracking. Uses ref struct for
    /// stack-only allocation and optimal performance.
    /// </para>
    /// <para>
    /// The context tracks several key positions:
    /// <list type="bullet">
    /// <item><strong>SourcePosition:</strong> Current reading position in source data</item>
    /// <item><strong>DestinationPosition:</strong> Current writing position in output</item>
    /// <item><strong>RunPosition:</strong> Start of current literal run</item>
    /// <item><strong>LiteralRun:</strong> Number of literal bytes accumulated</item>
    /// </list>
    /// </para>
    /// </remarks>
    private ref struct EnhancedEncodeContext(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        private Span<byte> _destination = destination;

        /// <summary>
        /// Gets the source data being compressed.
        /// </summary>
        public readonly ReadOnlySpan<byte> Source = source;

        /// <summary>
        /// Gets or sets the current reading position in the source data.
        /// </summary>
        public int SourcePosition = 0;

        /// <summary>
        /// Gets or sets the current writing position in the destination buffer.
        /// </summary>
        public int DestinationPosition = 0;

        /// <summary>
        /// Gets or sets the starting position of the current literal run.
        /// </summary>
        public int RunPosition = 0;

        /// <summary>
        /// Gets or sets the number of literal bytes in the current run.
        /// </summary>
        public uint LiteralRun = 0;

        /// <summary>
        /// Writes a single byte to the destination buffer and advances the position.
        /// </summary>
        /// <param name="value">The byte value to write.</param>
        public void WriteByte(byte value) => _destination[DestinationPosition++] = value;

        /// <summary>
        /// Writes a span of bytes to the destination buffer and advances the position.
        /// </summary>
        /// <param name="data">The data to write to the destination.</param>
        public void WriteBytes(ReadOnlySpan<byte> data)
        {
            data.CopyTo(_destination[DestinationPosition..]);
            DestinationPosition += data.Length;
        }

        /// <summary>
        /// Advances the source reading position by the specified number of bytes.
        /// </summary>
        /// <param name="count">Number of bytes to advance.</param>
        public void AdvanceSource(int count) => SourcePosition += count;

        /// <summary>
        /// Gets a single byte from the source data at the specified offset.
        /// </summary>
        /// <param name="offset">The offset into the source data.</param>
        /// <returns>The byte value at the specified offset.</returns>
        public readonly byte GetSourceByte(int offset) => Source[offset];

        /// <summary>
        /// Gets a slice of the source data starting at the specified position.
        /// </summary>
        /// <param name="start">Starting offset for the slice.</param>
        /// <param name="length">Length of the slice in bytes.</param>
        /// <returns>A read-only span representing the requested slice.</returns>
        public readonly ReadOnlySpan<byte> GetSourceSlice(int start, int length) =>
            Source.Slice(start, length);

        /// <summary>
        /// Increments the literal run counter and advances the source position.
        /// </summary>
        /// <remarks>
        /// This method is used when no suitable back-reference match is found,
        /// indicating that the current byte should be encoded as a literal.
        /// </remarks>
        public void IncrementLiteralRun()
        {
            LiteralRun++;
            AdvanceSource(1);
        }

        /// <summary>
        /// Resets the literal run tracking to start a new run.
        /// </summary>
        /// <remarks>
        /// Called after writing a back-reference to start tracking literals
        /// from the current position onwards.
        /// </remarks>
        public void ResetRun()
        {
            LiteralRun = 0;
            RunPosition = SourcePosition;
        }
    }

    /// <summary>
    /// Handles hash table operations for the compression algorithm.
    /// </summary>
    /// <param name="hashTable">The main hash table for position lookup.</param>
    /// <param name="linkTable">The link table for hash collision chaining.</param>
    /// <remarks>
    /// <para>
    /// Provides a clean interface for managing the hash chain data structures
    /// used to quickly find potential back-reference matches. The hash table
    /// stores the most recent position for each hash value, while the link
    /// table maintains chains of older positions with the same hash.
    /// </para>
    /// <para>
    /// Uses ref struct for stack-only allocation and contains references to
    /// the managed hash and link table objects.
    /// </para>
    /// </remarks>
    private ref struct HashManager(HashTable hashTable, LinkTable linkTable)
    {
        /// <summary>
        /// Updates the hash chain by adding a new position.
        /// </summary>
        /// <param name="position">The position to add to the chain.</param>
        /// <param name="hash">The hash value for this position.</param>
        /// <remarks>
        /// Links the new position into the hash chain by setting its link table
        /// entry to the previous head of the chain, then updating the hash table
        /// to point to the new position as the chain head.
        /// </remarks>
        public void UpdateChain(int position, int hash)
        {
            linkTable[position & 131071] = hashTable[hash];
            hashTable[hash] = position;
        }

        /// <summary>
        /// Gets the starting position of the hash chain for the specified hash value.
        /// </summary>
        /// <param name="hash">The hash value to look up.</param>
        /// <returns>The most recent position with this hash value, or the value "-1" if none is valid.</returns>
        public readonly int GetChainStart(int hash) => hashTable[hash];

        /// <summary>
        /// Gets the next position in the hash chain.
        /// </summary>
        /// <param name="position">The current position in the chain.</param>
        /// <returns>The previous position with the same hash value, or "-1" if we are at the end of the chain.</returns>
        public readonly int GetNextInChain(int position) => linkTable[position & 131071];
    }

    /// <summary>
    /// Calculates a hash value for the 3-byte sequence at the specified position.
    /// </summary>
    /// <param name="context">The compression context containing source data.</param>
    /// <param name="position">Position in the source data to hash.</param>
    /// <returns>A 16-bit hash value for the 3-byte sequence.</returns>
    /// <remarks>
    /// <para>
    /// Uses a simple hash function that combines bytes 0, 1, and 2 of the sequence:
    /// <code>hash = ((byte0 &lt;&lt; 8) | byte2) ^ (byte1 &lt;&lt; 4)</code>
    /// </para>
    /// <para>
    /// Returns 0 if there aren't enough bytes remaining for a 3-byte sequence.
    /// The hash is designed to provide good distribution while being fast to compute.
    /// </para>
    /// </remarks>
    private static int ComputeHash(ref EnhancedEncodeContext context, int position)
    {
        if (position + 2 >= context.Source.Length)
        {
            return 0;
        }

        var b0 = context.GetSourceByte(position);
        var b1 = context.GetSourceByte(position + 1);
        var b2 = context.GetSourceByte(position + 2);

        return ((b0 << 8) | b2) ^ (b1 << 4);
    }

    /// <summary>
    /// Finds the length of matching bytes between two positions in the source data.
    /// </summary>
    /// <param name="context">The compression context containing source data.</param>
    /// <param name="pos1">First position to compare.</param>
    /// <param name="pos2">Second position to compare.</param>
    /// <param name="maxMatch">Maximum number of bytes to compare.</param>
    /// <returns>The number of consecutive matching bytes.</returns>
    /// <remarks>
    /// <para>
    /// Performs a byte-by-byte comparison starting from the two positions until
    /// a mismatch is found, the maximum match length is reached, or the end of
    /// source data is encountered.
    /// </para>
    /// <para>
    /// This is a critical performance function as it's called frequently during
    /// match evaluation. The comparison stops early on the first mismatch.
    /// </para>
    /// </remarks>
    private static uint FindMatchLength(
        ref EnhancedEncodeContext context,
        int pos1,
        int pos2,
        uint maxMatch
    )
    {
        var length = 0U;
        while (
            length < maxMatch
            && pos1 + length < context.Source.Length
            && pos2 + length < context.Source.Length
            && context.GetSourceByte(pos1 + (int)length)
                == context.GetSourceByte(pos2 + (int)length)
        )
        {
            length++;
        }

        return length;
    }

    /// <summary>
    /// Calculates the encoding cost in bytes for a back-reference with given parameters.
    /// </summary>
    /// <param name="offset">The back-reference offset (distance to matching data).</param>
    /// <param name="length">The length of the matching sequence.</param>
    /// <returns>The number of bytes required to encode this back-reference (2, 3, or 4).</returns>
    /// <remarks>
    /// <para>
    /// RefPack uses three encoding formats with different capabilities and costs:
    /// <list type="bullet">
    /// <item><strong>Short form (2 bytes):</strong> Offsets &lt; 1024 and lengths ≤ 10</item>
    /// <item><strong>Medium form (3 bytes):</strong> Offsets &lt; 16384 and lengths ≤ 67</item>
    /// <item><strong>Long form (4 bytes):</strong> All other valid combinations</item>
    /// </list>
    /// </para>
    /// <para>
    /// The function selects the most compact encoding that can represent the
    /// given offset and length combination.
    /// </para>
    /// </remarks>
    private static uint CalculateEncodingCost(uint offset, uint length) =>
        offset switch
        {
            < 1024 when length <= 10 => 2, // Short form
            < 16384 when length <= 67 => 3, // Medium form
            _ => 4, // Long form
        };

    /// <summary>
    /// Searches for the best back-reference match at the current position.
    /// </summary>
    /// <param name="context">The compression context.</param>
    /// <param name="hashManager">The hash manager for chain traversal.</param>
    /// <param name="maxLength">Maximum match length to consider.</param>
    /// <returns>The best match found, or an invalid match if none suitable.</returns>
    /// <remarks>
    /// <para>
    /// Implements the core match-finding algorithm by:
    /// <list type="number">
    /// <item>Computing hash for the current 3-byte sequence</item>
    /// <item>Following the hash chain to find all positions with the same hash</item>
    /// <item>Evaluating each potential match for length and encoding efficiency</item>
    /// <item>Selecting the match with the best compression ratio</item>
    /// </list>
    /// </para>
    /// <para>
    /// Only considers positions within the maximum back-reference distance of 131,071 bytes.
    /// Stops early if a match of the maximum possible length (1028 bytes) is found.
    /// </para>
    /// </remarks>
    private static MatchResult FindBestMatch(
        ref EnhancedEncodeContext context,
        ref HashManager hashManager,
        uint maxLength
    )
    {
        var hash = ComputeHash(ref context, context.SourcePosition);
        var chainStart = hashManager.GetChainStart(hash);
        var minValidPosition = int.Max(context.SourcePosition - 131071, 0);

        var bestMatch = new MatchResult { Length = 2, Cost = 2 };

        var currentPosition = chainStart;
        while (currentPosition >= minValidPosition)
        {
            var match = EvaluateMatch(ref context, currentPosition, maxLength);
            if (match.IsValid && match.IsWorthwhile(bestMatch.Length, bestMatch.Cost))
            {
                bestMatch = match;
                if (bestMatch.Length >= 1028) // Max possible match
                {
                    break;
                }
            }

            currentPosition = hashManager.GetNextInChain(currentPosition);
        }

        return bestMatch;
    }

    /// <summary>
    /// Evaluates a potential match at a specific position in the source data.
    /// </summary>
    /// <param name="context">The compression context.</param>
    /// <param name="matchPosition">Position of the potential match.</param>
    /// <param name="maxLength">Maximum match length to consider.</param>
    /// <returns>A MatchResult describing the match quality and encoding cost.</returns>
    /// <remarks>
    /// <para>
    /// Performs a quick rejection test by comparing the byte at offset +2, which
    /// provides early termination for most non-matches without doing a full
    /// byte-by-byte comparison.
    /// </para>
    /// <para>
    /// If the quick test passes, performs a full match length calculation and
    /// determines the optimal encoding format and cost for this match.
    /// </para>
    /// </remarks>
    private static MatchResult EvaluateMatch(
        ref EnhancedEncodeContext context,
        int matchPosition,
        uint maxLength
    )
    {
        // Quick rejection test
        if (
            context.SourcePosition + 2 >= context.Source.Length
            || matchPosition + 2 >= context.Source.Length
            || context.GetSourceByte(context.SourcePosition + 2)
                != context.GetSourceByte(matchPosition + 2)
        )
        {
            return default;
        }

        var matchLength = FindMatchLength(
            ref context,
            context.SourcePosition,
            matchPosition,
            maxLength
        );

        if (matchLength <= 2)
        {
            return default;
        }

        var offset = (uint)(context.SourcePosition - 1 - matchPosition);
        var cost = CalculateEncodingCost(offset, matchLength);

        return new MatchResult
        {
            Offset = offset,
            Length = matchLength,
            Cost = cost,
        };
    }

    /// <summary>
    /// Processes accumulated literal bytes by writing them as literal blocks.
    /// </summary>
    /// <param name="context">The compression context.</param>
    /// <remarks>
    /// <para>
    /// RefPack encodes literal bytes in blocks of up to 112 bytes, with the
    /// block size rounded down to a multiple of 4. This method processes all
    /// accumulated literal bytes that exceed the minimum block size of 4 bytes.
    /// </para>
    /// <para>
    /// Literal blocks use the format: <c>0xE0 + (blockSize/4 - 1)</c> followed
    /// by the literal bytes themselves.
    /// </para>
    /// </remarks>
    private static void FlushLiteralRun(ref EnhancedEncodeContext context)
    {
        while (context.LiteralRun > 3)
        {
            var blockSize = uint.Min(112u, context.LiteralRun & ~3u);
            WriteLiteralBlock(ref context, blockSize);
            context.LiteralRun -= blockSize;
            context.RunPosition += (int)blockSize;
        }
    }

    /// <summary>
    /// Writes a literal block to the output stream.
    /// </summary>
    /// <param name="context">The compression context.</param>
    /// <param name="length">Number of literal bytes to write (must be multiple of 4).</param>
    /// <remarks>
    /// <para>
    /// Writes the literal block header byte followed by the literal data.
    /// The header byte format is: <c>0xE0 + (length/4 - 1)</c>.
    /// </para>
    /// <para>
    /// This encoding allows for literal blocks of 4, 8, 12, ..., up to 112 bytes
    /// using header values from 0xE0 to 0xFB.
    /// </para>
    /// </remarks>
    private static void WriteLiteralBlock(ref EnhancedEncodeContext context, uint length)
    {
        context.WriteByte((byte)(0xE0 + (length >> 2) - 1));
        var data = context.GetSourceSlice(context.RunPosition, (int)length);
        context.WriteBytes(data);
    }

    /// <summary>
    /// Writes a short-form back-reference (2 bytes) to the output stream.
    /// </summary>
    /// <param name="context">The compression context.</param>
    /// <param name="offset">Back-reference offset (must be &lt; 1024).</param>
    /// <param name="length">Match length (must be 3-10).</param>
    /// <param name="run">Number of preceding literal bytes (0-3).</param>
    /// <remarks>
    /// <para>
    /// Short form encoding uses 2 bytes with the following bit layout:
    /// <list type="bullet">
    /// <item>Byte 0 bits 7-5: High 3 bits of offset (offset >> 8)</item>
    /// <item>Byte 0 bits 4-2: Length - 3 (encoded as 0-7 for lengths 3-10)</item>
    /// <item>Byte 0 bits 1-0: Literal run count (0-3)</item>
    /// <item>Byte 1: Low 8 bits of offset</item>
    /// </list>
    /// </para>
    /// </remarks>
    private static void WriteShortForm(
        ref EnhancedEncodeContext context,
        uint offset,
        uint length,
        uint run
    )
    {
        context.WriteByte((byte)(((offset >> 8) << 5) + ((length - 3) << 2) + run));
        context.WriteByte((byte)offset);
    }

    /// <summary>
    /// Writes a medium-form back-reference (3 bytes) to the output stream.
    /// </summary>
    /// <param name="context">The compression context.</param>
    /// <param name="offset">Back-reference offset (must be &lt; 16384).</param>
    /// <param name="length">Match length (must be 4-67).</param>
    /// <param name="run">Number of preceding literal bytes (0-3).</param>
    /// <remarks>
    /// <para>
    /// Medium form encoding uses 3 bytes with the following bit layout:
    /// <list type="bullet">
    /// <item>Byte 0, bit 7: Set to 1 (format identifier)</item>
    /// <item>Byte 0, bits 5-0: Length - 4 (encoded as 0-63 for lengths 4-67)</item>
    /// <item>Byte 1, bits 7-6: Literal run count (0-3)</item>
    /// <item>Byte 1, bits 5-0: High 6 bits of offset (offset >> 8)</item>
    /// <item>Byte 2: Low 8 bits of offset</item>
    /// </list>
    /// </para>
    /// </remarks>
    private static void WriteMediumForm(
        ref EnhancedEncodeContext context,
        uint offset,
        uint length,
        uint run
    )
    {
        context.WriteByte((byte)(0x80 + (length - 4)));
        context.WriteByte((byte)((run << 6) + (offset >> 8)));
        context.WriteByte((byte)offset);
    }

    /// <summary>
    /// Writes a long-form back-reference (4 bytes) to the output stream.
    /// </summary>
    /// <param name="context">The compression context.</param>
    /// <param name="offset">Back-reference offset (can be up to 131,072).</param>
    /// <param name="length">Match length (can be 5-1028).</param>
    /// <param name="run">Number of preceding literal bytes (0-3).</param>
    /// <remarks>
    /// <para>
    /// Long form encoding uses 4 bytes with the following bit layout:
    /// <list type="bullet">
    /// <item>Byte 0 bits 7-6: Set to 11 (format identifier)</item>
    /// <item>Byte 0 bits 5-4: High 2 bits of offset (offset >> 16)</item>
    /// <item>Byte 0 bits 3-2: High 2 bits of (length - 5) >> 8</item>
    /// <item>Byte 0 bits 1-0: Literal run count (0-3)</item>
    /// <item>Byte 1: Middle 8 bits of offset (offset >> 8)</item>
    /// <item>Byte 2: Low 8 bits of offset</item>
    /// <item>Byte 3: Low 8 bits of (length - 5)</item>
    /// </list>
    /// </para>
    /// </remarks>
    private static void WriteLongForm(
        ref EnhancedEncodeContext context,
        uint offset,
        uint length,
        uint run
    )
    {
        context.WriteByte((byte)(0xc0 + ((offset >> 16) << 4) + (((length - 5) >> 8) << 2) + run));
        context.WriteByte((byte)(offset >> 8));
        context.WriteByte((byte)offset);
        context.WriteByte((byte)(length - 5));
    }

    /// <summary>
    /// Writes the RefPack header to the output buffer.
    /// </summary>
    /// <param name="output">Destination buffer for the header.</param>
    /// <param name="sourceLength">Length of the uncompressed source data.</param>
    /// <returns>Number of header bytes written (5 or 6).</returns>
    /// <remarks>
    /// <para>
    /// RefPack headers have two formats depending on the uncompressed size:
    /// <list type="bullet">
    /// <item><strong>Small format (5 bytes):</strong> For sizes ≤ 16MB (0xFFFFFF)</item>
    /// <item><strong>Large format (6 bytes):</strong> For sizes > 16MB</item>
    /// </list>
    /// </para>
    /// <para>
    /// Small format: 0x10FB + 3-byte big-endian size<br/>
    /// Large format: 0x90FB + 4-byte big-endian size
    /// </para>
    /// <para>
    /// All multibyte values are stored in big-endian byte order for compatibility
    /// with the original RefPack specification.
    /// </para>
    /// </remarks>
    private static int WriteHeader(Span<byte> output, int sourceLength)
    {
        if (sourceLength > 0xFFFFFF)
        {
            BinaryPrimitives.WriteUInt16BigEndian(output, 0x90FB);
            BinaryPrimitives.WriteUInt32BigEndian(output[2..], (uint)sourceLength);
            return 6;
        }

        BinaryPrimitives.WriteUInt16BigEndian(output, 0x10FB);
        var size = (uint)sourceLength;
        output[2] = (byte)(size >> 16);
        output[3] = (byte)(size >> 8);
        output[4] = (byte)size;
        return 5;
    }

    /// <summary>
    /// Writes a back-reference using the appropriate encoding format.
    /// </summary>
    /// <param name="context">The compression context.</param>
    /// <param name="match">The match information to encode.</param>
    /// <remarks>
    /// <para>
    /// Dispatches to the appropriate encoding function based on the match cost:
    /// <list type="bullet">
    /// <item>Cost 2: <see cref="WriteShortForm"/></item>
    /// <item>Cost 3: <see cref="WriteMediumForm"/></item>
    /// <item>Cost 4: <see cref="WriteLongForm"/></item>
    /// </list>
    /// </para>
    /// <para>
    /// All formats include the current literal run count as part of the encoding,
    /// allowing small numbers of literal bytes to be encoded inline with back-references.
    /// </para>
    /// </remarks>
    private static void WriteBackReference(ref EnhancedEncodeContext context, MatchResult match)
    {
        switch (match.Cost)
        {
            case 2: // Short form
                WriteShortForm(ref context, match.Offset, match.Length, context.LiteralRun);
                break;
            case 3: // Medium form
                WriteMediumForm(ref context, match.Offset, match.Length, context.LiteralRun);
                break;
            case 4: // Long form
                WriteLongForm(ref context, match.Offset, match.Length, context.LiteralRun);
                break;
        }
    }

    /// <summary>
    /// Writes remaining literal bytes and finalizes the compressed stream.
    /// </summary>
    /// <param name="context">The compression context.</param>
    /// <param name="remainingBytes">Number of bytes remaining in the source.</param>
    /// <remarks>
    /// <para>
    /// Handles end-of-stream processing by:
    /// <list type="number">
    /// <item>Writing any large literal blocks (4+ bytes) using standard literal encoding</item>
    /// <item>Writing the end-of-stream marker (0xFC-0xFF) with final 0-3 literal bytes</item>
    /// </list>
    /// </para>
    /// <para>
    /// The end-of-stream marker format is: <c>0xFC + finalLiteralCount</c> where
    /// finalLiteralCount is 0-3, followed by those literal bytes if any.
    /// </para>
    /// </remarks>
    private static void FinalizeStream(ref EnhancedEncodeContext context, int remainingBytes)
    {
        var totalLiterals = context.LiteralRun + (uint)remainingBytes;

        // Write large literal blocks
        while (totalLiterals > 3)
        {
            var blockSize = uint.Min(112U, totalLiterals & ~3U);
            totalLiterals -= blockSize;
            WriteLiteralBlock(ref context, blockSize);
            context.RunPosition += (int)blockSize;
        }

        // Write end-of-stream marker with final literals
        context.WriteByte((byte)(0xFC + totalLiterals));
        if (totalLiterals <= 0)
        {
            return;
        }

        var finalData = context.GetSourceSlice(context.RunPosition, (int)totalLiterals);
        context.WriteBytes(finalData);
    }

    /// <summary>
    /// Updates the hash chain for all positions covered by a back-reference match.
    /// </summary>
    /// <param name="context">The compression context.</param>
    /// <param name="hashManager">The hash manager for chain updates.</param>
    /// <param name="matchLength">Length of the match that was encoded.</param>
    /// <remarks>
    /// <para>
    /// After encoding a back-reference, the algorithm must update the hash tables
    /// for all positions within the matched region. This ensures that future
    /// matches can reference positions within this region.
    /// </para>
    /// <para>
    /// Each position gets its hash computed, and the chain updated before advancing
    /// to the next position. This maintains the hash table consistency throughout
    /// the compression process.
    /// </para>
    /// </remarks>
    private static void UpdateHashChainForMatch(
        ref EnhancedEncodeContext context,
        ref HashManager hashManager,
        uint matchLength
    )
    {
        for (int i = 0; i < (int)matchLength; i++)
        {
            var hash = ComputeHash(ref context, context.SourcePosition);
            hashManager.UpdateChain(context.SourcePosition, hash);
            context.AdvanceSource(1);
        }
    }

    /// <summary>
    /// Core RefPack compression algorithm implementation with a simplified control flow.
    /// </summary>
    /// <param name="source">Source data to compress.</param>
    /// <param name="destination">Destination buffer for compressed output.</param>
    /// <returns>Number of bytes written to the destination buffer.</returns>
    /// <remarks>
    /// <para>
    /// Implements the main compression loop using a greedy LZ77-style algorithm:
    /// <list type="number">
    /// <item>For each position, search for the best back-reference match</item>
    /// <item>If a good match is found, flush pending literals and encode the match</item>
    /// <item>If no good match, add the byte to the literal run and continue</item>
    /// <item>Finalize the stream when all inputs have been processed</item>
    /// </list>
    /// </para>
    /// <para>
    /// The algorithm reserves the last 4 bytes of input for special handling,
    /// ensuring there's always enough lookahead for match finding and proper
    /// stream termination.
    /// </para>
    /// </remarks>
    private static int RefCompress(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        var context = new EnhancedEncodeContext(source, destination);
        using var hashTable = new HashTable(65536);
        using var linkTable = new LinkTable(131072);
        var hashManager = new HashManager(hashTable, linkTable);

        var remainingBytes = source.Length - 4;

        while (remainingBytes >= 0)
        {
            var maxMatchLength = uint.Min((uint)remainingBytes, 1028U);
            var bestMatch = FindBestMatch(ref context, ref hashManager, maxMatchLength);

            if (!bestMatch.IsValid || bestMatch.Cost >= bestMatch.Length || remainingBytes < 4)
            {
                // No good match found - add to literal run
                var hash = ComputeHash(ref context, context.SourcePosition);
                hashManager.UpdateChain(context.SourcePosition, hash);
                context.IncrementLiteralRun();
                remainingBytes--;
                continue;
            }

            // Good match found - flush literals and write back-reference
            FlushLiteralRun(ref context);
            WriteBackReference(ref context, bestMatch);

            // Write any remaining literal bytes from the run
            if (context.LiteralRun > 0)
            {
                var literalData = context.GetSourceSlice(
                    context.RunPosition,
                    (int)context.LiteralRun
                );

                context.WriteBytes(literalData);
            }

            // Update hash chain and advance
            UpdateHashChainForMatch(ref context, ref hashManager, bestMatch.Length);
            context.ResetRun();
            remainingBytes -= (int)bestMatch.Length;
        }

        FinalizeStream(ref context, remainingBytes + 4);
        return context.DestinationPosition;
    }

    /// <summary>
    /// Compresses the input data using the RefPack algorithm.
    /// </summary>
    /// <param name="decompressedData">The source data to compress.</param>
    /// <param name="compressedData">The destination buffer for compressed output.</param>
    /// <returns>The total number of compressed bytes written, including header.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the decompressed data is too small (less than 2 bytes).
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the main entry point for RefPack compression. The method:
    /// <list type="number">
    /// <item>Validates input parameters</item>
    /// <item>Writes the appropriate RefPack header</item>
    /// <item>Compresses the data using the RefPack algorithm</item>
    /// <item>Returns the total compressed size including header</item>
    /// </list>
    /// </para>
    /// <para>
    /// The compressed data format consists of a header followed by the compressed
    /// payload. The header size varies (5 or 6 bytes) depending on the input size.
    /// </para>
    /// <para>
    /// The implementation uses safe managed code throughout, avoiding unsafe
    /// pointer operations while maintaining performance through span-based operations.
    /// </para>
    /// </remarks>
    public int Encode(ReadOnlySpan<byte> decompressedData, Span<byte> compressedData)
    {
        if (decompressedData.Length < 2)
        {
            throw new ArgumentException(
                "The decompressed data must be at least 2 bytes in length.",
                nameof(decompressedData)
            );
        }

        var headerLength = WriteHeader(compressedData, decompressedData.Length);
        var compressedLength = RefCompress(decompressedData, compressedData[headerLength..]);

        return headerLength + compressedLength;
    }
}
