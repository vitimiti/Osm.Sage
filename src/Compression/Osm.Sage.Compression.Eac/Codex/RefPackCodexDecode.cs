namespace Osm.Sage.Compression.Eac.Codex;

public partial class RefPackCodex
{
    /// <summary>
    /// Helper structure to track reading position in a span without unsafe code.
    /// </summary>
    /// <param name="data">The source data span to read from.</param>
    /// <remarks>
    /// Provides sequential byte reading with automatic position tracking.
    /// Uses ref struct for stack-only allocation and optimal performance.
    /// </remarks>
    private ref struct SpanReader(ReadOnlySpan<byte> data)
    {
        private readonly ReadOnlySpan<byte> _data = data;
        private int _position = 0;

        /// <summary>
        /// Reads the next byte and advances the position.
        /// </summary>
        /// <returns>The byte at the current position.</returns>
        public byte ReadByte() => _data[_position++];

        /// <summary>
        /// Skips the specified number of bytes by advancing the position.
        /// </summary>
        /// <param name="count">Number of bytes to skip.</param>
        public void Skip(int count) => _position += count;
    }

    /// <summary>
    /// Helper structure to track writing position in a span without unsafe code.
    /// </summary>
    /// <param name="data">The destination data span to write to.</param>
    /// <remarks>
    /// Provides sequential byte writing and back-reference copying with position tracking.
    /// Uses ref struct for stack-only allocation and optimal performance.
    /// </remarks>
    private ref struct SpanWriter(Span<byte> data)
    {
        private readonly Span<byte> _data = data;

        /// <summary>
        /// Gets the current write position in the output buffer.
        /// </summary>
        public int Position { get; private set; } = 0;

        /// <summary>
        /// Writes a single byte and advances the position.
        /// </summary>
        /// <param name="value">The byte value to write.</param>
        public void WriteByte(byte value) => _data[Position++] = value;

        /// <summary>
        /// Copies data from a previous position in the output buffer to the current position.
        /// </summary>
        /// <param name="sourceOffset">The source offset within the output buffer.</param>
        /// <param name="count">Number of bytes to copy.</param>
        /// <remarks>
        /// This method handles overlapping copy operations correctly, which is essential
        /// for RefPack's back-reference mechanism where the source and destination
        /// regions may overlap.
        /// </remarks>
        public void CopyFromPosition(int sourceOffset, int count)
        {
            var sourceSpan = _data.Slice(sourceOffset, count);
            for (int i = 0; i < count; i++)
            {
                _data[Position + i] = sourceSpan[i];
            }

            Position += count;
        }
    }

    /// <summary>
    /// Context for RefPack decompression operations containing reader and writer state.
    /// </summary>
    /// <param name="compressed">The compressed input data.</param>
    /// <param name="decompressed">The decompressed output buffer.</param>
    /// <remarks>
    /// Encapsulates the decompression state and provides high-level operations
    /// for copying literal bytes and back-references. Uses ref struct for
    /// stack-only allocation.
    /// </remarks>
    private ref struct DecodeContext(ReadOnlySpan<byte> compressed, Span<byte> decompressed)
    {
        /// <summary>
        /// Reader for the compressed input data.
        /// </summary>
        public SpanReader Reader = new(compressed);

        /// <summary>
        /// Writer for the decompressed output data.
        /// </summary>
        public SpanWriter Writer = new(decompressed);

        /// <summary>
        /// Copies literal bytes directly from input to output.
        /// </summary>
        /// <param name="count">Number of literal bytes to copy.</param>
        public void CopyLiteralBytes(uint count)
        {
            for (uint i = 0; i < count; i++)
            {
                Writer.WriteByte(Reader.ReadByte());
            }
        }

        /// <summary>
        /// Copies bytes from a back-reference position in the output buffer.
        /// </summary>
        /// <param name="offset">The source offset within the output buffer.</param>
        /// <param name="count">Number of bytes to copy from the reference.</param>
        public void CopyReferenceBytes(int offset, uint count) =>
            Writer.CopyFromPosition(offset, (int)count);
    }

    /// <summary>
    /// Validates the input data for decompression operations.
    /// </summary>
    /// <param name="compressedData">The compressed data to validate.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the data is invalid or insufficient for decompression.
    /// </exception>
    private void ValidateDecodeInput(ReadOnlySpan<byte> compressedData)
    {
        if (!IsValid(compressedData))
        {
            throw new ArgumentException("Invalid compressed data", nameof(compressedData));
        }

        if (compressedData.Length < 2)
        {
            throw new ArgumentException(
                "Compressed data is too small to read the header.",
                nameof(compressedData)
            );
        }
    }

