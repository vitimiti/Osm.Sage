namespace Osm.Sage.Compression.Eac.Codex;

public partial class HuffmanWithRunlengthCodex
{
    /// <summary>
    /// Maximum cost value used in compression calculations to represent infeasible options.
    /// </summary>
    private const int MaxCode = 32_000;

    /// <summary>
    /// Maximum number of tree nodes supported in the Huffman tree construction.
    /// </summary>
    private const int TreeNodeCapacity = 520;

    /// <summary>
    /// Total number of possible byte values (0-255).
    /// </summary>
    private const int ByteValueCount = 256;

    /// <summary>
    /// Maximum number of bits allowed in Huffman code bit lengths.
    /// </summary>
    private const int MaxCodeBits = 16;

    /// <summary>
    /// Size of the repeat table used for variable-length number encoding.
    /// </summary>
    private const int RepeatTableSize = 252;

    /// <summary>
    /// Number of delta encoding passes to apply during data preprocessing.
    /// </summary>
    private int _deltaBytesRuns = 3;

    /// <summary>
    /// Manages sequential byte writing to a span-based output buffer with overflow protection.
    /// </summary>
    /// <param name="data">The output span to write compressed data to.</param>
    /// <remarks>
    /// <para>
    /// This ref struct provides a safe interface for sequential byte writing operations
    /// during the compression process. It automatically tracks the current position
    /// and prevents buffer overruns through bounds' checking.
    /// </para>
    /// <para>
    /// Being a ref struct ensures stack-only allocation, avoiding heap pressure during
    /// compression operations while maintaining memory safety.
    /// </para>
    /// </remarks>
    private ref struct OutputBuffer(Span<byte> data)
    {
        /// <summary>
        /// The target output span for compressed data.
        /// </summary>
        private readonly Span<byte> _data = data;

        /// <summary>
        /// Current write position within the output buffer.
        /// </summary>
        public int Position = 0;

        /// <summary>
        /// Writes a single byte to the buffer and advances the position counter.
        /// </summary>
        /// <param name="value">The byte value to write.</param>
        /// <exception cref="ArgumentException">Thrown when the buffer is full.</exception>
        /// <remarks>
        /// This method provides safe byte writing with automatic bounds checking
        /// to prevent buffer overflow conditions during compression.
        /// </remarks>
        public void WriteByte(byte value)
        {
            if (Position >= _data.Length)
            {
                throw new ArgumentException("Output buffer too small.");
            }

            _data[Position++] = value;
        }
    }

    /// <summary>
    /// Maintains all compression state, intermediate data structures, and configuration
    /// throughout the entire compression process.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This comprehensive state container manages multiple categories of compression data:
    /// <list type="bullet">
    /// <item><b>Huffman Tables:</b> Frequency analysis, bit lengths, and code patterns</item>
    /// <item><b>Tree Construction:</b> Node relationships and tree building state</item>
    /// <item><b>The bit Packing:</b> Current bit buffer state and working patterns</item>
    /// <item><b>Input Processing:</b> Source data and analysis results</item>
    /// <item><b>Special Encoding:</b> Clue bytes and delta encoding parameters</item>
    /// </list>
    /// </para>
    /// <para>
    /// The context uses span-based arrays for performance and memory efficiency,
    /// allowing the algorithm to process large datasets without excessive allocations.
    /// </para>
    /// </remarks>
    private ref struct CompressionContext
    {
        // Huffman table data

        /// <summary>
        /// Leapfrog table used for efficient Huffman code construction and output.
        /// </summary>
        public Span<sbyte> LeapfrogTable;

        /// <summary>
        /// Frequency counts for different encoding contexts (768 = 3 * 256 for different contexts).
        /// </summary>
        public Span<uint> FrequencyCount;

        /// <summary>
        /// Count of codes for each bit length in the Huffman tree.
        /// </summary>
        public Span<uint> BitLengthCount;

        /// <summary>
        /// Bit counts for variable-length number encoding of different repeat lengths.
        /// </summary>
        public Span<uint> RepeatBits;

        /// <summary>
        /// Base values for variable-length number encoding of different repeat lengths.
        /// </summary>
        public Span<uint> RepeatBase;

        // Tree construction

        /// <summary>
        /// Left child node IDs for tree construction.
        /// </summary>
        public Span<uint> TreeLeft;

        /// <summary>
        /// Right child node IDs for tree construction.
        /// </summary>
        public Span<uint> TreeRight;

        /// <summary>
        /// The bit length assigned to each code in the Huffman tree.
        /// </summary>
        public Span<uint> CodeBitLength;

        /// <summary>
        /// Final bit patterns for each Huffman code.
        /// </summary>
        public Span<uint> CodePattern;

        /// <summary>
        /// Precomputed bit masks for efficient bit operations (BitMasks[n] = 2^n-1).
        /// </summary>
        public Span<uint> BitMasks;

        // The bit packing state

        /// <summary>
        /// Number of bits currently accumulated in the working pattern.
        /// </summary>
        public uint PackedBits;

        /// <summary>
        /// Current bit pattern being assembled for output.
        /// </summary>
        public uint WorkingPattern;

        // Input data

        /// <summary>
        /// The input data buffer being compressed.
        /// </summary>
        public ReadOnlySpan<byte> InputBuffer;

        // Code analysis results

        /// <summary>
        /// Maximum bit length used in the final Huffman tree.
        /// </summary>
        public uint MaxBitLength;

        /// <summary>
        /// Number of active (non-zero frequency) codes in the tree.
        /// </summary>
        public uint ActiveCodeCount;

        // Special encoding bytes (clue bytes)

        /// <summary>
        /// Primary clue byte used for run-length encoding sequences.
        /// </summary>
        public uint PrimaryClue;

        /// <summary>
        /// Delta clue byte used for delta encoding sequences.
        /// </summary>
        public uint DeltaClue;

        /// <summary>
        /// Number of consecutive bytes allocated for primary clue operations.
        /// </summary>
        public uint PrimaryClueCount;

        /// <summary>
        /// Number of consecutive bytes allocated for delta clue operations.
        /// </summary>
        public uint DeltaClueCount;

        /// <summary>
        /// Minimum delta value allowed in delta encoding.
        /// </summary>
        public int MinDelta;

        /// <summary>
        /// Maximum delta value allowed in delta encoding.
        /// </summary>
        public int MaxDelta;

        // Sorting and patterns

        /// <summary>
        /// Codes sorted by bit length for efficient pattern assignment.
        /// </summary>
        public Span<uint> SortedCodes;

        /// <summary>
        /// Creates and initializes a new compression context with all required data structures.
        /// </summary>
        /// <returns>A fully initialized compression context ready for use.</returns>
        /// <remarks>
        /// <para>
        /// This factory method allocates all necessary arrays and spans for the compression
        /// process, including:
        /// <list type="bullet">
        /// <item>Huffman coding tables and frequency counters</item>
        /// <item>Tree construction workspace arrays</item>
        /// <item>Variable-length encoding lookup tables</item>
        /// <item>Bit manipulation and pattern storage</item>
        /// </list>
        /// </para>
        /// <para>
        /// The allocated sizes are optimized for the maximum requirements of the algorithm,
        /// ensuring sufficient space for all compression scenarios.
        /// </para>
        /// </remarks>
        public static CompressionContext Create()
        {
            return new CompressionContext
            {
                LeapfrogTable = new sbyte[ByteValueCount],
                FrequencyCount = new uint[768], // 256 + 256 + 256 for different contexts
                BitLengthCount = new uint[MaxCodeBits + 1],
                RepeatBits = new uint[RepeatTableSize],
                RepeatBase = new uint[RepeatTableSize],
                TreeLeft = new uint[TreeNodeCapacity],
                TreeRight = new uint[TreeNodeCapacity],
                CodeBitLength = new uint[ByteValueCount],
                CodePattern = new uint[ByteValueCount],
                BitMasks = new uint[17],
                SortedCodes = new uint[ByteValueCount],
            };
        }
    }

    /// <summary>
    /// Gets or sets the number of delta encoding passes applied during preprocessing.
    /// </summary>
    /// <value>
    /// The number of delta encoding passes. Values less than 3 are automatically adjusted to 3.
    /// </value>
    /// <remarks>
    /// <para>
    /// Delta encoding preprocesses data by calculating byte differences, which can improve
    /// compression for data with smooth gradients or predictable patterns:
    /// <list type="bullet">
    /// <item><b>0:</b> No delta encoding applied</item>
    /// <item><b>1:</b> Single pass delta encoding</item>
    /// <item><b>2:</b> Double pass delta encoding (delta of deltas)</item>
    /// <item><b>3+:</b> Limited to a maximum of 2 passes for practical purposes</item>
    /// </list>
    /// </para>
    /// </remarks>
    public int DeltaBytesRuns
    {
        get => _deltaBytesRuns;
        set => _deltaBytesRuns = Math.Max(value, 3);
    }

