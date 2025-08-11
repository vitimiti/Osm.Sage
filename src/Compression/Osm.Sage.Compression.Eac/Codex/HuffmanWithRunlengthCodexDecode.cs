using System.Numerics;

namespace Osm.Sage.Compression.Eac.Codex;

public partial class HuffmanWithRunlengthCodex
{
    /// <summary>Magic value indicating 32-bit delta encoding type 1.</summary>
    private const uint FlagOx32Fb = 0x32FB;

    /// <summary>Magic value indicating 32-bit delta encoding type 1 (alternate).</summary>
    private const uint FlagOxB2Fb = 0xB2FB;

    /// <summary>Magic value indicating 32-bit delta encoding type 2.</summary>
    private const uint FlagOx34Fb = 0x34FB;

    /// <summary>Magic value indicating 32-bit delta encoding type 2 (alternate).</summary>
    private const uint FlagOxB4Fb = 0xB4FB;

    /// <summary>Flag indicating the header uses 4-byte size fields instead of smaller ones.</summary>
    private const uint Type4ByteSize = 0x8000;

    /// <summary>Flag indicating to skip reading the uncompressed length field.</summary>
    private const uint TypeSkipLength = 0x0100;

    /// <summary>Special marker value used in quick lookup tables to indicate clue symbols.</summary>
    private const int ClueMarker = 96;

    /// <summary>Size of quick lookup tables for 8-bit indexed access.</summary>
    private const int QuickTableSize = 256;

    /// <summary>
    /// Maintains the current state of the bit-stream decoder during decompression.
    /// Uses a ref struct for stack allocation and zero-copy span access.
    /// </summary>
    private ref struct DecodeContext
    {
        /// <summary>The source compressed data span.</summary>
        public ReadOnlySpan<byte> Source;

        /// <summary>Current reading position in the source data.</summary>
        public int SourcePosition;

        /// <summary>The destination buffer for decompressed data.</summary>
        public Span<byte> Destination;

        /// <summary>Current writing position in the destination buffer.</summary>
        public int DestinationPosition;

        /// <summary>Number of bits remaining in the current bit buffer before refill needed.</summary>
        public int BitsLeft;

        /// <summary>Current bit buffer for bit-oriented reading (32 bits).</summary>
        public uint Bits;

        /// <summary>Unshifted bits buffer used during bit refill operations.</summary>
        public uint BitsUnshifted;

        /// <summary>Temporary value register for the bit extraction operations.</summary>
        public uint V;
    }

    /// <summary>
    /// Contains all Huffman decoding tables and metadata required for symbol reconstruction.
    /// Uses arrays instead of spans to allow passing between methods without scope restrictions.
    /// </summary>
    private struct HuffmanTables
    {
        /// <summary>Number of symbols for each bit length (indexed by bit length).</summary>
        public int[] BitNumTable;

        /// <summary>Base offset values for symbol lookup (indexed by bit length).</summary>
        public uint[] DeltaTable;

        /// <summary>Comparison thresholds for the bit length determination.</summary>
        public uint[] CompareTable;

        /// <summary>Maps symbol indices to actual byte values.</summary>
        public byte[] CodeTable;

        /// <summary>Quick lookup table mapping 8-bit prefixes to symbol values.</summary>
        public byte[] QuickCodeTable;

        /// <summary>Quick lookup table mapping 8-bit prefixes to bit lengths.</summary>
        public byte[] QuickLengthTable;

        /// <summary>Maximum bit length used in the Huffman tree.</summary>
        public int MostBits;

        /// <summary>The bit length of the special clue symbol.</summary>
        public int ClueLength;

        /// <summary>The special clue symbol value used for run-length sequences.</summary>
        public byte Clue;
    }

    /// <summary>
    /// Initializes a new "decode" context with the given source and destination spans.
    /// Sets up the initial bit buffer state for reading bit-oriented data.
    /// </summary>
    /// <param name="source">The compressed data to read from.</param>
    /// <param name="destination">The buffer to write decompressed data to.</param>
    /// <returns>A new "decode" context ready for use.</returns>
    private static DecodeContext InitializeContext(
        ReadOnlySpan<byte> source,
        Span<byte> destination
    ) =>
        new()
        {
            Source = source,
            SourcePosition = 0,
            Destination = destination,
            DestinationPosition = 0,
            BitsLeft = -16, // Initial state requires immediate refill
            Bits = 0,
            BitsUnshifted = 0,
            V = 0,
        };