    /// <summary>
    /// Reads and parses the RefPack header to extract format information and uncompressed size.
    /// </summary>
    /// <param name="context">The decompression context containing reader state.</param>
    /// <returns>The expected size of the uncompressed data in bytes.</returns>
    /// <remarks>
    /// <para>
    /// RefPack headers contain:
    /// <list type="bullet">
    /// <item>2-byte format type with flags</item>
    /// <item>Optional metadata block (3 or 4 bytes) if bit 0x0100 is set</item>
    /// <item>Uncompressed size field (3 or 4 bytes based on bit 0x8000)</item>
    /// </list>
    /// </para>
    /// <para>
    /// All multibyte values are stored in big-endian byte order.
    /// </para>
    /// </remarks>
    private static int ReadHeader(ref DecodeContext context)
    {
        uint type = context.Reader.ReadByte();
        type = (type << 8) + context.Reader.ReadByte();

        // Skip the metadata block if present
        if ((type & 0x0100) != 0)
        {
            var skipBytes = (type & 0x80000) != 0 ? 4 : 3;
            context.Reader.Skip(skipBytes);
        }

        // Read uncompressed size
        int length = context.Reader.ReadByte();
        length = (length << 8) + context.Reader.ReadByte();
        length = (length << 8) + context.Reader.ReadByte();

        if ((type & 0x80000) != 0) // 4-byte size field
        {
            length = (length << 8) + context.Reader.ReadByte();
        }

        return length;
    }

    /// <summary>
    /// Processes a short-form control byte (0xxxxxxx) for back-references with 11-bit offsets.
    /// </summary>
    /// <param name="context">The decompression context.</param>
    /// <param name="controlByte">The control byte determining the operation parameters.</param>
    /// <returns>Always returns <c>true</c> to continue processing.</returns>
    /// <remarks>
    /// Short form encoding:
    /// <list type="bullet">
    /// <item>Bits 0-1: Number of literal bytes to copy (0-3)</item>
    /// <item>Bits 2-4: Reference length - 3 (total length 3-10)</item>
    /// <item>Bits 5-6: High bits of reference offset</item>
    /// <item>Next byte: Low 8 bits of reference offset</item>
    /// </list>
    /// </remarks>
    private static bool ProcessShortForm(ref DecodeContext context, byte controlByte)
    {
        var second = context.Reader.ReadByte();
        var literalCount = (uint)(controlByte & 3);

        // Copy literal bytes
        context.CopyLiteralBytes(literalCount);

        // Calculate reference parameters
        var refOffset = context.Writer.Position - 1 - (((controlByte & 0x60) << 3) + second);
        var refLength = (uint)(((controlByte & 0x1C) >> 2) + 3);

        // Copy from reference
        context.CopyReferenceBytes(refOffset, refLength);

        return true; // Continue processing
    }

    /// <summary>
    /// Processes a medium-form control byte (10xxxxxx) for back-references with 14-bit offsets.
    /// </summary>
    /// <param name="context">The decompression context.</param>
    /// <param name="controlByte">The control byte determining the operation parameters.</param>
    /// <returns>Always returns <c>true</c> to continue processing.</returns>
    /// <remarks>
    /// Medium form encoding:
    /// <list type="bullet">
    /// <item>Bits 0-5: Reference length - 4 (total length 4-67)</item>
    /// <item>Next byte bits 6-7: Number of literal bytes to copy (0-3)</item>
    /// <item>Next byte bits 0-5 + third byte: 14-bit reference offset</item>
    /// </list>
    /// </remarks>
    private static bool ProcessMediumForm(ref DecodeContext context, byte controlByte)
    {
        var second = context.Reader.ReadByte();
        var third = context.Reader.ReadByte();
        var literalCount = (uint)(second >> 6);

        // Copy literal bytes
        context.CopyLiteralBytes(literalCount);

        // Calculate reference parameters
        var refOffset = context.Writer.Position - 1 - (((second & 0x3F) << 8) + third);
        var refLength = (uint)((controlByte & 0x3F) + 4);

        // Copy from reference
        context.CopyReferenceBytes(refOffset, refLength);

        return true; // Continue processing
    }

    /// <summary>
    /// Processes a long-form control byte (110xxxxx) for back-references with 17-bit offsets.
    /// </summary>
    /// <param name="context">The decompression context.</param>
    /// <param name="controlByte">The control byte determining the operation parameters.</param>
    /// <returns>Always returns <c>true</c> to continue processing.</returns>
    /// <remarks>
    /// Long form encoding:
    /// <list type="bullet">
    /// <item>Bits 0-1: Number of literal bytes to copy (0-3)</item>
    /// <item>Bits 2-3: High bits of reference length</item>
    /// <item>Bit 4: Highest bit of 17-bit reference offset</item>
    /// <item>Next two bytes: Middle and low bytes of reference offset</item>
    /// <item>Fourth byte: Low 8 bits of reference length + 5</item>
    /// </list>
    /// </remarks>
    private static bool ProcessLongForm(ref DecodeContext context, byte controlByte)
    {
        var second = context.Reader.ReadByte();
        var third = context.Reader.ReadByte();
        var fourth = context.Reader.ReadByte();
        var literalCount = (uint)(controlByte & 3);

        // Copy literal bytes
        context.CopyLiteralBytes(literalCount);

        // Calculate reference parameters
        var refOffset =
            context.Writer.Position
            - 1
            - (((controlByte & 0x10) >> 4 << 16) + (second << 8) + third);

        var refLength = (uint)(((controlByte & 0x0C) >> 2 << 8) + fourth + 5);

        // Copy from reference
        context.CopyReferenceBytes(refOffset, refLength);

        return true; // Continue processing
    }