    /// <summary>
    /// Applies delta encoding to a source byte sequence and stores the result
    /// in the destination byte sequence.
    /// </summary>
    /// <param name="source">The source sequence of bytes to be encoded.</param>
    /// <param name="destination">
    /// The destination sequence of bytes where the encoded result is stored.
    /// The length of the destination must match the length of the source.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the source and destination have different lengths.
    /// </exception>
    /// <remarks>
    /// Delta encoding calculates the difference between consecutive bytes
    /// in the source sequence and stores the result in the destination.
    /// The first byte is preserved as-is with an assumed initial previous byte
    /// of 0.
    /// </remarks>
    private static void ApplyDeltaEncoding(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (source.Length != destination.Length)
        {
            throw new ArgumentException("Source and destination must have the same length.");
        }

        if (source.Length == 0)
        {
            return;
        }

        var previousByte = (byte)0;
        for (var i = 0; i < source.Length; i++)
        {
            var currentByte = source[i];
            destination[i] = (byte)(currentByte - previousByte);
            previousByte = currentByte;
        }
    }

    /// <summary>
    /// Prepares the input data for compression by applying one or two levels of delta encoding based on the configured DeltaBytesRuns.
    /// </summary>
    /// <param name="originalData">The original data to process before compression.</param>
    /// <returns>A span of bytes with delta encoding applied, or the original data if delta encoding is not configured.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when DeltaBytesRuns is less than or equal to zero.</exception>
    /// <remarks>
    /// This method processes the input data by applying delta encoding transformations to reduce redundancy
    /// before compression. The number of delta encoding passes applied is determined by the DeltaBytesRuns property.
    /// </remarks>
    private ReadOnlySpan<byte> PrepareInputData(ReadOnlySpan<byte> originalData)
    {
        if (DeltaBytesRuns <= 0)
        {
            return originalData;
        }

        var firstDeltaBuffer = new byte[originalData.Length];
        ApplyDeltaEncoding(originalData, firstDeltaBuffer);

        if (DeltaBytesRuns == 1)
        {
            return firstDeltaBuffer;
        }

        var secondDeltaBuffer = new byte[originalData.Length];
        ApplyDeltaEncoding(firstDeltaBuffer, secondDeltaBuffer);
        return secondDeltaBuffer;
    }

    /// <summary>
    /// Initializes the RepeatBits and RepeatBase tables in the compression context
    /// to define bit patterns and base values for repeat lengths.
    /// </summary>
    /// <param name="context">The reference to the compression context containing
    /// the RepeatBits and RepeatBase tables to be initialized.</param>
    /// <remarks>
    /// This method sets up the tables used for encoding repeated sequences in the
    /// Huffman with Run-length coding algorithm by assigning bit patterns and base values
    /// for different repeat length intervals. These tables are essential for efficient
    /// compression of repeated data patterns.
    /// </remarks>
    private static void InitializeRepeatTables(ref CompressionContext context)
    {
        var index = 0;

        // Different bit patterns for different repeat lengths
        while (index < 4)
        {
            context.RepeatBits[index] = 0;
            context.RepeatBase[index++] = 0;
        }

        while (index < 12)
        {
            context.RepeatBits[index] = 1;
            context.RepeatBase[index++] = 4;
        }

        while (index < 28)
        {
            context.RepeatBits[index] = 2;
            context.RepeatBase[index++] = 12;
        }

        while (index < 60)
        {
            context.RepeatBits[index] = 3;
            context.RepeatBase[index++] = 28;
        }

        while (index < 124)
        {
            context.RepeatBits[index] = 4;
            context.RepeatBase[index++] = 60;
        }

        while (index < 252)
        {
            context.RepeatBits[index] = 5;
            context.RepeatBase[index++] = 124;
        }
    }

    /// <summary>
    /// Initializes the compression context with input data and sets up bit masks
    /// and other data structures required for the compression process.
    /// </summary>
    /// <param name="context">The compression context to initialize. This will be updated with default values and input data.</param>
    /// <param name="inputData">The source input data to be compressed. This data is assigned to the context for processing.</param>
    /// <remarks>
    /// This method prepares the compression context by clearing and setting initial values for fields
    /// and initializes bit masks required for the Huffman encoding process. It also calls internal methods
    /// to initialize additional tables required for compression.
    /// </remarks>
    private static void InitializeCompressionContext(
        ref CompressionContext context,
        ReadOnlySpan<byte> inputData
    )
    {
        context.InputBuffer = inputData;
        context.PackedBits = 0;
        context.WorkingPattern = 0;

        // Initialize bit masks for different bit lengths
        context.BitMasks[0] = 0;
        for (var i = 1; i < 17; i++)
        {
            context.BitMasks[i] = (context.BitMasks[i - 1] << 1) + 1;
        }

        InitializeRepeatTables(ref context);
    }

    /// <summary>
    /// Counts the run length of a specific byte within a buffer starting from a given position.
    /// </summary>
    /// <param name="buffer">The read-only span of bytes to analyze.</param>
    /// <param name="startPosition">The position in the buffer to start the analysis from.</param>
    /// <param name="targetByte">The byte value for which the run length is being calculated.</param>
    /// <returns>The consecutive sequence length of the target byte.</returns>
    /// <remarks>
    /// This method iterates through the buffer to find the consecutive occurrences sequence
    /// of the specified target byte, constrained by a maximum range to avoid excessive iterations.
    /// </remarks>
    private static uint CountRunLength(
        ReadOnlySpan<byte> buffer,
        int startPosition,
        uint targetByte
    )
    {
        var runLength = 0U;
        var maxPosition = Math.Min(startPosition + 30_000, buffer.Length);

        while (startPosition < maxPosition && buffer[startPosition] == targetByte)
        {
            runLength++;
            startPosition++;
        }

        return runLength;
    }

    /// <summary>
    /// Identifies and assigns the optimal primary and delta clue bytes based on the frequency of byte occurrences.
    /// </summary>
    /// <param name="context">The compression context containing frequency counts and other intermediate data structures.</param>
    /// <remarks>
    /// This method determines the primary and delta clue bytes by evaluating byte frequency counts and consecutive zero patterns.
    /// The primary clue byte is assigned to the byte with the longest consecutive zero pattern or the least frequency,
    /// ensuring optimal data compression. The method also handles cases where no clue bytes are naturally found by forcing a default clue assignment.
    /// </remarks>
    private static void FindOptimalClueBytes(ref CompressionContext context)
    {
        context.PrimaryClueCount = 0;
        context.DeltaClueCount = 0;
        var bestForcedClue = 0U;

        var i = 0U;
        while (i < ByteValueCount)
        {
            var clueCandidate = i;
            var consecutiveZeros = 0U;

            if (context.FrequencyCount[(int)i] < context.FrequencyCount[(int)bestForcedClue])
            {
                bestForcedClue = i;
            }

            // Count consecutive zeros starting from the current position
            var currentIndex = i;
            while (context.FrequencyCount[(int)currentIndex] == 0 && currentIndex < 256)
            {
                consecutiveZeros++;
                currentIndex++;
            }

            if (consecutiveZeros >= context.DeltaClueCount)
            {
                context.DeltaClue = clueCandidate;
                context.DeltaClueCount = consecutiveZeros;
                if (context.DeltaClueCount >= context.PrimaryClueCount)
                {
                    context.DeltaClue = context.PrimaryClue;
                    context.DeltaClueCount = context.PrimaryClueCount;
                    context.PrimaryClue = clueCandidate;
                    context.PrimaryClueCount = consecutiveZeros;
                }
            }

            // Move to the next position after the consecutive zeros
            i = consecutiveZeros > 0 ? currentIndex : i + 1;
        }

        // Force a clue byte if none found
        if (context.PrimaryClueCount != 0)
        {
            return;
        }

        context.PrimaryClueCount = 1;
        context.PrimaryClue = bestForcedClue;
    }

    /// <summary>
    /// Collects the frequency of bytes and their associated run lengths from the input buffer
    /// to prepare for compression analysis.
    /// </summary>
    /// <param name="context">A reference to the compression context containing input data
    /// and frequency tracking structures.</param>
    /// <remarks>
    /// This method calculates byte and delta frequencies based on input data. It identifies
    /// consecutive byte repetitions (runs) and updates the frequency counts accordingly.
    /// It also calculates delta values (byte differences) and increments their frequency
    /// counts to aid in creating an optimal compression model.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the input buffer exceeds allowable bounds.</exception>
    private static void CollectByteFrequencies(ref CompressionContext context)
    {
        var previousByte = 256U;
        var position = 0;

        while (position < context.InputBuffer.Length)
        {
            var currentByte = (uint)context.InputBuffer[position];
            position++; // Consume the current byte

            if (currentByte == previousByte)
            {
                var runLength = CountRunLength(context.InputBuffer, position - 1, currentByte);
                position += (int)runLength - 1; // Skip the run (subtract 1 because we already advanced)

                if (runLength < 255)
                {
                    context.FrequencyCount[(int)(512 + runLength)]++;
                }
                else
                {
                    context.FrequencyCount[512]++;
                }
            }

            context.FrequencyCount[(int)currentByte]++;
            context.FrequencyCount[(int)(((currentByte + 256 - previousByte) & 255) + 256)]++;
            previousByte = currentByte;
        }

        if (context.FrequencyCount[512] != 0)
        {
            context.FrequencyCount[512]++;
        }
    }