    /// <summary>
    /// Refills the bit buffer by reading up to 16 bits (2 bytes) from the source stream.
    /// Handles end-of-stream gracefully by reading available bytes only.
    /// </summary>
    /// <param name="context">The "decode" context containing source data and the bit state.</param>
    private static void Get16Bits(ref DecodeContext context)
    {
        if (context.SourcePosition < context.Source.Length)
        {
            context.BitsUnshifted =
                context.Source[context.SourcePosition] | (context.BitsUnshifted << 8);

            context.SourcePosition++;
        }

        if (context.SourcePosition >= context.Source.Length)
        {
            return;
        }

        context.BitsUnshifted =
            context.Source[context.SourcePosition] | (context.BitsUnshifted << 8);

        context.SourcePosition++;
    }

    /// <summary>
    /// Extracts exactly n bits from the bit stream and converts to the specified numeric type.
    /// Automatically refills the bit buffer when needed.
    /// </summary>
    /// <typeparam name="T">The target numeric type implementing INumber.</typeparam>
    /// <param name="context">The "decode" context containing bit stream state.</param>
    /// <param name="val">Reference to store the extracted value.</param>
    /// <param name="n">Number of bits to extract (0-32).</param>
    /// <exception cref="ArgumentException">Thrown when attempting to read beyond available data.</exception>
    private static void GetBits<T>(ref DecodeContext context, ref T val, int n)
        where T : INumber<T>
    {
        if (n is < 0 or > 32)
        {
            throw new ArgumentException(
                $"Invalid bit count: {n}. Must be between 0 and 32.",
                nameof(n)
            );
        }

        if (n != 0)
        {
            val = T.CreateTruncating(context.Bits >> (32 - n));
            context.Bits <<= n;
            context.BitsLeft -= n;
        }

        if (context.BitsLeft >= 0)
        {
            return;
        }

        if (context.SourcePosition >= context.Source.Length)
        {
            throw new ArgumentException(
                "Unexpected end of compressed data while reading bits.",
                nameof(context)
            );
        }

        Get16Bits(ref context);
        context.Bits = context.BitsUnshifted << (-context.BitsLeft);
        context.BitsLeft += 16;
    }

    /// <summary>
    /// Reads and parses the compressed data header to extract the format type and uncompressed length.
    /// Handles multiple header formats based on size and skip flags.
    /// </summary>
    /// <param name="context">The "decode" context for bit stream access.</param>
    /// <returns>A tuple containing the format type flags and uncompressed data length.</returns>
    /// <exception cref="ArgumentException">Thrown when the header data is not enough or corrupted.</exception>
    private static (uint type, int ulen) ReadHeader(ref DecodeContext context)
    {
        if (context.Source.Length < 2)
        {
            throw new ArgumentException(
                "Compressed data is too small to contain a valid header.",
                nameof(context)
            );
        }

        GetBits(ref context, ref context.V, 0);

        uint type = 0;
        GetBits(ref context, ref type, 16);

        int length = 0;
        if ((type & Type4ByteSize) != 0) // 4-byte size field
        {
            if (context.Source.Length < 8)
            {
                throw new ArgumentException(
                    "Compressed data is too small for 4-byte size header format.",
                    nameof(context)
                );
            }

            if ((type & TypeSkipLength) != 0) // Skip length
            {
                GetBits(ref context, ref context.V, 16);
                GetBits(ref context, ref context.V, 16);
            }

            type &= ~TypeSkipLength;
            GetBits(ref context, ref context.V, 16);
        }
        else
        {
            if (context.Source.Length < 5)
            {
                throw new ArgumentException(
                    "Compressed data is too small for standard header format.",
                    nameof(context)
                );
            }

            if ((type & TypeSkipLength) != 0) // Skip length
            {
                GetBits(ref context, ref context.V, 8);
                GetBits(ref context, ref context.V, 16);
            }

            type &= ~TypeSkipLength;
            GetBits(ref context, ref context.V, 8);
        }

        GetBits(ref context, ref length, 16);
        length |= (int)(context.V << 16);

        if (length < 0)
        {
            throw new ArgumentException(
                $"Invalid uncompressed length in header: {length}.",
                nameof(context)
            );
        }

        return (type, length);
    }