    /// <summary>
    /// Processes literal data blocks or end-of-file markers (111xxxxx).
    /// </summary>
    /// <param name="context">The decompression context.</param>
    /// <param name="controlByte">The control byte determining the operation type.</param>
    /// <returns>
    /// <c>true</c> if end-of-file was reached; <c>false</c> to continue processing.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This handles two cases based on the computed literal count:
    /// <list type="bullet">
    /// <item>Count ≤ 122: Regular literal block, copy specified number of bytes</item>
    /// <item>Count > 122: End-of-file marker with final literal bytes (0-3 bytes)</item>
    /// </list>
    /// </para>
    /// <para>
    /// The literal count is calculated as ((controlByte &amp; 0x1F) &lt;&lt; 2) + 4.
    /// For EOF, only the lowest 2 bits determine the final literal byte count.
    /// </para>
    /// </remarks>
    private static bool ProcessLiteralOrEof(ref DecodeContext context, byte controlByte)
    {
        var literalCount = (uint)(((controlByte & 0x1F) << 2) + 4);

        if (literalCount <= 122) // Regular literal block
        {
            context.CopyLiteralBytes(literalCount);
            return false; // Continue processing
        }

        // EOF with final literal bytes
        var finalLiterals = (uint)(controlByte & 3);
        context.CopyLiteralBytes(finalLiterals);
        return true; // EOF reached
    }

    /// <summary>
    /// Main decompression loop that processes control bytes and dispatches to appropriate handlers.
    /// </summary>
    /// <param name="context">The decompression context containing reader and writer state.</param>
    /// <remarks>
    /// <para>
    /// The algorithm examines the most significant bits of each control byte to determine
    /// the operation type and dispatches to the appropriate processing method:
    /// <list type="bullet">
    /// <item>0xxxxxxx → <see cref="ProcessShortForm"/></item>
    /// <item>10xxxxxx → <see cref="ProcessMediumForm"/></item>
    /// <item>110xxxxx → <see cref="ProcessLongForm"/></item>
    /// <item>111xxxxx → <see cref="ProcessLiteralOrEof"/></item>
    /// </list>
    /// </para>
    /// <para>
    /// The loop continues until an end-of-file marker is encountered.
    /// </para>
    /// </remarks>
    private static void DecompressData(ref DecodeContext context)
    {
        while (true)
        {
            var controlByte = context.Reader.ReadByte();

            // Short form
            if ((controlByte & 0x80) == 0 && ProcessShortForm(ref context, controlByte))
            {
                continue;
            }

            // Medium form
            if ((controlByte & 0x40) == 0 && ProcessMediumForm(ref context, controlByte))
            {
                continue;
            }

            // Long form
            if ((controlByte & 0x20) == 0 && ProcessLongForm(ref context, controlByte))
            {
                continue;
            }

            // Literal or EOF
            if (ProcessLiteralOrEof(ref context, controlByte))
            {
                break; // EOF reached
            }
        }
    }

    /// <summary>
    /// Decompresses RefPack-compressed data into the provided output buffer.
    /// </summary>
    /// <param name="compressedData">The source compressed data.</param>
    /// <param name="decompressedData">
    /// The destination buffer for decompressed bytes. Must be at least as large
    /// as the value returned by <see cref="GetSize"/>.
    /// </param>
    /// <returns>The number of bytes written to the output buffer.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the input data is invalid or the output buffer is insufficient.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when this codex does not support decoding operations.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method implements the complete RefPack decompression algorithm:
    /// <list type="number">
    /// <item>Validates input data format and size</item>
    /// <item>Reads and parses the RefPack header</item>
    /// <item>Processes compressed data chunks according to control byte patterns</item>
    /// <item>Returns the actual number of decompressed bytes</item>
    /// </list>
    /// </para>
    /// <para>
    /// The implementation uses safe managed code without pointer arithmetic,
    /// relying on span-based operations for optimal performance.
    /// </para>
    /// </remarks>
    public int Decode(ReadOnlySpan<byte> compressedData, Span<byte> decompressedData)
    {
        ValidateDecodeInput(compressedData);

        var context = new DecodeContext(compressedData, decompressedData);
        var uncompressedSize = ReadHeader(ref context);

        DecompressData(ref context);

        return uncompressedSize;
    }
}