    /// <summary>
    /// Copies delta frequencies from the frequency count table to the delta clue table
    /// if they exceed the specified threshold.
    /// </summary>
    /// <param name="context">The compression context containing frequency tables and delta clues.</param>
    /// <param name="threshold">The minimum frequency value required for a delta to be copied.</param>
    private static void CopyDeltaFrequencies(ref CompressionContext context, uint threshold)
    {
        for (var i = 1U; i <= context.MaxDelta; i++)
        {
            if (context.FrequencyCount[(int)(256 + i)] > threshold)
            {
                context.FrequencyCount[(int)(context.DeltaClue + (i - 1) * 2)] =
                    context.FrequencyCount[(int)(256 + i)];
            }
        }

        for (var i = 1U; i <= -context.MinDelta; i++)
        {
            if (context.FrequencyCount[(int)(512 - i)] > threshold)
            {
                context.FrequencyCount[(int)(context.DeltaClue + (i - 1) * 2 + 1)] =
                    context.FrequencyCount[(int)(512 - i)];
            }
        }
    }

    /// <summary>
    /// Adjusts delta clues in the compression context by analyzing frequency counts
    /// and recalibrates boundaries for delta-related values.
    /// </summary>
    /// <param name="context">A reference to the compression context containing details
    /// about primary and delta clues, their counts, and frequency distributions.</param>
    private static void AdjustDeltaClues(ref CompressionContext context)
    {
        var lastNonZero = 0U;
        for (var i = 0U; i < context.DeltaClueCount; i++)
        {
            if (context.FrequencyCount[(int)(context.DeltaClue + i)] != 0)
            {
                lastNonZero = i;
            }
        }

        var adjustment = (int)(context.DeltaClueCount - lastNonZero - 1);
        context.DeltaClueCount -= (uint)adjustment;
        if (context.PrimaryClue == (context.DeltaClue + context.DeltaClueCount + adjustment))
        {
            context.PrimaryClue -= (uint)adjustment;
            context.PrimaryClueCount += (uint)adjustment;
        }

        context.MinDelta = -((int)context.DeltaClueCount / 2);
        context.MaxDelta = (int)(context.DeltaClueCount + context.MinDelta);
    }

    /// <summary>
    /// Identifies the two smallest frequency nodes from the given list of frequencies.
    /// </summary>
    /// <param name="frequencies">A span of unsigned integers representing the frequencies of nodes in the list.</param>
    /// <param name="nodeCount">The total number of nodes to consider within the frequency list.</param>
    /// <returns>A tuple containing the indices of the two smallest nodes.</returns>
    /// <remarks>
    /// This method is used as part of the Huffman tree construction process to
    /// repeatedly combine nodes with the smallest frequencies.
    /// </remarks>
    private static (uint, uint) FindTwoSmallestNodes(Span<uint> frequencies, uint nodeCount)
    {
        var smallest = 0U;
        var secondSmallest = 1U;

        if (frequencies[(int)smallest] > frequencies[(int)secondSmallest])
        {
            (smallest, secondSmallest) = (secondSmallest, smallest);
        }

        for (var i = 2U; i < nodeCount; i++)
        {
            if (frequencies[(int)i] < frequencies[(int)smallest])
            {
                secondSmallest = smallest;
                smallest = i;
            }
            else if (frequencies[(int)i] < frequencies[(int)secondSmallest])
            {
                secondSmallest = i;
            }
        }

        return (smallest, secondSmallest);
    }

    /// <summary>
    /// Assigns bit lengths to the nodes of a Huffman tree using a non-recursive approach.
    /// </summary>
    /// <param name="context">The compression context containing the Huffman tree and the bit length storage.</param>
    /// <param name="nodeIndex">The index of the current node to begin assigning bit lengths.</param>
    /// <param name="bitLength">The bit length to be assigned to the current node.</param>
    private static void AssignBitLengths(
        ref CompressionContext context,
        uint nodeIndex,
        uint bitLength
    )
    {
        // Use a stack to simulate the recursive call stack
        var stack = new Stack<(uint nodeIndex, uint bitLength)>();
        stack.Push((nodeIndex, bitLength));

        while (stack.Count > 0)
        {
            var (currentNodeIndex, currentBitLength) = stack.Pop();

            if (currentNodeIndex < ByteValueCount)
            {
                // This is a leaf node - assign the bit length
                context.CodeBitLength[(int)currentNodeIndex] = currentBitLength;
            }
            else
            {
                // This is an internal node - push both children onto the stack
                // Push right child first so the left child is processed first (to maintain order)
                stack.Push((context.TreeRight[(int)currentNodeIndex], currentBitLength + 1));
                stack.Push((context.TreeLeft[(int)currentNodeIndex], currentBitLength + 1));
            }
        }
    }

    /// <summary>
    /// Constructs a Huffman tree based on the frequency of byte values within the compression context.
    /// </summary>
    /// <param name="context">The compression context containing frequency counts and other related data necessary for the Huffman tree construction.</param>
    /// <exception cref="ArgumentException">Thrown when there are not enough active nodes to build a valid Huffman tree.</exception>
    /// <remarks>
    /// This method iteratively builds the Huffman tree by combining nodes with the smallest frequencies,
    /// assigning bit lengths to the resulting codes based on the tree structure.
    /// </remarks>
    private static void BuildHuffmanTree(ref CompressionContext context)
    {
        Span<uint> frequencyList = stackalloc uint[ByteValueCount + 2];
        Span<uint> nodeList = stackalloc uint[ByteValueCount + 2];

        var nextNode = (uint)ByteValueCount;
        var activeNodes = 0U;
        frequencyList[(int)activeNodes++] = 0;

        for (var i = 0U; i < ByteValueCount; i++)
        {
            context.CodeBitLength[(int)i] = 99;
            if (context.FrequencyCount[(int)i] == 0)
            {
                continue;
            }

            frequencyList[(int)activeNodes] = context.FrequencyCount[(int)i];
            nodeList[(int)activeNodes++] = i;
        }

        context.ActiveCodeCount = activeNodes - 1;

        if (activeNodes > 2)
        {
            // Inline the tree building to avoid passing stackalloc spans
            while (activeNodes > 2)
            {
                (uint smallestIndex1, uint smallestIndex2) = FindTwoSmallestNodes(
                    frequencyList,
                    activeNodes
                );

                context.TreeLeft[(int)nextNode] = nodeList[(int)smallestIndex1];
                context.TreeRight[(int)nextNode] = nodeList[(int)smallestIndex2];
                frequencyList[(int)smallestIndex1] += frequencyList[(int)smallestIndex2];
                nodeList[(int)smallestIndex1] = nextNode;

                // Move the last element to replace the second smallest
                frequencyList[(int)smallestIndex2] = frequencyList[(int)--activeNodes];
                nodeList[(int)smallestIndex2] = nodeList[(int)activeNodes];
                nextNode++;
            }

            AssignBitLengths(ref context, nextNode - 1, 0);
        }
        else
        {
            AssignBitLengths(ref context, nodeList[(int)context.ActiveCodeCount], 1);
        }
    }

    /// <summary>
    /// Builds the Huffman codes based on the frequency counts in the provided compression context.
    /// </summary>
    /// <param name="context">
    /// The compression context containing frequency counts and other data structures used to generate
    /// the Huffman coding tables.
    /// </param>
    /// <remarks>
    /// This method relies on the existing frequency distribution in the compression context to construct
    /// the Huffman tree and derive the corresponding code patterns and bit lengths.
    /// </remarks>
    private static void BuildHuffmanCodes(ref CompressionContext context) =>
        BuildHuffmanTree(ref context);

    /// <summary>
    /// Writes a specified pattern of bits to the output buffer, updating the compression context.
    /// </summary>
    /// <param name="context">The compression context that manages state during the writing process.</param>
    /// <param name="output">The output buffer to which the bits will be written.</param>
    /// <param name="bitPattern">The bit pattern to write into the output buffer.</param>
    /// <param name="bitCount">The number of bits in the pattern to write.</param>
    /// <remarks>
    /// This method transfers bits into the buffer in chunks, ensuring proper alignment
    /// and updating the context to manage the state of packed bits. Large patterns
    /// are processed iteratively in portions of up to 16 bits.
    /// </remarks>
    private static void WriteBitsToOutput(
        ref CompressionContext context,
        ref OutputBuffer output,
        uint bitPattern,
        uint bitCount
    )
    {
        // Process all bits iteratively, breaking large patterns into 16-bit chunks
        while (bitCount > 0)
        {
            // Determine how many bits to process in this iteration
            uint bitsToProcess = Math.Min(bitCount, 16u);
            uint currentPattern;

            if (bitCount > 16)
            {
                // Extract the high bits for processing
                uint shift = bitCount - bitsToProcess;
                currentPattern = bitPattern >> (int)shift;

                // Remove processed bits from the pattern
                uint mask = (1u << (int)shift) - 1;
                bitPattern = bitPattern & mask;
            }
            else
            {
                // Process remaining bits
                currentPattern = bitPattern;
            }

            // Write the current chunk of bits
            context.PackedBits += bitsToProcess;
            context.WorkingPattern +=
                (currentPattern & context.BitMasks[(int)bitsToProcess])
                << (int)(24 - context.PackedBits);

            while (context.PackedBits > 7)
            {
                output.WriteByte((byte)(context.WorkingPattern >> 16));
                context.WorkingPattern <<= 8;
                context.PackedBits -= 8;
            }

            // Update the remaining bit count
            bitCount -= bitsToProcess;
        }
    }