    /// <summary>
    /// Reads a variable-length encoded integer from the bit stream.
    /// Uses a custom encoding scheme optimized for small values.
    /// </summary>
    /// <typeparam name="T">The target numeric type implementing INumber.</typeparam>
    /// <param name="context">The "decode" context for bit stream access.</param>
    /// <param name="val">Reference to store the decoded integer value.</param>
    private static void GetNum<T>(ref DecodeContext context, ref T val)
        where T : INumber<T>
    {
        if ((int)context.Bits < 0)
        {
            GetBits(ref context, ref context.V, 3);
            val = T.CreateTruncating(context.V) - T.CreateTruncating(4);
            return;
        }

        int n =
            (context.Bits >> 16) != 0
                ? CountLeadingBits(ref context)
                : CountBitsWithGetBits(ref context, ref val);

        if (n > 16)
        {
            HandleLargeNumber(ref context, ref val, n);
        }
        else
        {
            GetBits(ref context, ref val, n);
            val += T.CreateTruncating((1U << n) - 4U);
        }
    }

    /// <summary>
    /// Counts leading bits by shifting until a zero bit is encountered.
    /// Used for variable-length integer decoding.
    /// </summary>
    /// <param name="context">The "decode" context for bit manipulation.</param>
    /// <returns>The number of leading bits counted.</returns>
    private static int CountLeadingBits(ref DecodeContext context)
    {
        int n = 2;
        do
        {
            context.Bits <<= 1;
            n++;
        } while ((int)context.Bits >= 0);

        context.Bits <<= 1;
        context.BitsLeft -= n - 1;
        return n;
    }

    /// <summary>
    /// Counts bits by reading individual bits until zero is encountered.
    /// Alternative method for variable-length integer decoding.
    /// </summary>
    /// <typeparam name="T">The target numeric type implementing INumber.</typeparam>
    /// <param name="context">The "decode" context for bit stream access.</param>
    /// <param name="val">Temporary value storage for the bit reading.</param>
    /// <returns>The number of bits counted.</returns>
    private static int CountBitsWithGetBits<T>(ref DecodeContext context, ref T val)
        where T : INumber<T>
    {
        int n = 2;
        do
        {
            n++;
            GetBits(ref context, ref val, 1);
        } while (val != T.Zero);

        return n;
    }

    /// <summary>
    /// Handles decoding of large variable-length integers that exceed 16 bits.
    /// Reads the value in two parts to handle the extended range.
    /// </summary>
    /// <typeparam name="T">The target numeric type implementing INumber.</typeparam>
    /// <param name="context">The "decode" context for bit stream access.</param>
    /// <param name="val">Reference to store the decoded large integer.</param>
    /// <param name="n">Total number of bits in the encoded integer.</param>
    private static void HandleLargeNumber<T>(ref DecodeContext context, ref T val, int n)
        where T : INumber<T>
    {
        GetBits(ref context, ref val, n - 16);

        uint v1 = 0;
        GetBits(ref context, ref v1, 16);

        var valAsUint = uint.CreateTruncating(val);
        var result = v1 | (valAsUint << 16);
        val = T.CreateTruncating(result) + T.CreateTruncating((1U << n) - 4U);
    }