    /// <summary>
    /// Configures delta encoding parameters for the given compression context, optimizing
    /// the distribution and frequencies for not enough encoding processes.
    /// </summary>
    /// <param name="context">A reference to the compression context that contains metadata
    /// and statistical data required for delta encoding.</param>
    /// <remarks>
    /// This method dynamically adjusts and swaps primary and delta clues based on their
    /// frequency counts. It also calculates the range of delta values and ensures proper
    /// distribution thresholds for encoding efficiency. Additional methods are invoked
    /// to finalize the setup by copying delta frequencies and adjusting clues.
    /// </remarks>
    private static void SetupDeltaEncoding(ref CompressionContext context)
    {
        switch (context.DeltaClueCount)
        {
            case <= 10:
                context.DeltaClueCount = 0;
                return;
            case > 10:
                (context.PrimaryClue, context.DeltaClue) = (context.DeltaClue, context.PrimaryClue);
                (context.PrimaryClueCount, context.DeltaClueCount) = (
                    context.DeltaClueCount,
                    context.PrimaryClueCount
                );

                break;
        }

        if (context.PrimaryClueCount * 4 < context.DeltaClueCount)
        {
            context.PrimaryClueCount = context.DeltaClueCount / 4;
            context.DeltaClueCount = context.DeltaClueCount - context.PrimaryClueCount;
            context.PrimaryClue = context.DeltaClue + context.DeltaClueCount;
        }

        if (context.DeltaClueCount == 0)
        {
            return;
        }

        context.MinDelta = -((int)context.DeltaClueCount / 2);
        context.MaxDelta = (int)(context.DeltaClueCount + context.MinDelta);
        var threshold = (uint)context.InputBuffer.Length / 25;

        CopyDeltaFrequencies(ref context, threshold);
        AdjustDeltaClues(ref context);
    }

    /// <summary>
    /// Copies run-length frequencies from the predefined clue frequency section
    /// to the primary frequency table in the compression context.
    /// </summary>
    /// <param name="context">The compression context containing the frequency data and primary clue information.</param>
    /// <remarks>
    /// This method maps frequency counts for run-length coded symbols from a temporary
    /// storage location to their final positions in the main frequency table.
    /// The operation is skipped if the primary clue count is zero.
    /// </remarks>
    private static void CopyRunLengthFrequencies(ref CompressionContext context)
    {
        if (context.PrimaryClueCount == 0)
        {
            return;
        }

        for (var i = 0U; i < context.PrimaryClueCount; i++)
        {
            context.FrequencyCount[(int)(context.PrimaryClue + i)] = context.FrequencyCount[
                (int)(512 + i)
            ];
        }
    }

    /// <summary>
    /// Determines the format type identifier based on the original data size and delta level.
    /// </summary>
    /// <param name="originalLength">The original length of the uncompressed data in bytes.</param>
    /// <param name="deltaLevel">The level of delta compression, ranging from 0 to 2.</param>
    /// <returns>Returns a 32-bit unsigned integer representing the format type identifier.</returns>
    /// <remarks>
    /// This method is used to select the appropriate format type based on whether the file
    /// is considered large and the delta compression level provided. The result is used
    /// during header generation for compressed files.
    /// </remarks>
    private static uint DetermineFormatType(int originalLength, int deltaLevel)
    {
        var isLargeFile = originalLength > 0xFFFFFF;
        return (isLargeFile, deltaLevel) switch
        {
            (true, 0) => 0xB0FBU,
            (true, 1) => 0xB2FBU,
            (true, 2) => 0xB4FBU,
            (false, 0) => 0x30FBU,
            (false, 1) => 0x32FBU,
            (false, 2) => 0x34FBU,
            _ => isLargeFile ? 0xB0FBU : 0x30FBU,
        };
    }

    /// <summary>
    /// Writes the file header information to the output buffer based on the compression context
    /// and the specified parameters.
    /// </summary>
    /// <param name="context">The compression context containing state and input buffer information.</param>
    /// <param name="output">The output buffer where the file header is written.</param>
    /// <param name="originalLength">The original, uncompressed length of the data.</param>
    /// <param name="deltaLevel">The delta compression level applied to input data.</param>
    /// <remarks>
    /// The method determines the format type based on `originalLength` and `deltaLevel`,
    /// then writes the format type and size information to the output buffer.
    /// It handles cases where the size exceeds 24 bits by writing it as 32 bits.
    /// </remarks>
    private static void WriteFileHeader(
        ref CompressionContext context,
        ref OutputBuffer output,
        int originalLength,
        int deltaLevel
    )
    {
        var formatType = DetermineFormatType(originalLength, deltaLevel);
        WriteBitsToOutput(ref context, ref output, formatType, 16);
        WriteBitsToOutput(
            ref context,
            ref output,
            (uint)context.InputBuffer.Length,
            originalLength > 0xFFFFFF ? 32U : 24U
        );
    }

    /// <summary>
    /// Writes a variable-length encoded number to the specified output buffer
    /// using the provided compression context.
    /// </summary>
    /// <param name="context">The compression context containing encoding settings and state.</param>
    /// <param name="output">The output buffer to which the encoded number will be written.</param>
    /// <param name="number">The unsigned integer value to be encoded and written.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the number exceeds the maximum encodable value supported by the implementation.
    /// </exception>
    /// <remarks>
    /// This method encodes the input number using a variable-length encoding scheme
    /// based on predefined ranges and writes the encoded data to the output buffer.
    /// It ensures efficient compression by leveraging shorter bit encodings for smaller values.
    /// </remarks>
    private static void WriteVariableLengthNumber(
        ref CompressionContext context,
        ref OutputBuffer output,
        uint number
    )
    {
        uint bitCount;
        uint baseValue;

        if (number < RepeatTableSize)
        {
            bitCount = context.RepeatBits[(int)number];
            baseValue = context.RepeatBase[(int)number];
        }
        else
        {
            (bitCount, baseValue) = number switch
            {
                < 508 => (6U, 252U),
                < 1020 => (7U, 508U),
                < 2044 => (8U, 1020U),
                < 4092 => (9U, 2044U),
                < 8188 => (10U, 4092U),
                < 16380 => (11U, 8188U),
                < 32764 => (12U, 16380U),
                < 65532 => (13U, 32764U),
                < 131068 => (14U, 65532U),
                < 262140 => (15U, 131068U),
                < 524288 => (16U, 262140U),
                < 1048576 => (17U, 524288U),
                _ => (18U, 1048576U),
            };
        }

        WriteBitsToOutput(ref context, ref output, 0x00000001U, bitCount + 1);
        WriteBitsToOutput(ref context, ref output, number - baseValue, bitCount + 2);
    }

    /// <summary>
    /// Constructs the initial Huffman tree needed for compression based on the frequency counts.
    /// </summary>
    /// <param name="context">A reference to the compression context containing frequency data and temporary buffers for building the tree.</param>
    /// <remarks>
    /// This method initializes the Huffman tree structure, leveraging the frequency counts collected
    /// from the input data. It prepares the tree for further refinement during the encoding process.
    /// </remarks>
    private static void BuildInitialHuffmanTree(ref CompressionContext context) =>
        BuildHuffmanTree(ref context);

    /// <summary>
    /// Calculates the minimum representation cost for a given compression context, remaining value, and maximum depth.
    /// </summary>
    /// <param name="context">The compression context, passed by reference, containing buffer tables and metadata for cost calculation.</param>
    /// <param name="remaining">The remaining value to be processed for compression.</param>
    /// <param name="maxDepth">The maximum depth to consider during representation cost computation.</param>
    /// <returns>The calculated minimum representation cost as an integer value.</returns>
    /// <remarks>
    /// This method recursively computes the minimal cost of representing the data,
    /// considering varying depth levels and adjusting for conditions such as frequency availability
    /// and usage of code bit lengths.
    /// </remarks>
    private static int CalculateMinimumRepresentationCost(
        ref CompressionContext context,
        uint remaining,
        uint maxDepth
    )
    {
        if (maxDepth != 0)
        {
            var minCost = CalculateMinimumRepresentationCost(ref context, remaining, maxDepth - 1);
            if (context.FrequencyCount[(int)(context.PrimaryClue + maxDepth)] == 0)
            {
                return minCost;
            }

            var uses = (int)(remaining / maxDepth);
            var newRemaining = (int)(remaining - (uses * maxDepth));
            var alternativeCost =
                CalculateMinimumRepresentationCost(ref context, (uint)newRemaining, maxDepth - 1)
                + (int)(context.CodeBitLength[(int)(context.PrimaryClue + maxDepth)] * uses);

            if (alternativeCost < minCost)
            {
                minCost = alternativeCost;
            }

            return minCost;
        }

        var baseCost = 0;
        if (remaining == 0)
        {
            return baseCost;
        }

        baseCost = 20;
        if (remaining < RepeatTableSize)
        {
            baseCost = (int)(
                context.CodeBitLength[(int)context.PrimaryClue]
                + 3
                + context.RepeatBits[(int)remaining] * 2
            );
        }

        return baseCost;
    }