    /// <summary>
    /// Constructs the complete Huffman decoding tables from embedded metadata in the compressed stream.
    /// This includes the bit length tables, comparison thresholds, and the symbol mapping table.
    /// </summary>
    /// <param name="context">The "decode" context for reading table data.</param>
    /// <param name="tables">The Huffman tables structure to populate.</param>
    /// <exception cref="ArgumentException">Thrown when the Huffman table data is invalid or corrupted.</exception>
    private static void BuildHuffmanTables(ref DecodeContext context, ref HuffmanTables tables)
    {
        // Read clue byte
        uint clueValue = 0;
        GetBits(ref context, ref clueValue, 8);
        tables.Clue = (byte)clueValue;

        // Decode the bit number tables
        int numChars = 0;
        int numBits = 1;
        uint baseCompare = 0;

        do
        {
            if (numBits >= tables.BitNumTable.Length)
            {
                throw new ArgumentException(
                    "Huffman table bit length exceeds maximum supported value.",
                    nameof(context)
                );
            }

            baseCompare <<= 1;
            tables.DeltaTable[numBits] = baseCompare - (uint)numChars;

            int bitNum = 0;
            GetNum(ref context, ref bitNum);

            if (bitNum < 0)
            {
                throw new ArgumentException(
                    $"Invalid bit number in Huffman table: {bitNum}.",
                    nameof(context)
                );
            }

            tables.BitNumTable[numBits] = bitNum;

            numChars += bitNum;
            if (numChars > QuickTableSize)
            {
                throw new ArgumentException(
                    $"Too many characters in Huffman alphabet: {numChars}. Maximum is {QuickTableSize}.",
                    nameof(context)
                );
            }

            baseCompare += (uint)bitNum;

            uint compare = 0;
            if (bitNum != 0) // Left justify compare
            {
                compare = (baseCompare << (16 - numBits)) & 0xFFFF;
            }

            tables.CompareTable[numBits++] = compare;
        } while (tables.BitNumTable[numBits - 1] == 0 || tables.CompareTable[numBits - 1] != 0);

        tables.CompareTable[numBits - 1] = 0xFFFFFFFF; // Force match on most bits
        tables.MostBits = numBits - 1;

        if (tables.MostBits <= 0)
        {
            throw new ArgumentException(
                "Invalid Huffman table: no valid bit lengths found.",
                nameof(context)
            );
        }

        // Build leapfrog code table
        BuildLeapfrogTable(ref context, ref tables, numChars);
    }

    /// <summary>
    /// Constructs the symbol-to-value mapping table using a leapfrog algorithm.
    /// This determines which byte values correspond to each Huffman symbol.
    /// </summary>
    /// <param name="context">The "decode" context for reading symbol mapping data.</param>
    /// <param name="tables">The Huffman tables structure to populate.</param>
    /// <param name="numChars">Total number of symbols in the alphabet.</param>
    /// <exception cref="ArgumentException">Thrown when the leapfrog table data is invalid.</exception>
    private static void BuildLeapfrogTable(
        ref DecodeContext context,
        ref HuffmanTables tables,
        int numChars
    )
    {
        if (numChars is <= 0 or > QuickTableSize)
        {
            throw new ArgumentException(
                $"Invalid character count for leapfrog table: {numChars}.",
                nameof(numChars)
            );
        }

        Span<byte> leap = stackalloc byte[QuickTableSize];
        byte nextChar = unchecked((byte)-1);

        for (int i = 0; i < numChars; i++)
        {
            int leapDelta = 0;
            GetNum(ref context, ref leapDelta);
            leapDelta++;

            if (leapDelta <= 0)
            {
                throw new ArgumentException(
                    $"Invalid leap delta in leapfrog table: {leapDelta - 1}.",
                    nameof(context)
                );
            }

            int iterations = 0;
            do
            {
                nextChar++;
                if (leap[nextChar] == 0)
                {
                    leapDelta--;
                }

                iterations++;
                if (iterations > QuickTableSize)
                {
                    throw new ArgumentException(
                        "Leapfrog table construction failed: infinite loop detected.",
                        nameof(context)
                    );
                }
            } while (leapDelta != 0);

            leap[nextChar] = 1;
            tables.CodeTable[i] = nextChar;
        }
    }

    /// <summary>
    /// Builds optimized quick lookup tables for fast decoding of short Huffman codes.
    /// Maps 8-bit prefixes directly to symbols and bit lengths for performance.
    /// </summary>
    /// <param name="tables">The Huffman tables structure containing symbol data.</param>
    private static void BuildQuickTables(ref HuffmanTables tables)
    {
        Array.Fill(tables.QuickLengthTable, (byte)64);

        int codeIndex = 0;
        int quickIndex = 0;

        for (uint bits = 1; bits <= tables.MostBits; bits++)
        {
            int bitNum = tables.BitNumTable[(int)bits];
            if (bits >= 9)
                break;

            int numBitEntries = 1 << (int)(8 - bits);

            for (int b = 0; b < bitNum; b++)
            {
                int nextCode = tables.CodeTable[codeIndex++];
                int nextLength = (int)bits;

                if (nextCode == tables.Clue)
                {
                    tables.ClueLength = (int)bits;
                    nextLength = ClueMarker;
                }

                for (int i = 0; i < numBitEntries; i++)
                {
                    tables.QuickCodeTable[quickIndex] = (byte)nextCode;
                    tables.QuickLengthTable[quickIndex] = (byte)nextLength;
                    quickIndex++;
                }
            }
        }
    }

    /// <summary>
    /// Safely writes a byte to the destination buffer, respecting bounds' checking.
    /// </summary>
    /// <param name="context">The "decode" context containing the destination buffer.</param>
    /// <param name="value">The byte value to write.</param>
    /// <exception cref="ArgumentException">Thrown when attempting to write beyond the destination buffer.</exception>
    private static void WriteDestinationByte(ref DecodeContext context, byte value)
    {
        if (context.DestinationPosition >= context.Destination.Length)
        {
            throw new ArgumentException(
                "Destination buffer is too small for the decompressed data.",
                nameof(context)
            );
        }

        context.Destination[context.DestinationPosition] = value;
        context.DestinationPosition++;
    }

    /// <summary>
    /// Safely reads a byte from the destination buffer at the specified position.
    /// Used for run-length encoding where we need to reference previously written data.
    /// </summary>
    /// <param name="context">The "decode" context containing the destination buffer.</param>
    /// <param name="position">The position to read from.</param>
    /// <returns>The byte value at the position, or 0 if out of bounds.</returns>
    private static byte ReadDestinationByte(ref DecodeContext context, int position) =>
        position >= 0 && position < context.Destination.Length
            ? context.Destination[position]
            : (byte)0;

    /// <summary>
    /// Performs optimized quick decoding using 8-bit lookup tables.
    /// Handles the fast path for short Huffman codes and manages the bit buffer state.
    /// </summary>
    /// <param name="context">The "decode" context for bit stream access.</param>
    /// <param name="tables">The Huffman tables containing quick lookup data.</param>
    /// <param name="firstIteration">True if this is the first call in the "decode" loop.</param>
    /// <returns>The number of bits consumed for the current symbol.</returns>
    private static int ProcessQuickDecode(
        ref DecodeContext context,
        ref HuffmanTables tables,
        bool firstIteration
    )
    {
        if (firstIteration)
        {
            int initialBits = tables.QuickLengthTable[context.Bits >> 24];
            context.BitsLeft -= initialBits;
            return initialBits;
        }

        // Quick 8-bit decoding loop
        while (context.BitsLeft >= 0)
        {
            WriteDestinationByte(ref context, tables.QuickCodeTable[context.Bits >> 24]);
            Get16Bits(ref context);
            context.Bits = context.BitsUnshifted << (16 - context.BitsLeft);

            int numBits = tables.QuickLengthTable[context.Bits >> 24];
            context.BitsLeft -= numBits;

            if (context.BitsLeft >= 0)
            {
                ProcessQuickDecodeSequence(ref context, ref tables, ref numBits);
            }

            context.BitsLeft += 16;
        }

        int finalBits = tables.QuickLengthTable[context.Bits >> 24];
        context.BitsLeft = context.BitsLeft - 16 + finalBits;
        return finalBits;
    }