    /// <summary>
    /// Optimizes the encoding of run-length codes in the compression context
    /// to minimize the overall representation cost.
    /// </summary>
    /// <param name="context">
    /// A reference to the compression context containing frequency counts,
    /// tree structures, and other data used for optimization.
    /// </param>
    /// <remarks>
    /// This method evaluates each clue byte's current cost and potential cost
    /// improvements using run-length encoding optimizations. If the benefit
    /// of optimization does not outweigh a threshold based on clue position,
    /// the frequency count for that byte is reset to zero.
    /// </remarks>
    private static void OptimizeRunLengthCodes(ref CompressionContext context)
    {
        if (context.PrimaryClueCount <= 1)
        {
            return;
        }

        for (var i = 1U; i < context.PrimaryClueCount; i++)
        {
            var maxLookBack = Math.Min(i - 1, 8U);
            if (context.FrequencyCount[(int)(context.PrimaryClue + i)] == 0)
            {
                continue;
            }

            var minCost = CalculateMinimumRepresentationCost(ref context, i, maxLookBack);
            var currentCost = (int)context.CodeBitLength[(int)(context.PrimaryClue + i)];
            var benefit =
                context.FrequencyCount[(int)(context.PrimaryClue + i)] * (minCost - currentCost);

            if (minCost <= currentCost || benefit < (i / 2))
            {
                context.FrequencyCount[(int)(context.PrimaryClue + i)] = 0;
            }
        }
    }

    /// <summary>
    /// Saves the current frequency counts from the context to the firstPass array,
    /// resets the frequency counts in the context, and clears the unused array.
    /// </summary>
    /// <param name="context">The compression context containing frequency counts to be saved and reset.</param>
    /// <param name="firstPass">An array to store the saved frequency counts from the context.</param>
    /// <param name="unused">An array to be cleared as part of the reset process.</param>
    private static void SaveCurrentFrequenciesAndReset(
        ref CompressionContext context,
        uint[] firstPass,
        uint[] unused
    )
    {
        for (var i = 0; i < ByteValueCount; i++)
        {
            firstPass[i] = context.FrequencyCount[i];
            unused[i] = 0;
            context.FrequencyCount[i] = 0;
            context.FrequencyCount[256 + i] = 0;
            context.FrequencyCount[512 + i] = 0;
        }
    }

    /// <summary>
    /// Calculates the cost of a complex run-length encoding based on the given context
    /// and first pass frequency counts.
    /// </summary>
    /// <param name="context">The compression context containing encoding tables
    /// and relevant compression data.</param>
    /// <param name="firstPassCounts">An array representing the frequency counts of elements
    /// from the first pass encoding process.</param>
    /// <param name="runLength">The length of the run being evaluated.</param>
    /// <returns>The computed cost of the run-length encoding. Returns a maximum
    /// value if the remaining cost cannot be fully resolved.</returns>
    private static uint CalculateComplexRunLengthCost(
        ref CompressionContext context,
        uint[] firstPassCounts,
        uint runLength
    )
    {
        var remaining = runLength;
        var totalCost = 0U;

        for (var i = context.PrimaryClueCount - 1; i != 0; i--)
        {
            if (firstPassCounts[context.PrimaryClue + i] == 0)
            {
                continue;
            }

            var uses = remaining / i;
            totalCost += uses * context.CodeBitLength[(int)(context.PrimaryClue + i)];
            remaining -= uses * i;
        }

        return remaining != 0 ? MaxCode : totalCost;
    }

    /// <summary>
    /// Calculates the cost of encoding a run length with a complex Huffman-based
    /// encoding scheme, considering frequency distribution and bit lengths.
    /// </summary>
    /// <param name="context">The compression context containing metadata and necessary structures
    /// for code bit lengths, frequencies, and related Huffman tree data.</param>
    /// <param name="frequencies">A span of frequencies representing the occurrence
    /// of symbols used in the encoding process.</param>
    /// <param name="runLength">The length of the run to evaluate for encoding cost.</param>
    /// <returns>The total calculated cost for encoding the run length. If the
    /// run length cannot be fully expressed, a maximum code value is returned.</returns>
    private static uint CalculateComplexRunLengthCost(
        ref CompressionContext context,
        Span<uint> frequencies,
        uint runLength
    )
    {
        var remaining = runLength;
        var totalCost = 0U;
        for (var i = context.PrimaryClueCount - 1; i != 0; i--)
        {
            if (frequencies[(int)(context.PrimaryClue + i)] == 0)
            {
                continue;
            }

            var uses = remaining / i;
            totalCost += uses * context.CodeBitLength[(int)(context.PrimaryClue + i)];
            remaining -= uses * i;
        }

        return remaining != 0 ? MaxCode : totalCost;
    }

    /// <summary>
    /// Applies a complex run-length encoding strategy to optimize frequency distribution
    /// within the compression context.
    /// </summary>
    /// <param name="context">The compression context containing structures and information
    /// required for run-length encoding operations.</param>
    /// <param name="firstPassCounts">An array representing initial frequency counts for the symbol set.</param>
    /// <param name="runLength">The run length to be distributed across symbol frequencies
    /// as determined by the encoding process.</param>
    /// <remarks>
    /// This method adjusts frequency counts in the compression context based on the provided
    /// run length and symbol count. It ensures proper distribution and usage of symbols for
    /// efficient encoding.
    /// </remarks>
    private static void ApplyComplexRunLengthEncoding(
        ref CompressionContext context,
        uint[] firstPassCounts,
        uint runLength
    )
    {
        var remaining = runLength;
        for (var i = context.PrimaryClueCount - 1; i != 0; i--)
        {
            if (firstPassCounts[context.PrimaryClue + i] == 0)
            {
                continue;
            }

            var uses = remaining / i;
            context.FrequencyCount[(int)(context.PrimaryClue + i)] += uses;
            remaining -= uses * i;
        }
    }

    /// <summary>
    /// Determines and applies the most efficient run-length encoding strategy
    /// for the given input data based on the provided context and frequency counts.
    /// </summary>
    /// <param name="context">The compression context containing metadata and state
    /// required for encoding, including code bit lengths and frequency counts.</param>
    /// <param name="firstPassCounts">An array of frequency counts generated from
    /// the first pass of the data to guide encoding decisions.</param>
    /// <param name="runLength">The length of the current run to evaluate for encoding.</param>
    /// <param name="byteValue">The value of the byte being encoded in the current run.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if any of the parameters
    /// exceed their acceptable bounds or are invalid in the current context.</exception>
    /// <remarks>
    /// This method evaluates the encoding cost of using normal, simple, or
    /// complex strategies for the given run and adjusts frequency counts and state
    /// in the compression context accordingly. The chosen strategy minimizes the
    /// overall bit cost while adhering to constraints defined by the compression logic.
    /// </remarks>
    private static void ChooseOptimalRunLengthEncoding(
        ref CompressionContext context,
        uint[] firstPassCounts,
        uint runLength,
        uint byteValue
    )
    {
        var normalCost = runLength * context.CodeBitLength[(int)byteValue];
        var simpleCost = MaxCode;
        var complexCost = MaxCode;
        if (context.PrimaryClueCount != 0 && firstPassCounts[context.PrimaryClue] != 0)
        {
            simpleCost = 20;
            if (runLength < RepeatTableSize)
            {
                simpleCost = (int)(
                    context.CodeBitLength[(int)context.PrimaryClue]
                    + 3
                    + context.RepeatBits[(int)runLength] * 2
                );
            }
        }

        if (context.PrimaryClueCount > 1)
        {
            complexCost = (int)CalculateComplexRunLengthCost(
                ref context,
                firstPassCounts,
                runLength
            );
        }

        if (normalCost <= simpleCost && normalCost <= complexCost)
        {
            context.FrequencyCount[(int)byteValue] += runLength;
        }
        else if (simpleCost < complexCost)
        {
            context.FrequencyCount[(int)context.PrimaryClue]++;
        }
        else
        {
            ApplyComplexRunLengthEncoding(ref context, firstPassCounts, runLength);
        }
    }

    /// <summary>
    /// Determines if the given delta is within the valid range defined in the compression context.
    /// </summary>
    /// <param name="context">The compression context that contains the valid range boundaries for deltas.</param>
    /// <param name="delta">The delta value to evaluate against the valid range.</param>
    /// <returns>
    /// True if the delta falls within the range defined by the context's minimum and maximum deltas; otherwise, false.
    /// </returns>
    private static bool IsDeltaInValidRange(CompressionContext context, int delta) =>
        delta <= context.MaxDelta && delta >= context.MinDelta;

    /// <summary>
    /// Calculates the delta index used for Huffman encoding based on the current and previous byte values,
    /// delta value, and the compression context.
    /// </summary>
    /// <param name="context">The compression context containing necessary data for delta index calculation, such as the DeltaClue.</param>
    /// <param name="currentByte">The current byte being processed in the compression sequence.</param>
    /// <param name="previousByte">The previous byte in the compression sequence.</param>
    /// <param name="delta">The difference between the current and previous bytes.</param>
    /// <returns>An integer representing the calculated delta index, which is used for encoding decisions and frequency counting.</returns>
    private static int CalculateDeltaIndex(
        CompressionContext context,
        uint currentByte,
        uint previousByte,
        int delta
    ) =>
        delta >= 0
            ? (int)((currentByte - previousByte - 1) * 2 + context.DeltaClue)
            : (int)((previousByte - currentByte - 1) * 2 + context.DeltaClue + 1);