    /// <summary>
    /// Processes an unrolled sequence of quick decode operations for performance.
    /// Handles multiple symbols in a tight loop when sufficient bits are available.
    /// </summary>
    /// <param name="context">The "decode" context for bit stream access.</param>
    /// <param name="tables">The Huffman tables containing quick lookup data.</param>
    /// <param name="numBits">Reference to the current bit count, updated during processing.</param>
    private static void ProcessQuickDecodeSequence(
        ref DecodeContext context,
        ref HuffmanTables tables,
        ref int numBits
    )
    {
        // Unrolled loop for performance
        for (int unroll = 0; unroll < 4; unroll++)
        {
            WriteDestinationByte(ref context, tables.QuickCodeTable[context.Bits >> 24]);
            context.Bits <<= numBits;

            numBits = tables.QuickLengthTable[context.Bits >> 24];
            context.BitsLeft -= numBits;
            if (context.BitsLeft < 0)
                break;
        }
    }

    /// <summary>
    /// Decodes a 16-bit Huffman symbol using the comparison tables for longer codes.
    /// This handles the slow path when quick lookup tables are not enough.
    /// </summary>
    /// <param name="context">The "decode" context for bit stream access.</param>
    /// <param name="tables">The Huffman tables containing comparison data.</param>
    /// <param name="numBits">The number of bits to process for this symbol.</param>
    /// <returns>The decoded byte symbol.</returns>
    /// <exception cref="ArgumentException">Thrown when the symbol cannot be decoded or is invalid.</exception>
    private static byte Decode16BitSymbol(
        ref DecodeContext context,
        ref HuffmanTables tables,
        int numBits
    )
    {
        uint compare;

        if (numBits != ClueMarker)
        {
            compare = context.Bits >> 16; // 16 bit left justified compare
            numBits = 8;
            do
            {
                numBits++;
                if (numBits > tables.MostBits)
                {
                    throw new ArgumentException(
                        "Invalid Huffman symbol: bit length exceeds maximum.",
                        nameof(context)
                    );
                }
            } while (compare >= tables.CompareTable[numBits]);
        }
        else
        {
            numBits = tables.ClueLength;
            if (numBits <= 0 || numBits > tables.MostBits)
            {
                throw new ArgumentException($"Invalid clue length: {numBits}.", nameof(context));
            }
        }

        compare = context.Bits >> (32 - numBits);
        context.Bits <<= numBits;
        context.BitsLeft -= numBits;

        uint symbolIndex = compare - tables.DeltaTable[numBits];
        if (symbolIndex >= tables.CodeTable.Length)
        {
            throw new ArgumentException($"Invalid symbol index: {symbolIndex}.", nameof(context));
        }

        return tables.CodeTable[symbolIndex];
    }

    /// <summary>
    /// Handles special clue symbols that indicate run-length sequences or explicit bytes.
    /// Clue symbols are special markers in the Huffman stream that trigger different processing.
    /// </summary>
    /// <param name="context">The "decode" context for bit stream access and destination writing.</param>
    /// <returns>True to continue decoding, false if end-of-file is reached.</returns>
    private static bool HandleClueSymbol(ref DecodeContext context)
    {
        // Handle the run-length sequence
        int runLength = 0;
        GetNum(ref context, ref runLength);

        if (runLength != 0)
        {
            byte repeatByte = ReadDestinationByte(ref context, context.DestinationPosition - 1);
            for (int r = 0; r < runLength; r++)
            {
                WriteDestinationByte(ref context, repeatByte);
            }
            return true;
        }

        // Check for the EOF
        GetBits(ref context, ref context.V, 1);
        if (context.V != 0)
        {
            return false; // EOF
        }

        // Handle explicit byte
        uint explicitByte = 0;
        GetBits(ref context, ref explicitByte, 8);
        WriteDestinationByte(ref context, (byte)explicitByte);
        return true;
    }

    /// <summary>
    /// The main decoding loop that processes the Huffman-encoded data stream.
    /// Combines quick decoding, symbol reconstruction, and run-length expansion.
    /// </summary>
    /// <param name="context">The "decode" context for stream processing.</param>
    /// <param name="tables">The Huffman tables for symbol decoding.</param>
    private static void DecodeMainLoop(ref DecodeContext context, ref HuffmanTables tables)
    {
        bool firstIteration = true;
        bool continueDecoding = true;

        while (continueDecoding)
        {
            int numBits = ProcessQuickDecode(ref context, ref tables, firstIteration);
            firstIteration = false;

            byte code = Decode16BitSymbol(ref context, ref tables, numBits);

            if (code != tables.Clue && context.BitsLeft >= 0)
            {
                WriteDestinationByte(ref context, code);
            }
            else
            {
                continueDecoding = HandleClueSymbol(ref context);
            }
        }
    }