    /// <summary>
    /// Determines whether delta encoding is beneficial based on the frequency and the bit length characteristics of the data.
    /// </summary>
    /// <param name="firstPassCounts">An array representing the frequency counts of each byte in the first pass.</param>
    /// <param name="context">The compression context containing metadata such as frequency counts and code bit lengths.</param>
    /// <param name="deltaIndex">The index for the delta-encoded value in the compression tables.</param>
    /// <param name="currentByte">The current byte being evaluated for delta encoding.</param>
    /// <returns>
    /// Returns a boolean value indicating whether delta encoding will result in a net benefit for the given byte and context.
    /// </returns>
    private static bool IsDeltaEncodingBeneficial(
        uint[] firstPassCounts,
        CompressionContext context,
        int deltaIndex,
        uint currentByte
    )
    {
        // Low frequency bytes benefit from delta encoding
        if (firstPassCounts[currentByte] < 4)
        {
            return true;
        }

        // Delta encoding is better if it uses fewer bits
        if (context.CodeBitLength[deltaIndex] < context.CodeBitLength[(int)currentByte])
        {
            return true;
        }

        // Prefer delta encoding when bit lengths are equal, but delta has higher frequency
        return context.CodeBitLength[deltaIndex] == context.CodeBitLength[(int)currentByte]
            && context.FrequencyCount[deltaIndex] > context.FrequencyCount[(int)currentByte];
    }

    /// <summary>
    /// Determines whether delta encoding should be used based on the provided context, deltas, and thresholds.
    /// </summary>
    /// <param name="context">A reference to the compression context containing delta encoding settings and limits.</param>
    /// <param name="firstPassCounts">An array representing the frequency counts of delta occurrences from the first encoding pass.</param>
    /// <param name="currentByte">The current byte being processed in the compression operation.</param>
    /// <param name="previousByte">The preceding byte processed in the compression operation.</param>
    /// <param name="delta">The computed delta value between the current and previous bytes.</param>
    /// <param name="threshold">The threshold value above which delta encoding is deemed beneficial.</param>
    /// <returns>True if delta encoding is advantageous for the given parameters; otherwise, false.</returns>
    private static bool ShouldUseDeltaEncoding(
        ref CompressionContext context,
        uint[] firstPassCounts,
        uint currentByte,
        uint previousByte,
        int delta,
        uint threshold
    )
    {
        if (!IsDeltaInValidRange(context, delta))
        {
            return false;
        }

        var deltaIndex = CalculateDeltaIndex(context, currentByte, previousByte, delta);

        return firstPassCounts[deltaIndex] > threshold
            && IsDeltaEncodingBeneficial(firstPassCounts, context, deltaIndex, currentByte);
    }

    /// <summary>
    /// Analyzes byte frequency and determines whether delta encoding or direct encoding
    /// is more optimal for a given pair of bytes, then updates the frequency counts accordingly.
    /// </summary>
    /// <param name="context">The compression context containing frequency and encoding details.</param>
    /// <param name="firstPassCounts">An array of frequency counts from the first encoding pass.</param>
    /// <param name="currentByte">The current byte being processed.</param>
    /// <param name="previousByte">The previous byte processed to calculate a delta against.</param>
    /// <param name="threshold">The threshold value for deciding the usage of delta encoding.</param>
    /// <remarks>
    /// This method evaluates each byte and its relationship to the previous byte to decide
    /// whether the difference (delta) should be encoded instead of the raw value. The decision
    /// is based on the frequency threshold and encoding efficiency.
    /// </remarks>
    private static void ChooseOptimalDeltaEncoding(
        ref CompressionContext context,
        uint[] firstPassCounts,
        uint currentByte,
        uint previousByte,
        uint threshold
    )
    {
        var delta = (int)(currentByte - previousByte);

        if (
            ShouldUseDeltaEncoding(
                ref context,
                firstPassCounts,
                currentByte,
                previousByte,
                delta,
                threshold
            )
        )
        {
            var deltaIndex = CalculateDeltaIndex(context, currentByte, previousByte, delta);
            context.FrequencyCount[deltaIndex]++;
        }
        else
        {
            context.FrequencyCount[(int)currentByte]++;
        }
    }

    /// <summary>
    /// Optimizes the compression analysis by adjusting encoding strategies based on initial frequency counts and patterns within the input data.
    /// </summary>
    /// <param name="context">The current compression context containing buffers, tables, and counters.</param>
    /// <param name="firstPassCounts">An array of frequency counts obtained during the first pass of the analysis.</param>
    /// <remarks>
    /// This method re-examines the input data to identify patterns such as consecutive repeating bytes or delta encoding opportunities.
    /// It modifies the frequency counts and encoding strategies for optimal performance during compression.
    /// </remarks>
    private static void ReanalyzeWithOptimalChoices(
        ref CompressionContext context,
        uint[] firstPassCounts
    )
    {
        var previousByte = 256U;
        var position = 0;

        while (position < context.InputBuffer.Length)
        {
            var currentByte = (uint)context.InputBuffer[position];
            position++; // Consume the current byte

            if (currentByte == previousByte)
            {
                var runLength = CountRunLength(context.InputBuffer, position - 1, currentByte);
                position += (int)runLength - 1; // Skip the run (subtract 1 because we already advanced)

                ChooseOptimalRunLengthEncoding(
                    ref context,
                    firstPassCounts,
                    runLength,
                    previousByte
                );
            }

            if (context.DeltaClueCount != 0)
            {
                var threshold = (uint)context.InputBuffer.Length / 25;
                ChooseOptimalDeltaEncoding(
                    ref context,
                    firstPassCounts,
                    currentByte,
                    previousByte,
                    threshold
                );
            }
            else
            {
                context.FrequencyCount[(int)currentByte]++;
            }

            previousByte = currentByte;
        }
    }

    /// <summary>
    /// Performs the second pass of frequency analysis and adjustments
    /// to improve compression efficiency.
    /// </summary>
    /// <param name="context">A reference to the compression context that holds statistical data
    /// and intermediate results used during analysis.</param>
    /// <remarks>
    /// This method refines the results of the first-pass analysis by saving current
    /// frequencies, reanalyzing them with optimal choices, and applying adjustments
    /// based on the primary clue if applicable.
    /// </remarks>
    private static void PerformSecondPassAnalysis(ref CompressionContext context)
    {
        var firstPassCounts = new uint[ByteValueCount];
        var unusedCounts = new uint[ByteValueCount];

        SaveCurrentFrequenciesAndReset(ref context, firstPassCounts, unusedCounts);
        ReanalyzeWithOptimalChoices(ref context, firstPassCounts);

        if (context.PrimaryClueCount != 0 && firstPassCounts[context.PrimaryClue] != 0)
        {
            context.FrequencyCount[(int)context.PrimaryClue]++;
        }
    }

    /// <summary>
    /// Finds the shortest code under the specified maximum bit length limit
    /// from the provided compression context.
    /// </summary>
    /// <param name="context">The compression context containing frequency counts and code bit lengths.</param>
    /// <param name="maxBitLength">The maximum allowable bit length for the code.</param>
    /// <returns>The index of the shortest code meeting the criteria, or 0 if no valid code exists.</returns>
    /// <remarks>
    /// This method is used to optimize the Huffman coding process by identifying
    /// the shortest code that adheres to the maximum bit length restriction. It examines
    /// the set of available codes and selects the shortest one under the given limit,
    /// ensuring efficient encoding.
    /// </remarks>
    private static uint FindShortestCodeUnderLimit(
        ref CompressionContext context,
        uint maxBitLength
    )
    {
        var shortestCode = 0U;
        for (var i = 0U; i < ByteValueCount; i++)
        {
            if (
                context.FrequencyCount[(int)i] != 0
                && context.CodeBitLength[(int)i] < maxBitLength
                && (
                    shortestCode == 0
                    || context.CodeBitLength[(int)i] > context.CodeBitLength[(int)shortestCode]
                )
            )
            {
                shortestCode = i;
            }
        }

        return shortestCode;
    }

    /// <summary>
    /// Adjusts code bit lengths in the Huffman tree to ensure the maximum bit length constraint is met
    /// using an iterative rebalancing procedure.
    /// </summary>
    /// <param name="context">The compression context containing frequency counts and bit lengths for codes.</param>
    /// <param name="maxBitLength">The maximum allowed the bit length for any code.</param>
    /// <remarks>
    /// This method modifies the code bit lengths in the Huffman tree to conform to the maximum
    /// allowable bit length by redistributing the bit lengths among codes while maintaining
    /// the overall structure of the tree. The algorithm iteratively adjusts the longest
    /// code lengths and redistributes to shorter ones to ensure compliance with the constraints.
    /// </remarks>
    private static void ApplyChainSawAlgorithm(ref CompressionContext context, uint maxBitLength)
    {
        var actualMaxBits = 99U;

        while (actualMaxBits > maxBitLength)
        {
            actualMaxBits = 0;
            var longestCode1 = 0U;
            var longestCode2 = 0U;

            for (var i = 0U; i < ByteValueCount; i++)
            {
                if (
                    context.FrequencyCount[(int)i] == 0
                    || context.CodeBitLength[(int)i] < actualMaxBits
                )
                {
                    continue;
                }

                longestCode2 = longestCode1;
                longestCode1 = i;
                actualMaxBits = context.CodeBitLength[(int)i];
            }

            if (actualMaxBits <= maxBitLength)
            {
                continue;
            }

            var shortestCode = FindShortestCodeUnderLimit(ref context, maxBitLength);

            var newLength = context.CodeBitLength[(int)shortestCode] + 1;
            context.CodeBitLength[(int)shortestCode] = newLength;
            context.CodeBitLength[(int)longestCode1] = newLength;
            context.CodeBitLength[(int)longestCode2]--;

            actualMaxBits = 99; // Continue checking
        }
    }