    /// <summary>
    /// Applies delta decoding transformation to the decompressed data based on the format type.
    /// Delta decoding reverses a preprocessing step that stores differences between values.
    /// </summary>
    /// <param name="context">The "decode" context containing the destination buffer.</param>
    /// <param name="type">The format type flags indicating which delta algorithm to use.</param>
    /// <param name="length">The number of bytes to process.</param>
    private static void ApplyDeltaDecoding(ref DecodeContext context, uint type, int length)
    {
        switch (type)
        {
            case FlagOx32Fb or FlagOxB2Fb:
            {
                int accumulator = 0;
                for (int pos = 0; pos < length; pos++)
                {
                    accumulator += context.Destination[pos];
                    context.Destination[pos] = (byte)accumulator;
                }

                break;
            }
            case FlagOx34Fb or FlagOxB4Fb:
            {
                int accumulator = 0;
                int nextChar = 0;
                for (int pos = 0; pos < length; pos++)
                {
                    accumulator += context.Destination[pos];
                    nextChar += accumulator;
                    context.Destination[pos] = (byte)nextChar;
                }

                break;
            }
        }
    }

    /// <summary>
    /// Decompresses the provided compressed data into the destination span.
    /// This is the main entry point for the Huffman with the run-length decompression algorithm.
    /// </summary>
    /// <param name="compressedData">The source compressed data to decompress.</param>
    /// <param name="decompressedData">The destination buffer for the decompressed output.</param>
    /// <returns>The number of bytes written to the destination buffer.</returns>
    /// <exception cref="ArgumentException">Thrown if the compressed data is invalid or the destination buffer is too small.</exception>
    /// <remarks>
    /// The decompression process follows these steps:
    /// <list type="number">
    /// <item>Parse the header to determine format and length</item>
    /// <item>Build Huffman decoding tables from embedded metadata</item>
    /// <item>Generate quick lookup tables for performance</item>
    /// <item>Execute the main decoding loop with run-length expansion</item>
    /// <item>Apply delta decoding if required by format flags</item>
    /// </list>
    /// </remarks>
    public int Decode(ReadOnlySpan<byte> compressedData, Span<byte> decompressedData)
    {
        if (compressedData.Length < 5)
        {
            throw new ArgumentException(
                "Compressed data is too small to be valid.",
                nameof(compressedData)
            );
        }

        var context = InitializeContext(compressedData, decompressedData);

        (uint type, int length) = ReadHeader(ref context);

        if (length > decompressedData.Length)
        {
            throw new ArgumentException(
                $"Destination buffer is too small. Required: {length}, Available: {decompressedData.Length}.",
                nameof(decompressedData)
            );
        }

        var tables = new HuffmanTables
        {
            BitNumTable = new int[16],
            DeltaTable = new uint[16],
            CompareTable = new uint[16],
            CodeTable = new byte[QuickTableSize],
            QuickCodeTable = new byte[QuickTableSize],
            QuickLengthTable = new byte[QuickTableSize],
        };

        try
        {
            BuildHuffmanTables(ref context, ref tables);
            BuildQuickTables(ref tables);
            DecodeMainLoop(ref context, ref tables);
            ApplyDeltaDecoding(ref context, type, length);
        }
        catch (IndexOutOfRangeException ex)
        {
            throw new ArgumentException(
                "Corrupted compressed data: array index out of bounds during decoding.",
                nameof(compressedData),
                ex
            );
        }
        catch (OverflowException ex)
        {
            throw new ArgumentException(
                "Corrupted compressed data: arithmetic overflow during decoding.",
                nameof(compressedData),
                ex
            );
        }

        return length;
    }
}