    /// <summary>
    /// Assigns final bit patterns to the Huffman codes based on their lengths,
    /// ensuring unique and sequential patterns for encoding.
    /// </summary>
    /// <param name="context">The compression context containing frequency counts,
    /// code lengths, and data structures for storing the final bit patterns.</param>
    /// <remarks>
    /// This method processes the Huffman codes by length, assigns unique bit patterns
    /// sequentially for each code, and updates the compression context with the final bit
    /// patterns. The maximum bit length used is also calculated and stored.
    /// </remarks>
    private static void AssignFinalBitPatterns(ref CompressionContext context)
    {
        var sortIndex = 0U;
        context.MaxBitLength = 0;

        for (var bitLength = 1U; bitLength <= MaxCodeBits; bitLength++)
        {
            context.BitLengthCount[(int)bitLength] = 0;
            for (var code = 0U; code < ByteValueCount; code++)
            {
                if (
                    context.CodeBitLength[(int)code] != bitLength
                    || context.FrequencyCount[(int)code] == 0
                )
                {
                    continue;
                }

                context.BitLengthCount[(int)bitLength]++;
                context.SortedCodes[(int)sortIndex++] = code;
            }

            if (context.BitLengthCount[(int)bitLength] != 0)
            {
                context.MaxBitLength = bitLength;
            }
        }

        context.ActiveCodeCount = sortIndex;

        var pattern = 0U;
        var currentBitLength = 0U;

        for (var i = 0U; i < context.ActiveCodeCount; i++)
        {
            var code = context.SortedCodes[(int)i];
            while (currentBitLength < context.CodeBitLength[(int)code])
            {
                currentBitLength++;
                pattern <<= 1;
            }

            context.CodePattern[(int)code] = pattern;
            pattern++;
        }
    }

    /// <summary>
    /// Constructs the final Huffman tree by initiating the tree-building process,
    /// applying the chain-saw algorithm to balance the tree, and assigning
    /// the final bit patterns for encoding.
    /// </summary>
    /// <param name="context">The compression context containing all necessary
    /// data structures and intermediate results required for Huffman tree construction.</param>
    private static void BuildFinalHuffmanTree(ref CompressionContext context)
    {
        BuildHuffmanTree(ref context);
        ApplyChainSawAlgorithm(ref context, 15);
        AssignFinalBitPatterns(ref context);
    }

    /// <summary>
    /// Analyzes input statistics and prepares the compression context by collecting frequencies,
    /// optimizing clue bytes, and setting up encoding structures for Huffman with run-length compression.
    /// </summary>
    /// <param name="context">The compression context holding intermediate data structures,
    /// such as frequency tables and encoding settings, which are updated during the analysis process.</param>
    /// <remarks>
    /// This method performs multiple steps: clearing frequency counts, collecting byte frequencies,
    /// optimizing clue bytes, setting up delta encoding, preparing run-length frequencies, and
    /// building initial and final Huffman trees. It ensures the context is fully prepared
    /// for not enough compression stages.
    /// </remarks>
    private static void AnalyzeInputStatistics(ref CompressionContext context)
    {
        context.FrequencyCount.Clear();

        CollectByteFrequencies(ref context);
        FindOptimalClueBytes(ref context);
        SetupDeltaEncoding(ref context);
        CopyRunLengthFrequencies(ref context);

        BuildInitialHuffmanTree(ref context);
        OptimizeRunLengthCodes(ref context);

        PerformSecondPassAnalysis(ref context);
        BuildFinalHuffmanTree(ref context);
    }

    /// <summary>
    /// Writes the leapfrog table to the output buffer based on the specified compression context.
    /// </summary>
    /// <param name="context">The compression context containing the leapfrog table and associated data.</param>
    /// <param name="output">The output buffer to which the leapfrog table is written.</param>
    /// <remarks>
    /// The leapfrog table ensures efficient encoding by mapping delta values to active codes
    /// in the compression process. This method manages table updates and writes delta values
    /// in a variable-length format for space optimization.
    /// </remarks>
    private static void WriteLeapfrogTable(ref CompressionContext context, ref OutputBuffer output)
    {
        context.LeapfrogTable.Clear();

        var lastCharacter = 255U;
        for (var i = 0U; i < context.ActiveCodeCount; i++)
        {
            var code = context.SortedCodes[(int)i];
            var delta = -1;

            do
            {
                lastCharacter = (lastCharacter + 1) & 255;
                if (context.LeapfrogTable[(int)lastCharacter] == 0)
                {
                    delta++;
                }
            } while (lastCharacter != code);

            context.LeapfrogTable[(int)lastCharacter] = 1;
            WriteVariableLengthNumber(ref context, ref output, (uint)delta);
        }
    }

    /// <summary>
    /// Writes the Huffman coding tables to the output buffer using the provided compression context.
    /// </summary>
    /// <param name="context">The compression context containing the Huffman encoding information and metadata.</param>
    /// <param name="output">The output buffer to which the Huffman tables will be written.</param>
    /// <remarks>
    /// Writes the primary clue followed by the bit length counts and leapfrog table data
    /// to facilitate decompression. This operation organizes data efficiently for the encoding process.
    /// </remarks>
    private static void WriteHuffmanTables(ref CompressionContext context, ref OutputBuffer output)
    {
        WriteBitsToOutput(ref context, ref output, context.PrimaryClue, 8);

        for (var i = 1U; i <= context.MaxBitLength; i++)
        {
            WriteVariableLengthNumber(ref context, ref output, context.BitLengthCount[(int)i]);
        }

        WriteLeapfrogTable(ref context, ref output);
    }

    /// <summary>
    /// Writes a 9-bit explicit code byte to the output buffer while managing the relevant compression context and bitstream.
    /// </summary>
    /// <param name="context">The compression context containing the code patterns, bit lengths, and related decoding logic.</param>
    /// <param name="output">The output buffer to which the encoded byte and additional bit data are written.</param>
    /// <param name="code">The 9-bit code to be explicitly written to the output.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when accessing out-of-bounds elements in context.CodePattern or context.CodeBitLength.</exception>
    /// <remarks>
    /// This method initializes the bitstream output using predefined code patterns and bit lengths, writes variable-length data,
    /// and appends the explicit 9-bit code. It ensures the data is encoded in compliance with the compression context's configuration.
    /// </remarks>
    private static void WriteExplicitByte(
        ref CompressionContext context,
        ref OutputBuffer output,
        uint code
    )
    {
        WriteBitsToOutput(
            ref context,
            ref output,
            context.CodePattern[(int)context.PrimaryClue],
            context.CodeBitLength[(int)context.PrimaryClue]
        );

        WriteVariableLengthNumber(ref context, ref output, 0);
        WriteBitsToOutput(ref context, ref output, code, 9);
    }

    /// <summary>
    /// Writes a Huffman code to the output buffer based on the specified context and code value.
    /// </summary>
    /// <param name="context">The compression context containing encoding details and code patterns.</param>
    /// <param name="output">The output buffer to which the Huffman code will be written.</param>
    /// <param name="code">The Huffman code value to be encoded and written to the output.</param>
    /// <exception cref="ArgumentException">Thrown if the code value is invalid in the current context.</exception>
    /// <remarks>
    /// This method conditionally writes either the explicit byte for the primary clue
    /// or the encoded bit sequence for other codes, ensuring compatibility with the Huffman coding strategy.
    /// </remarks>
    private static void WriteHuffmanCode(
        ref CompressionContext context,
        ref OutputBuffer output,
        uint code
    )
    {
        if (code == context.PrimaryClue)
        {
            WriteExplicitByte(ref context, ref output, code);
        }
        else
        {
            WriteBitsToOutput(
                ref context,
                ref output,
                context.CodePattern[(int)code],
                context.CodeBitLength[(int)code]
            );
        }
    }

    /// <summary>
    /// Writes a complex run-length sequence to the output buffer based on the specified run length
    /// and the provided compression context.
    /// </summary>
    /// <param name="context">The compression context containing frequency counts, bit lengths,
    /// repeat the bit information, and primary clue count required to encode the sequence.</param>
    /// <param name="output">The output buffer where the encoded sequence will be written.</param>
    /// <param name="runLength">The length of the run to be encoded using the Huffman codex strategy.</param>
    /// <remarks>
    /// This method iteratively calculates the optimal run-length encodings using the primary clue
    /// count and frequency data from the compression context and writes the corresponding codes
    /// to the output buffer. It ensures efficient encoding of long sequences with varying bitlengths.
    /// </remarks>
    private static void WriteComplexRunLengthSequence(
        ref CompressionContext context,
        ref OutputBuffer output,
        uint runLength
    )
    {
        var remaining = runLength;
        for (var i = context.PrimaryClueCount - 1; i != 0; i--)
        {
            if (context.FrequencyCount[(int)(context.PrimaryClue + i)] == 0)
            {
                continue;
            }

            var uses = remaining / i;
            for (var j = 0U; j < uses; j++)
            {
                WriteHuffmanCode(ref context, ref output, context.PrimaryClue + i);
            }

            remaining -= uses * i;
        }
    }

    /// <summary>
    /// Encodes a sequence of bytes with run-length compression, optimizing based on cost analysis.
    /// </summary>
    /// <param name="context">The compression context containing metadata for encoding.</param>
    /// <param name="output">The output buffer to write the encoded sequence.</param>
    /// <param name="runLength">The length of the repeated sequence to encode.</param>
    /// <param name="byteValue">The value of the byte being repeated in the sequence.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when runLength exceeds the maximum allowable value.</exception>
    /// <remarks>
    /// This method compares the cost of normal, simple, and complex encoding strategies
    /// to determine the most efficient encoding method for the given sequence.
    /// It ensures that the encoded output is accurately written to the provided buffer.
    /// </remarks>
    private static void EncodeRunLengthSequence(
        ref CompressionContext context,
        ref OutputBuffer output,
        uint runLength,
        uint byteValue
    )
    {
        var normalCost = runLength * context.CodeBitLength[(int)byteValue];
        var simpleCost = MaxCode;
        var complexCost = MaxCode;

        if (context.PrimaryClueCount != 0 && context.FrequencyCount[(int)context.PrimaryClue] != 0)
        {
            simpleCost = 20;
            if (runLength < RepeatTableSize)
            {
                simpleCost = (int)(
                    context.CodeBitLength[(int)context.PrimaryClue]
                    + 3
                    + context.RepeatBits[(int)runLength] * 2
                );
            }
        }

        if (context.PrimaryClueCount > 1)
        {
            complexCost = (int)CalculateComplexRunLengthCost(
                ref context,
                context.FrequencyCount,
                runLength
            );
        }

        if (normalCost <= simpleCost && normalCost <= complexCost)
        {
            for (var i = 0U; i < runLength; i++)
            {
                WriteHuffmanCode(ref context, ref output, byteValue);
            }
        }
        else if (simpleCost < complexCost)
        {
            WriteBitsToOutput(
                ref context,
                ref output,
                context.CodePattern[(int)context.PrimaryClue],
                context.CodeBitLength[(int)context.PrimaryClue]
            );

            WriteVariableLengthNumber(ref context, ref output, runLength - 1);
        }
        else
        {
            WriteComplexRunLengthSequence(ref context, ref output, runLength);
        }
    }

    /// <summary>
    /// Attempts to encode a delta byte based on the difference between the current
    /// byte and the previous byte, using predefined delta encoding rules.
    /// </summary>
    /// <param name="context">
    /// The compression context containing metadata such as delta clue count,
    /// minimum and maximum delta values, and encoding patterns.
    /// </param>
    /// <param name="output">
    /// The output buffer to which the encoded delta byte is written if encoding is successful.
    /// </param>
    /// <param name="currentByte">
    /// The current byte being processed in the input stream.
    /// </param>
    /// <param name="previousByte">
    /// The previous byte processed in the input stream, used to compute the delta.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the delta byte is successfully encoded and written to the output buffer;
    /// <c>false</c> otherwise.
    /// </returns>
    /// <remarks>
    /// This method checks whether the delta between the current and previous bytes is within
    /// the allowed range and whether the encoding is efficient based on predefined code lengths.
    /// If the conditions are met, the encoded delta byte is written to the output buffer.
    /// </remarks>
    private static bool TryEncodeDeltaByte(
        ref CompressionContext context,
        ref OutputBuffer output,
        uint currentByte,
        uint previousByte
    )
    {
        if (context.DeltaClueCount == 0)
        {
            return false;
        }

        var delta = (int)(currentByte - previousByte);
        if (delta > context.MaxDelta || delta < context.MinDelta)
        {
            return false;
        }

        var deltaIndex = CalculateDeltaIndex(context, currentByte, previousByte, delta);

        if (context.CodeBitLength[deltaIndex] >= context.CodeBitLength[(int)currentByte])
        {
            return false;
        }

        WriteBitsToOutput(
            ref context,
            ref output,
            context.CodePattern[deltaIndex],
            context.CodeBitLength[deltaIndex]
        );

        return true;
    }

    /// <summary>
    /// Encodes a data stream using a combination of Huffman coding and run-length encoding.
    /// </summary>
    /// <param name="context">The compression context containing the input data to be encoded.</param>
    /// <param name="output">The output buffer where the encoded data is written.</param>
    /// <remarks>
    /// This method processes the input data, detects patterns such as repeated bytes for run-length encoding,
    /// and applies Huffman coding for efficient compression. It modifies the output buffer in-place.
    /// </remarks>
    private static void EncodeDataStream(ref CompressionContext context, ref OutputBuffer output)
    {
        var previousByte = 256U;
        var position = 0;

        while (position < context.InputBuffer.Length)
        {
            var currentByte = (uint)context.InputBuffer[position];
            position++; // Consume the current byte

            if (currentByte == previousByte)
            {
                var runLength = CountRunLength(context.InputBuffer, position - 1, currentByte);
                position += (int)runLength - 1; // Skip the run (subtract 1 because we already advanced)

                EncodeRunLengthSequence(ref context, ref output, runLength, previousByte);
            }

            if (!TryEncodeDeltaByte(ref context, ref output, currentByte, previousByte))
            {
                WriteHuffmanCode(ref context, ref output, currentByte);
            }

            previousByte = currentByte;
        }
    }

    /// <summary>
    /// Writes the end-of-file marker to the output buffer, signaling the completion of the compressed data stream.
    /// </summary>
    /// <param name="context">The compression context containing the current state, Huffman codes, and related metadata.</param>
    /// <param name="output">The output buffer to which the end-of-file marker is written.</param>
    /// <remarks>
    /// This method finalizes the encoding process by writing a special marker along with associated metadata,
    /// ensuring proper termination of the compressed data for not enough decoding.
    /// </remarks>
    private static void WriteEndOfFileMarker(
        ref CompressionContext context,
        ref OutputBuffer output
    )
    {
        WriteBitsToOutput(
            ref context,
            ref output,
            context.CodePattern[(int)context.PrimaryClue],
            context.CodeBitLength[(int)context.PrimaryClue]
        );

        WriteVariableLengthNumber(ref context, ref output, 0);
        WriteBitsToOutput(ref context, ref output, 2, 2);
    }

    /// <summary>
    /// Flushes the remaining bits in the output buffer to ensure alignment.
    /// </summary>
    /// <param name="context">The compression context containing state information and tables used during compression.</param>
    /// <param name="output">The output buffer where the remaining bits will be written.</param>
    /// <remarks>
    /// This method ensures the final output is properly aligned by writing the necessary padding bits.
    /// It relies on the `WriteBitsToOutput` method to handle the bit writing operation.
    /// </remarks>
    private static void FlushRemainingBits(
        ref CompressionContext context,
        ref OutputBuffer output
    ) => WriteBitsToOutput(ref context, ref output, 0, 7);

    /// <summary>
    /// Compresses the input data using Huffman encoding with run-length coding and writes the output to the specified buffer.
    /// </summary>
    /// <param name="context">The compression context containing the state and data required for processing.</param>
    /// <param name="inputData">A read-only span of the input data to be compressed.</param>
    /// <param name="outputData">A span of bytes where the compressed data will be written.</param>
    /// <param name="originalLength">The original length of the uncompressed input data.</param>
    /// <param name="deltaLevel">The delta level used for run-length optimization during compression.</param>
    /// <returns>The number of bytes written to the output buffer after compression.</returns>
    /// <remarks>
    /// This method initializes the compression context, analyzes input data, encodes the data stream,
    /// and writes file headers, Huffman tables, and other compression metadata to the output buffer.
    /// </remarks>
    private static int CompressData(
        ref CompressionContext context,
        ReadOnlySpan<byte> inputData,
        Span<byte> outputData,
        int originalLength,
        int deltaLevel
    )
    {
        var outputBuffer = new OutputBuffer(outputData);

        InitializeCompressionContext(ref context, inputData);
        AnalyzeInputStatistics(ref context);
        BuildHuffmanCodes(ref context);

        WriteFileHeader(ref context, ref outputBuffer, originalLength, deltaLevel);
        WriteHuffmanTables(ref context, ref outputBuffer);
        EncodeDataStream(ref context, ref outputBuffer);
        WriteEndOfFileMarker(ref context, ref outputBuffer);
        FlushRemainingBits(ref context, ref outputBuffer);

        return outputBuffer.Position;
    }

    /// <summary>
    /// Encodes a given input data using Huffman coding with run-length compression
    /// and writes the resulting compressed data to the specified buffer.
    /// </summary>
    /// <param name="decompressedData">The input data to be compressed, represented as a read-only span of bytes.</param>
    /// <param name="compressedData">
    /// The buffer to store the compressed output data. The buffer must be large enough
    /// to hold the results, otherwise an exception is thrown.
    /// </param>
    /// <returns>The number of bytes written to the compressed data buffer.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the provided compressed data buffer is too small to hold the compressed output.
    /// </exception>
    public int Encode(ReadOnlySpan<byte> decompressedData, Span<byte> compressedData)
    {
        if (compressedData.Length < decompressedData.Length + 100)
        {
            throw new ArgumentException(
                "Compressed data buffer too small.",
                nameof(compressedData)
            );
        }

        var context = CompressionContext.Create();
        var dataToCompress = PrepareInputData(decompressedData);
        var deltaLevel = Math.Min(DeltaBytesRuns, 2);

        return CompressData(
            ref context,
            dataToCompress,
            compressedData,
            decompressedData.Length,
            deltaLevel
        );
    }
}
