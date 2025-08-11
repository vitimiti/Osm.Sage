using System.Diagnostics.CodeAnalysis;

namespace Osm.Sage.Compression.Eac.Codex;

public partial class BinaryTreeCodex
{
    /// <summary>
    /// Maximum number of byte codes supported (0-255).
    /// </summary>
    private const int MaxByteCodes = 256;

    /// <summary>
    /// Marker value used to indicate unused nodes during frequency analysis.
    /// </summary>
    private const int UnusedNodeMarker = 32000;

    /// <summary>
    /// Safety padding added to working buffers to prevent overflow during transformation.
    /// </summary>
    private const int BufferSafetyPadding = 16384;

    /// <summary>
    /// Encapsulates a data buffer with offset tracking and length management for output operations.
    /// </summary>
    /// <param name="buffer">The underlying byte array to write to.</param>
    /// <param name="offset">The starting offset within the buffer (default: 0).</param>
    /// <remarks>
    /// <para>
    /// This sealed class provides a safe wrapper around byte array operations with
    /// automatic bounds checking and length tracking. It's designed for sequential
    /// write operations where data is appended to the buffer.
    /// </para>
    /// <para>
    /// The buffer automatically handles capacity limits by silently ignoring writes
    /// that would exceed the available space, preventing buffer overruns while
    /// maintaining a consistent API.
    /// </para>
    /// </remarks>
    private sealed class DataBuffer(byte[] buffer, int offset = 0)
    {
        /// <summary>
        /// Gets the current number of bytes written to the buffer.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// The maximum number of bytes that can be written to this buffer.
        /// </summary>
        private int Capacity { get; } = buffer.Length - offset;

        /// <summary>
        /// Writes a single byte to the buffer and advances the length counter.
        /// </summary>
        /// <param name="value">The byte value to write.</param>
        /// <remarks>
        /// If the buffer is at capacity, this method silently returns without
        /// writing, preventing buffer overflow exceptions.
        /// </remarks>
        public void WriteByte(byte value)
        {
            if (Length >= Capacity)
            {
                return;
            }

            buffer[offset + Length] = value;
            Length++;
        }
    }

    /// <summary>
    /// Manages all compression state, working buffers, and intermediate data structures.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This sealed class serves as the central state container for the entire compression
    /// process, consolidating related data structures and providing a clean interface
    /// for state management operations.
    /// </para>
    /// <para>
    /// <b>State Categories:</b>
    /// <list type="bullet">
    /// <item><b>The bit Packing:</b> Manages bit-level output operations and byte boundary handling</item>
    /// <item><b>Node Classification:</b> Tracks the role and relationships of each byte value in the tree</item>
    /// <item><b>Working Data:</b> Input data references and processing parameters</item>
    /// <item><b>Double Buffering:</b> Alternating buffers for efficient data transformation</item>
    /// <item><b>Tree Building:</b> Analysis data and candidate tracking for tree construction</item>
    /// </list>
    /// </para>
    /// <para>
    /// The context uses lazy initialization for large data structures to minimize
    /// memory usage when compression operations are not needed.
    /// </para>
    /// </remarks>
    private sealed class CompressionContext
    {
        #region Bit Packing State
        /// <summary>
        /// The current number of bits accumulated in the bit buffer.
        /// </summary>
        public uint BitBuffer { get; set; }

        /// <summary>
        /// Bit pattern being assembled for output.
        /// </summary>
        public uint BitPattern { get; set; }

        /// <summary>
        /// Total number of bytes written to the output stream.
        /// </summary>
        public uint OutputLength { get; set; }
        #endregion

        #region Bit Manipulation
        /// <summary>
        /// Precomputed bit masks for efficient bit field operations (masks[n] = 2^n-1).
        /// </summary>
        public readonly uint[] BitMasks = new uint[17];
        #endregion

        #region Node Classification Tables
        /// <summary>
        /// Node classification array where each byte's role is defined:
        /// 0 = normal byte, 1 = join left node, 2 = clue node, 3 = clue expansion.
        /// </summary>
        public readonly byte[] NodeClues = new byte[MaxByteCodes];

        /// <summary>
        /// Maps left join nodes to their corresponding right child nodes.
        /// </summary>
        public readonly byte[] RightChildren = new byte[MaxByteCodes];

        /// <summary>
        /// Maps left join nodes to their replacement join node values.
        /// </summary>
        public readonly byte[] JoinNodes = new byte[MaxByteCodes];
        #endregion

        #region Working Data
        /// <summary>
        /// Reference to the input data being compressed.
        /// </summary>
        public ReadOnlyMemory<byte> InputData;

        /// <summary>
        /// Working span view of the current input data segment.
        /// </summary>
        public ReadOnlySpan<byte> InputSpan => InputData.Span;
        #endregion

        #region Double Buffering for Data Transformation
        /// <summary>
        /// Primary working buffer for data transformation operations.
        /// </summary>
        public byte[] Buffer1 = [];

        /// <summary>
        /// Secondary working buffer for data transformation operations.
        /// </summary>
        public byte[] Buffer2 = [];

        /// <summary>
        /// Currently active buffer segment for reading transformed data.
        /// </summary>
        public BufferSegment ActiveBuffer;

        /// <summary>
        /// Currently inactive buffer segment for writing transformed data.
        /// </summary>
        public BufferSegment InactiveBuffer;
        #endregion

        #region Tree Building State
        /// <summary>
        /// Frequency analysis buffer for adjacent byte pair counting (64KB).
        /// </summary>
        public short[] AnalysisBuffer { get; set; } = [];

        /// <summary>
        /// Container for the best compression candidates found during analysis.
        /// </summary>
        public CompressionCandidates Candidates { get; set; }

        /// <summary>
        /// Tuple tracking node availability: (available for joining, candidate for analysis).
        /// </summary>
        public (byte[] available, byte[] candidates) NodeAvailability { get; set; }

        /// <summary>
        /// Frequency count for each byte value in the input.
        /// </summary>
        public uint[] Frequencies { get; set; } = [];

        /// <summary>
        /// Array of byte values sorted by frequency (the least frequent first).
        /// </summary>
        public uint[] SortedNodes { get; set; } = [];

        /// <summary>
        /// The selected clue node (typically the least frequent byte).
        /// </summary>
        public uint ClueNode { get; set; }

        /// <summary>
        /// Next available node index for tree join operations.
        /// </summary>
        public uint NextAvailableNode { get; set; } = 1;

        /// <summary>
        /// List of tree node definitions (parent, left child, right child).
        /// </summary>
        public List<(uint node, uint left, uint right)> TreeNodes { get; set; } = [];
        #endregion

        /// <summary>
        /// Initializes a new compression context and sets up bit manipulation masks.
        /// </summary>
        public CompressionContext() => InitializeBitMasks();

        /// <summary>
        /// Initializes the bit mask lookup table for efficient bit field operations.
        /// </summary>
        /// <remarks>
        /// Creates masks where BitMasks[n] represents a mask with n bits set to 1.
        /// For example, BitMasks[3] = 0x07 (binary: 111), BitMasks[8] = 0xFF.
        /// </remarks>
        private void InitializeBitMasks()
        {
            BitMasks[0] = 0;
            for (uint i = 1; i < 17; i++)
            {
                BitMasks[i] = (BitMasks[i - 1] << 1) + 1;
            }
        }

        /// <summary>
        /// Swaps the active and inactive buffer segments for double-buffering operations.
        /// </summary>
        /// <remarks>
        /// This operation is used during data transformation phases where the algorithm
        /// reads from one buffer while writing to another, then swaps them for the next iteration.
        /// </remarks>
        public void SwapBuffers() =>
            (ActiveBuffer, InactiveBuffer) = (InactiveBuffer, ActiveBuffer);
    }

    /// <summary>
    /// Represents a segment within a working buffer, defining start and end boundaries.
    /// </summary>
    /// <param name="buffer">The underlying byte array for this segment.</param>
    /// <remarks>
    /// <para>
    /// This struct provides a view into a portion of a byte array, allowing algorithms
    /// to work with specific ranges without copying data. It's particularly useful
    /// in double-buffering scenarios where different parts of buffers may contain
    /// valid data at different times.
    /// </para>
    /// <para>
    /// The segment uses inclusive start and end indices, where End = -1 indicates
    /// an empty or uninitialized segment.
    /// </para>
    /// </remarks>
    private struct BufferSegment(byte[] buffer)
    {
        /// <summary>
        /// The underlying byte array containing the segment data.
        /// </summary>
        public readonly byte[] Buffer = buffer;

        /// <summary>
        /// The starting index of valid data within the buffer (inclusive).
        /// </summary>
        public int Start = 0;

        /// <summary>
        /// The ending index of valid data within the buffer (inclusive, -1 for empty).
        /// </summary>
        public int End = -1;

        /// <summary>
        /// Gets a read-only span view of the valid data within this buffer segment.
        /// </summary>
        /// <value>A read-only span covering the valid data range, or empty span if End &lt; Start.</value>
        public readonly ReadOnlySpan<byte> ValidReadOnlySpan =>
            End >= Start && Start >= 0
                ? Buffer.AsSpan(Start, End - Start + 1)
                : ReadOnlySpan<byte>.Empty;
    }

    /// <summary>
    /// Stores and manages the best compression candidates found during frequency analysis.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This struct maintains sorted lists of the most promising byte pair candidates
    /// for tree construction, along with their associated savings potential and
    /// the join nodes that would be created.
    /// </para>
    /// <para>
    /// The candidates are kept sorted by saving value in descending order, allowing
    /// the algorithm to quickly identify and process the most beneficial compressions first.
    /// </para>
    /// </remarks>
    private struct CompressionCandidates()
    {
        /// <summary>
        /// Array of node pairs encoded as (high_byte <![CDATA[<]]><![CDATA[<]]> 8) | low_byte.
        /// </summary>
        public readonly uint[] NodePairs = new uint[MaxByteCodes];

        /// <summary>
        /// Array of potential compression savings for each candidate pair.
        /// </summary>
        public readonly uint[] Savings = new uint[MaxByteCodes];

        /// <summary>
        /// Array of join node IDs that would be created for each candidate.
        /// </summary>
        public readonly byte[] JoinNodes = new byte[MaxByteCodes];

        /// <summary>
        /// Resets the candidate list by setting a sentinel value for empty state detection.
        /// </summary>
        /// <remarks>
        /// The reset operation sets Savings[0] to uint.MaxValue, which serves as a
        /// sentinel to indicate that no valid candidates have been inserted yet.
        /// </remarks>
        public void Reset() => Savings[0] = uint.MaxValue;
    }

    /// <summary>
    /// Gets or sets whether zero suppression optimization should be applied.
    /// </summary>
    /// <value>
    /// <c>true</c> to enable zero suppression (excludes control characters 0-31 from compression);
    /// <c>false</c> to compress all byte values normally.
    /// </value>
    /// <remarks>
    /// Zero suppression is useful for text-based data where control characters are rare,
    /// and excluding them can improve compression efficiency for printable characters.
    /// </remarks>
    public bool ShouldZeroSuppress { get; set; }

    /// <summary>
    /// Writes a variable number of bits to the output stream with automatic byte alignment.
    /// </summary>
    /// <param name="context">The compression context containing the bit packing state.</param>
    /// <param name="output">The output buffer to write completed bytes to.</param>
    /// <param name="bits">The bit values to write.</param>
    /// <param name="bitCount">The number of bits to write from the bit parameter.</param>
    /// <remarks>
    /// <para>
    /// This method handles the complex process of packing variable-width bit fields
    /// into byte-aligned output. It accumulates bits in an internal buffer and
    /// outputs complete bytes as they become available.
    /// </para>
    /// <para>
    /// <b>The Bit Packing Strategy:</b>
    /// <list type="bullet">
    /// <item>Bits are accumulated in a 32-bit buffer with bit counting</item>
    /// <item>When 8 or more bits are available, complete bytes are output</item>
    /// <item>Large bit counts (>16) are processed iteratively to avoid overflow</item>
    /// <item>Remaining bits stay in the buffer for the next write operation</item>
    /// </list>
    /// </para>
    /// <para>
    /// The method is optimized for the common case of writing 8-bit values while
    /// still supporting arbitrary bit widths needed for tree structure encoding.
    /// </para>
    /// </remarks>
    private static void WriteBitsToStream(
        CompressionContext context,
        DataBuffer output,
        uint bits,
        uint bitCount
    )
    {
        // Handle large bit counts iteratively to avoid recursion overhead
        while (bitCount > 16)
        {
            // Process the high bits first
            uint highBitCount = bitCount - 16;
            uint highBits = bits >> 16;

            // Write the high bits
            context.BitBuffer += highBitCount;
            context.BitPattern +=
                (highBits & context.BitMasks[highBitCount]) << (int)(24 - context.BitBuffer);

            while (context.BitBuffer > 7)
            {
                output.WriteByte((byte)(context.BitPattern >> 16));
                context.BitPattern <<= 8;
                context.BitBuffer -= 8;
                context.OutputLength++;
            }

            // Prepare for the next iteration with the low 16 bits
            bits &= 0xFFFF;
            bitCount = 16;
        }

        // Handle the remaining bits (≤ 16)
        context.BitBuffer += bitCount;
        context.BitPattern += (bits & context.BitMasks[bitCount]) << (int)(24 - context.BitBuffer);

        while (context.BitBuffer > 7)
        {
            output.WriteByte((byte)(context.BitPattern >> 16));
            context.BitPattern <<= 8;
            context.BitBuffer -= 8;
            context.OutputLength++;
        }
    }

    /// <summary>
    /// Counts frequencies of adjacent byte pairs for compression candidate analysis.
    /// </summary>
    /// <param name="data">The data span to analyze.</param>
    /// <param name="frequencies">Frequency accumulation span (65536 entries for all 16-bit pairs).</param>
    /// <remarks>
    /// <para>
    /// This method performs sliding window analysis to count how often each possible
    /// byte pair appears adjacently in the input data. The frequency information
    /// drives the selection of optimal compression candidates.
    /// </para>
    /// <para>
    /// <b>Optimization Features:</b>
    /// <list type="bullet">
    /// <item>Chunk-based processing (16-byte blocks) for improved cache performance</item>
    /// <item>Rolling pattern technique to avoid redundant bit operations</item>
    /// <item>Early termination on empty ranges to avoid unnecessary work</item>
    /// <item>Span-based operations for improved bounds safety and performance</item>
    /// </list>
    /// </para>
    /// <para>
    /// The method uses a 16-bit rolling pattern where each byte pair (high_byte, low_byte)
    /// is encoded as (high_byte <![CDATA[<]]><![CDATA[<]]> 8) | low_byte, providing direct indexing into
    /// the 65536-element frequency array.
    /// </para>
    /// </remarks>
    private static void CountAdjacentPairs(ReadOnlySpan<byte> data, Span<short> frequencies)
    {
        if (data.IsEmpty)
        {
            return;
        }

        ushort rollingPattern = data[0];
        int index = 1;
        int fastProcessEnd = Math.Max(1, data.Length - 16);

        // Process in 16-byte chunks for efficiency
        while (index < fastProcessEnd)
        {
            for (var offset = 0; offset < 16 && index + offset < data.Length; offset++)
            {
                rollingPattern = (ushort)((rollingPattern << 8) | data[index + offset]);
                frequencies[rollingPattern]++;
            }

            index += 16;
        }

        // Process remaining bytes
        while (index < data.Length)
        {
            rollingPattern = (ushort)((rollingPattern << 8) | data[index]);
            frequencies[rollingPattern]++;
            index++;
        }
    }

    /// <summary>
    /// Resets frequency counters for active byte codes in preparation for a new analysis pass.
    /// </summary>
    /// <param name="activeNodes">Span indicating which nodes are active (non-zero = active).</param>
    /// <param name="frequencies">The frequency span to reset (256 entries per node).</param>
    /// <remarks>
    /// <para>
    /// This method prepares the frequency analysis arrays for a fresh counting pass
    /// by clearing the frequency data for all currently active nodes while also
    /// normalizing their active status to 1.
    /// </para>
    /// <para>
    /// The frequency array is organized as 256 consecutive blocks of 256 entries each,
    /// where block N contains frequencies for all byte pairs starting with byte N.
    /// Only blocks corresponding to active nodes are cleared to optimize performance.
    /// </para>
    /// </remarks>
    private static void ResetFrequencyCounters(Span<byte> activeNodes, Span<short> frequencies)
    {
        var frequencyIndex = 0;
        for (var nodeId = 0; nodeId < MaxByteCodes; nodeId++)
        {
            if (activeNodes[nodeId] != 0)
            {
                activeNodes[nodeId] = 1;
                // Clear 256 frequency entries for this node
                frequencies.Slice(frequencyIndex, 256).Clear();
            }

            frequencyIndex += 256;
        }
    }

    /// <summary>
    /// Applies node transformations to the data stream using the current tree configuration.
    /// </summary>
    /// <param name="context">The compression context containing buffers and node configuration.</param>
    /// <param name="clueNode">The clue node value used for special transformations.</param>
    /// <remarks>
    /// <para>
    /// This method performs data transformation by applying the constructed binary tree
    /// rules to convert byte sequences into more compressible forms. It uses double
    /// buffering to efficiently process the data without in-place modifications.
    /// </para>
    /// <para>
    /// <b>Transformation Process:</b>
    /// <list type="number">
    /// <item>Set up source and destination buffers (swapping active/inactive)</item>
    /// <item>Place sentinel value at buffer boundary for processing control</item>
    /// <item>Copy bytes until a marked node (NodeClues != 0) is encountered</item>
    /// <item>Apply appropriate transformation based on the node's classification</item>
    /// <item>Continue until all data is processed</item>
    /// </list>
    /// </para>
    /// <para>
    /// The method handles three types of node transformations as defined in
    /// <see cref="ApplyNodeTransformation"/>.
    /// </para>
    /// </remarks>
    private static void TransformDataStream(CompressionContext context, uint clueNode)
    {
        var source = context.ActiveBuffer;
        context.SwapBuffers();
        var destination = context.ActiveBuffer;

        // Set sentinel value for processing boundary
        if (source.End >= 0 && source.End < source.Buffer.Length - 1)
        {
            source.Buffer[source.End + 1] = (byte)clueNode;
        }

        var sourceSpan = source.ValidReadOnlySpan;
        var destinationSpan = destination.Buffer.AsSpan();
        var destIndex = 0;

        for (
            var sourceIndex = 0;
            sourceIndex < sourceSpan.Length && destIndex < destinationSpan.Length;
            sourceIndex++
        )
        {
            destinationSpan[destIndex++] = sourceSpan[sourceIndex];

            if (context.NodeClues[sourceSpan[sourceIndex]] != 0)
            {
                ApplyNodeTransformation(
                    context,
                    sourceSpan,
                    ref sourceIndex,
                    destinationSpan,
                    ref destIndex,
                    sourceSpan[sourceIndex],
                    clueNode
                );
            }
        }

        destination.Start = 0;
        destination.End = Math.Max(0, destIndex - 2);
    }

    /// <summary>
    /// Applies specific transformation rules based on the node classification type.
    /// </summary>
    /// <param name="context">The compression context containing node configuration tables.</param>
    /// <param name="source">Source span containing the original data.</param>
    /// <param name="sourceIndex">Current reading position in the source span (modified).</param>
    /// <param name="dest">Destination span for transformed data.</param>
    /// <param name="destIndex">Current writing position in the destination span (modified).</param>
    /// <param name="currentByte">The byte value triggering this transformation.</param>
    /// <param name="clueNode">The designated clue node value.</param>
    /// <remarks>
    /// <para>
    /// This method implements the core transformation logic based on node classifications
    /// stored in the NodeClues array:
    /// </para>
    /// <para>
    /// <b>Transformation Types:</b>
    /// <list type="bullet">
    /// <item><b>Case 1 (Join):</b> If the current byte is a left join node and the next byte
    /// matches its right child, replace the pair with the designated join node</item>
    /// <item><b>Case 3 (Clue Expansion):</b> Replace the current byte with the clue node
    /// followed by the original byte value</item>
    /// <item><b>Default:</b> Copy the next byte without transformation</item>
    /// </list>
    /// </para>
    /// <para>
    /// These transformations create patterns that can be more efficiently encoded
    /// in the final compressed output by leveraging the binary tree structure.
    /// </para>
    /// </remarks>
    private static void ApplyNodeTransformation(
        CompressionContext context,
        ReadOnlySpan<byte> source,
        ref int sourceIndex,
        Span<byte> dest,
        ref int destIndex,
        byte currentByte,
        uint clueNode
    )
    {
        switch (context.NodeClues[currentByte])
        {
            case 1: // Join transformation
                if (
                    sourceIndex + 1 < source.Length
                    && source[sourceIndex + 1] == context.RightChildren[currentByte]
                )
                {
                    if (destIndex > 0)
                    {
                        dest[destIndex - 1] = context.JoinNodes[currentByte];
                    }

                    sourceIndex++; // Skip the right child as it's been consumed
                }

                break;

            case 3: // Clue expansion
                if (destIndex > 0)
                {
                    dest[destIndex - 1] = (byte)clueNode;
                }

                if (destIndex < dest.Length)
                {
                    dest[destIndex++] = currentByte;
                }

                break;

            default: // Copy next byte
                if (sourceIndex + 1 < source.Length && destIndex < dest.Length)
                {
                    dest[destIndex++] = source[sourceIndex + 1];
                    sourceIndex++; // Consume the copied byte
                }

                break;
        }
    }

    /// <summary>
    /// Analyzes frequency data to identify the most promising compression candidates.
    /// </summary>
    /// <param name="frequencies">Span of adjacent byte pair frequencies.</param>
    /// <param name="activeNodes">Span indicating which nodes are available for analysis.</param>
    /// <param name="candidates">Structure to store the best candidates found.</param>
    /// <param name="compressionRatio">Ratio used for threshold calculations.</param>
    /// <returns>The total number of valid candidates identified.</returns>
    /// <remarks>
    /// <para>
    /// This method scans through all possible byte pair combinations to identify
    /// those with sufficient frequency to justify creating tree join operations.
    /// It maintains a sorted list of the best candidates based on their potential
    /// compression savings.
    /// </para>
    /// <para>
    /// <b>Analysis Process:</b>
    /// <list type="number">
    /// <item>Iterate through all possible byte pairs (256×256 = 65,536 combinations)</item>
    /// <item>Skip pairs where either byte is inactive or the frequency is too low</item>
    /// <item>Calculate potential savings and insert viable candidates</item>
    /// <item>Dynamically adjust the threshold based on current best candidates</item>
    /// <item>Return the total count of candidates meeting the criteria</item>
    /// </list>
    /// </para>
    /// <para>
    /// The method uses an adaptive threshold system that becomes more selective
    /// as better candidates are found, ensuring only the most beneficial
    /// compressions are considered.
    /// </para>
    /// </remarks>
    private static uint AnalyzeCompressionCandidates(
        ReadOnlySpan<short> frequencies,
        ReadOnlySpan<byte> activeNodes,
        CompressionCandidates candidates,
        int compressionRatio
    )
    {
        var threshold = 3U;
        var candidateCount = 1U;
        var baseNodeValue = 0U;
        var frequencyIndex = 0;

        for (var highByte = 0; highByte < MaxByteCodes; highByte++)
        {
            if (activeNodes[highByte] != 0)
            {
                for (var lowByte = 0; lowByte < MaxByteCodes; lowByte++)
                {
                    var frequency = frequencies[frequencyIndex++];
                    if (frequency <= threshold || activeNodes[lowByte] == 0)
                    {
                        continue;
                    }

                    InsertCandidate(
                        candidates,
                        ref candidateCount,
                        baseNodeValue + (uint)lowByte,
                        (uint)frequency,
                        compressionRatio
                    );

                    threshold = CalculateNewThreshold(candidates, candidateCount, compressionRatio);
                }
            }
            else
            {
                frequencyIndex += MaxByteCodes;
            }

            baseNodeValue += MaxByteCodes;
        }

        return candidateCount;
    }

    /// <summary>
    /// Inserts a compression candidate into the sorted list while maintaining order and size limits.
    /// </summary>
    /// <param name="candidates">The candidates structure to insert into.</param>
    /// <param name="candidateCount">Current number of candidates (modified if insertion occurs).</param>
    /// <param name="nodePair">The byte pair encoded as (high <![CDATA[<]]><![CDATA[<]]> 8) | low.</param>
    /// <param name="savings">The potential compression savings for this pair.</param>
    /// <param name="compressionRatio">Ratio used for pruning low-value candidates.</param>
    /// <remarks>
    /// <para>
    /// This method maintains a sorted list of compression candidates ordered by
    /// their potential savings value. It uses insertion sort logic to place new
    /// candidates in the correct position while shifting existing entries as needed.
    /// </para>
    /// <para>
    /// <b>Insertion Process:</b>
    /// <list type="number">
    /// <item>Find the correct insertion position by comparing saving values</item>
    /// <item>Shift existing candidates to make room for the new entry</item>
    /// <item>Insert the new candidate at the determined position</item>
    /// <item>Increment candidate count if space allows (maximum 48 candidates)</item>
    /// <item>Remove low-value candidates that fall below the dynamic threshold</item>
    /// </list>
    /// </para>
    /// <para>
    /// The method enforces a maximum of 48 candidates and uses a dynamic threshold
    /// system to automatically prune candidates that are significantly less valuable
    /// than the current best options.
    /// </para>
    /// </remarks>
    private static void InsertCandidate(
        CompressionCandidates candidates,
        ref uint candidateCount,
        uint nodePair,
        uint savings,
        int compressionRatio
    )
    {
        // Find the insertion point
        var insertPosition = candidateCount;
        while (insertPosition > 0 && candidates.Savings[insertPosition - 1] < savings)
        {
            if (insertPosition < candidates.NodePairs.Length)
            {
                candidates.NodePairs[insertPosition] = candidates.NodePairs[insertPosition - 1];
                candidates.Savings[insertPosition] = candidates.Savings[insertPosition - 1];
            }

            insertPosition--;
        }

        // Insert a new candidate
        if (insertPosition < candidates.NodePairs.Length)
        {
            candidates.NodePairs[insertPosition] = nodePair;
            candidates.Savings[insertPosition] = savings;
        }

        if (candidateCount < 48)
        {
            candidateCount++;
        }

        // Remove candidates below a threshold
        while (
            candidateCount > 1
            && candidates.Savings[candidateCount - 1]
                < (candidates.Savings[1] / (uint)compressionRatio)
        )
        {
            candidateCount--;
        }
    }

    /// <summary>
    /// Calculates the dynamic threshold for candidate selection based on current best candidates.
    /// </summary>
    /// <param name="candidates">The current candidates list.</param>
    /// <param name="candidateCount">Number of valid candidates currently stored.</param>
    /// <param name="compressionRatio">Base ratio for threshold calculation.</param>
    /// <returns>The calculated threshold value for candidate filtering.</returns>
    /// <remarks>
    /// <para>
    /// This method implements an adaptive threshold system that becomes more selective
    /// as the candidate list fills up. The threshold is calculated relative to the
    /// current best candidates to maintain a high-quality selection.
    /// </para>
    /// <para>
    /// <b>Threshold Logic:</b>
    /// <list type="bullet">
    /// <item><b>Space Available:</b> Use the second-best candidate divided by the compression ratio</item>
    /// <item><b>List Full:</b> Use the worst current candidate as the minimum threshold</item>
    /// </list>
    /// </para>
    /// <para>
    /// This approach ensures that only candidates with meaningful compression potential
    /// are retained, improving both compression quality and algorithm performance.
    /// </para>
    /// </remarks>
    private static uint CalculateNewThreshold(
        CompressionCandidates candidates,
        uint candidateCount,
        int compressionRatio
    ) =>
        candidateCount < 48
            ? candidates.Savings[1] / (uint)compressionRatio
            : candidates.Savings[candidateCount - 1];

    /// <summary>
    /// Main compression orchestration method that coordinates the entire Binary Tree compression process.
    /// </summary>
    /// <param name="context">The compression context containing input data and state.</param>
    /// <param name="output">The output buffer for compressed data.</param>
    /// <param name="maxPasses">Maximum number of optimization passes to perform.</param>
    /// <param name="maxMultiJoins">Maximum number of simultaneous join operations per pass.</param>
    /// <param name="enableZeroSuppression">Whether to exclude control characters (0-31) from compression.</param>
    /// <remarks>
    /// <para>
    /// This method orchestrates the complete compression process by coordinating all
    /// phases from initial frequency analysis through final data output. It serves
    /// as the main entry point for the compression algorithm.
    /// </para>
    /// <para>
    /// <b>Compression Phases:</b>
    /// <list type="number">
    /// <item><b>Buffer Setup:</b> Initialize working buffers and copy input data</item>
    /// <item><b>Frequency Analysis:</b> Count individual byte frequencies</item>
    /// <item><b>Node Initialization:</b> Set up availability tracking and apply zero suppression</item>
    /// <item><b>Sorting:</b> Create the frequency-sorted node list for optimal selection</item>
    /// <item><b>Clue Selection:</b> Choose and configure the special clue node</item>
    /// <item><b>Tree Building:</b> Iteratively construct the optimal binary tree</item>
    /// <item><b>Output Generation:</b> Write tree structure and compressed data</item>
    /// </list>
    /// </para>
    /// <para>
    /// The method allocates a 128KB analysis buffer for frequency counting operations,
    /// which is sized to handle all possible 16-bit byte pair combinations efficiently.
    /// </para>
    /// </remarks>
    private static void CompressWithBinaryTree(
        CompressionContext context,
        DataBuffer output,
        uint maxPasses,
        uint maxMultiJoins,
        bool enableZeroSuppression = false
    )
    {
        context.AnalysisBuffer = new short[65536]; // 128KB for frequency analysis
        SetupWorkingBuffers(context);

        context.Frequencies = CountByteFrequencies(context);
        context.NodeAvailability = InitializeNodeAvailability(
            context.Frequencies,
            enableZeroSuppression
        );

        context.SortedNodes = CreateFrequencySortedNodes(context.Frequencies);

        context.ClueNode = SelectClueNode(
            context.SortedNodes,
            context.NodeAvailability,
            context,
            context.Frequencies
        );

        context.Candidates = new CompressionCandidates();
        context.Candidates.Reset();

        var treeNodes = BuildCompressionTree(context, maxPasses, maxMultiJoins);

        WriteCompressionData(context, output, context.ClueNode, treeNodes);
    }

    /// <summary>
    /// Initializes the working buffers required for data transformation operations.
    /// </summary>
    /// <param name="context">The compression context to configure with working buffers.</param>
    /// <remarks>
    /// <para>
    /// This method sets up the double-buffering system used during data transformation
    /// phases. The buffer size is calculated to accommodate worst-case expansion scenarios
    /// with additional safety padding.
    /// </para>
    /// <para>
    /// <b>Buffer Configuration:</b>
    /// <list type="bullet">
    /// <item><b>Size:</b> 1.5× input size + 16KB safety padding</item>
    /// <item><b>Primary (Buffer1):</b> Initially contains input data and serves as an active buffer</item>
    /// <item><b>Secondary (Buffer2):</b> Initially empty and serves as an inactive buffer</item>
    /// <item><b>Segments:</b> Define valid data regions within each buffer</item>
    /// </list>
    /// </para>
    /// <para>
    /// The oversized allocation ensures that data transformations, which may temporarily
    /// expand the data, never exceed buffer boundaries and cause corruption or exceptions.
    /// </para>
    /// </remarks>
    private static void SetupWorkingBuffers(CompressionContext context)
    {
        var inputLength = context.InputSpan.Length;
        var bufferSize = inputLength * 3 / 2 + BufferSafetyPadding;

        context.Buffer1 = new byte[bufferSize];
        context.Buffer2 = new byte[bufferSize];

        context.InputSpan.CopyTo(context.Buffer1.AsSpan());

        context.ActiveBuffer = new BufferSegment(context.Buffer1)
        {
            Start = 0,
            End = inputLength - 1,
        };

        context.InactiveBuffer = new BufferSegment(context.Buffer2);
    }

    /// <summary>
    /// Counts the frequency of occurrence for each byte value in the input data.
    /// </summary>
    /// <param name="context">The compression context containing input data parameters.</param>
    /// <returns>Array of 256 frequency counts, indexed by byte value.</returns>
    /// <remarks>
    /// <para>
    /// This method performs the initial statistical analysis of the input data by
    /// counting how many times each possible byte value (0-255) appears. This
    /// frequency information is crucial for optimal tree construction and clue node selection.
    /// </para>
    /// <para>
    /// The frequency counts directly influence compression effectiveness:
    /// <list type="bullet">
    /// <item><b>High-frequency bytes:</b> Become candidates for tree join operations</item>
    /// <item><b>Low-frequency bytes:</b> May be selected as clue nodes or suppressed</item>
    /// <item><b>Zero-frequency bytes:</b> Are marked as unused and excluded from processing</item>
    /// </list>
    /// </para>
    /// <para>
    /// The method processes the entire input span using efficient span-based operations
    /// for improved performance and bounds' safety.
    /// </para>
    /// </remarks>
    private static uint[] CountByteFrequencies(CompressionContext context)
    {
        var frequencies = new uint[MaxByteCodes];
        var inputSpan = context.InputSpan;

        foreach (var byteValue in inputSpan)
        {
            frequencies[byteValue]++;
        }

        return frequencies;
    }

    /// <summary>
    /// Initializes node availability tracking based on frequency analysis and compression options.
    /// </summary>
    /// <param name="frequencies">Array of byte frequency counts.</param>
    /// <param name="enableZeroSuppression">Whether to exclude control characters from compression.</param>
    /// <returns>Tuple containing availability arrays for joining and candidate analysis.</returns>
    /// <remarks>
    /// <para>
    /// This method sets up the tracking systems that determine which byte values
    /// are available for various compression operations. It creates two parallel
    /// tracking arrays with different purposes:
    /// </para>
    /// <para>
    /// <b>Array Purposes:</b>
    /// <list type="bullet">
    /// <item><b>Available:</b> Tracks bytes available for tree join operations (1=available, 0=used)</item>
    /// <item><b>Candidates:</b> Tracks bytes eligible for frequency analysis (1=eligible, 0=excluded)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Initialization Rules:</b>
    /// <list type="bullet">
    /// <item>All bytes start as available for joining</item>
    /// <item>Only bytes with frequency > 3 are eligible as analysis candidates</item>
    /// <item>Byte 0 is reserved and marked as unused</item>
    /// <item>Control characters (0-31) are suppressed if zero suppression is enabled</item>
    /// </list>
    /// </para>
    /// </remarks>
    private static (byte[] available, byte[] candidates) InitializeNodeAvailability(
        Span<uint> frequencies,
        bool enableZeroSuppression = false
    )
    {
        var available = new byte[MaxByteCodes];
        var candidates = new byte[MaxByteCodes];

        for (var i = 0; i < MaxByteCodes; i++)
        {
            available[i] = 1;
            candidates[i] = frequencies[i] > 3 ? (byte)1 : (byte)0;
        }

        // Reserve node 0
        frequencies[0] = UnusedNodeMarker;

        // Apply zero suppression if enabled
        if (enableZeroSuppression)
        {
            ApplyZeroSuppression(frequencies, available, candidates);
        }

        return (available, candidates);
    }

    /// <summary>
    /// Applies zero suppression by excluding control characters (0-31) from compression operations.
    /// </summary>
    /// <param name="frequencies">Frequency span to modify.</param>
    /// <param name="available">Availability span to modify.</param>
    /// <param name="candidates">Candidates span to modify.</param>
    /// <remarks>
    /// <para>
    /// Zero suppression is an optimization technique useful for text-based data
    /// where control characters (ASCII 0-31) are rare or absent. By excluding
    /// these characters from compression operations, more resources can be devoted
    /// to compressing the printable characters that actually appear in the data.
    /// </para>
    /// <para>
    /// <b>Suppression Effects:</b>
    /// <list type="bullet">
    /// <item>Control characters are marked with unused frequency (32,000)</item>
    /// <item>They are excluded from candidate analysis (candidates[i] = 0)</item>
    /// <item>They are marked as unavailable for joining (available[i] = 0)</item>
    /// <item>This frees up 32 node positions for more effective use</item>
    /// </list>
    /// </para>
    /// </remarks>
    private static void ApplyZeroSuppression(
        Span<uint> frequencies,
        Span<byte> available,
        Span<byte> candidates
    )
    {
        for (var i = 0; i < 32; i++)
        {
            frequencies[i] = UnusedNodeMarker;
            candidates[i] = 0;
            available[i] = 0;
        }
    }

    /// <summary>
    /// Creates an array of byte values sorted by frequency in ascending order (the least frequent first).
    /// </summary>
    /// <param name="frequencies">Array of frequency counts for each byte value.</param>
    /// <returns>Array of byte values sorted by frequency, then by byte value for ties.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a sorted ordering of all byte values based on their
    /// frequency of occurrence. The sorting is crucial for optimal compression
    /// as it allows the algorithm to prioritize operations on less common bytes.
    /// </para>
    /// <para>
    /// <b>Sorting Criteria:</b>
    /// <list type="number">
    /// <item><b>Primary:</b> Frequency count (ascending - the least frequent first)</item>
    /// <item><b>Secondary:</b> Byte value (descending - larger values first for ties)</item>
    /// </list>
    /// </para>
    /// <para>
    /// The method uses bubble sort, which provides stable sorting behavior essential
    /// for maintaining consistent results across different runs with identical input.
    /// While not the fastest sorting algorithm, bubble sort's stability and simplicity
    /// are more important than speed for the small 256-element arrays used here.
    /// </para>
    /// </remarks>
    private static uint[] CreateFrequencySortedNodes(ReadOnlySpan<uint> frequencies)
    {
        var sortedNodes = new uint[MaxByteCodes];
        for (uint i = 0; i < MaxByteCodes; i++)
        {
            sortedNodes[i] = i;
        }

        // Bubble sort by frequency (stable sort preserving secondary key order)
        var hasSwaps = true;
        while (hasSwaps)
        {
            hasSwaps = false;
            for (var i = 1; i < MaxByteCodes; i++)
            {
                if (!ShouldSwapNodes(frequencies, sortedNodes, i))
                {
                    continue;
                }

                (sortedNodes[i], sortedNodes[i - 1]) = (sortedNodes[i - 1], sortedNodes[i]);
                hasSwaps = true;
            }
        }

        return sortedNodes;
    }

    /// <summary>
    /// Determines whether two adjacent nodes in the sorted array should be swapped.
    /// </summary>
    /// <param name="frequencies">Span of frequency counts.</param>
    /// <param name="sortedNodes">Array of nodes being sorted.</param>
    /// <param name="index">Index of the current node to compare with the previous.</param>
    /// <returns><c>true</c> if the nodes should be swapped; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// This helper method implements the comparison logic for the stable bubble sort,
    /// ensuring that nodes are ordered by frequency first, then by value for ties.
    /// </remarks>
    private static bool ShouldSwapNodes(
        ReadOnlySpan<uint> frequencies,
        uint[] sortedNodes,
        int index
    )
    {
        var current = sortedNodes[index];
        var previous = sortedNodes[index - 1];

        return frequencies[(int)current] < frequencies[(int)previous]
            || (frequencies[(int)current] == frequencies[(int)previous] && current > previous);
    }

    /// <summary>
    /// Selects and configures the clue node, typically the least frequent byte in the input.
    /// </summary>
    /// <param name="sortedNodes">Array of nodes sorted by frequency (the least frequent first).</param>
    /// <param name="nodeAvailability">Tuple containing node availability tracking arrays.</param>
    /// <param name="context">The compression context to configure.</param>
    /// <param name="frequencies">Array of byte frequency counts.</param>
    /// <returns>The selected clue node value.</returns>
    /// <remarks>
    /// <para>
    /// The clue node serves a special role in the Binary Tree compression algorithm
    /// as a sentinel value that helps distinguish between different types of encoded
    /// sequences in the compressed data stream.
    /// </para>
    /// <para>
    /// <b>Clue Node Selection Process:</b>
    /// <list type="number">
    /// <item>Select the least frequent byte (first in the sorted array)</item>
    /// <item>Mark it as unavailable for normal join operations</item>
    /// <item>Configure it initially for clue expansion (NodeClues = 3)</item>
    /// <item>Apply data transformation if the byte actually appears in input</item>
    /// <item>Finalize configuration as clue node (NodeClues = 2)</item>
    /// </list>
    /// </para>
    /// <para>
    /// The temporary assignment of value 3 followed by value 2 is intentional and
    /// supports the transformation process where the clue node may need different
    /// handling during intermediate steps.
    /// </para>
    /// </remarks>
    [SuppressMessage(
        "csharpsquid",
        "S4143:Collection elements should not be replaced unconditionally",
        Justification = "This is intentional, as it is used in the `TransformDataStream` method."
    )]
    private static uint SelectClueNode(
        uint[] sortedNodes,
        (byte[] available, byte[] candidates) nodeAvailability,
        CompressionContext context,
        uint[] frequencies
    )
    {
        var clueNode = sortedNodes[0];
        nodeAvailability.available[clueNode] = 0;
        nodeAvailability.candidates[clueNode] = 0;

        // Temporarily mark as clue expansion node for transformation
        context.NodeClues[clueNode] = 3;

        if (frequencies[clueNode] != 0)
        {
            TransformDataStream(context, clueNode);
        }

        // Set the final clue node state
        context.NodeClues[clueNode] = 2;
        return clueNode;
    }

    /// <summary>
    /// Builds the optimal compression tree through iterative analysis and candidate processing.
    /// </summary>
    /// <param name="context">The compression context containing all necessary state data.</param>
    /// <param name="maxPasses">Maximum number of optimization passes to perform.</param>
    /// <param name="maxMultiJoins">Maximum number of join operations to perform per pass.</param>
    /// <returns>List of tree node definitions created during the building process.</returns>
    /// <remarks>
    /// <para>
    /// This method implements the core optimization loop of the Binary Tree compression
    /// algorithm. It iteratively analyzes the current data state, identifies the best
    /// compression opportunities, and applies transformations to build an optimal tree structure.
    /// </para>
    /// <para>
    /// <b>Iterative Process:</b>
    /// <list type="number">
    /// <item><b>Frequency Analysis:</b> Count adjacent byte pair frequencies in current data</item>
    /// <item><b>Candidate Selection:</b> Identify the most promising compression opportunities</item>
    /// <item><b>Join Processing:</b> Create tree join operations for selected candidates</item>
    /// <item><b>Data Transformation:</b> Apply the new joins to transform the data</item>
    /// <item><b>State Restoration:</b> Reset temporary node states for the next iteration</item>
    /// </list>
    /// </para>
    /// <para>
    /// The algorithm continues until no beneficial candidates are found or the maximum
    /// number of passes is reached. Each successful pass typically improves the compression
    /// ratio at the cost of increased processing time and tree complexity.
    /// </para>
    /// </remarks>
    private static List<(uint node, uint left, uint right)> BuildCompressionTree(
        CompressionContext context,
        uint maxPasses,
        uint maxMultiJoins
    )
    {
        context.TreeNodes.Clear();
        var passesRemaining = maxPasses;

        while (passesRemaining > 0)
        {
            ResetFrequencyCounters(
                context.NodeAvailability.candidates.AsSpan(),
                context.AnalysisBuffer.AsSpan()
            );

            var currentBuffer = context.ActiveBuffer;
            CountAdjacentPairs(currentBuffer.ValidReadOnlySpan, context.AnalysisBuffer.AsSpan());

            var candidateCount = AnalyzeCompressionCandidates(
                context.AnalysisBuffer.AsSpan(),
                context.NodeAvailability.candidates.AsSpan(),
                context.Candidates,
                2
            );

            if (candidateCount <= 1)
            {
                break;
            }

            var joinCount = ProcessCompressionCandidates(context, candidateCount, maxMultiJoins);

            if (joinCount > 1)
            {
                TransformDataStream(context, context.ClueNode);
                RestoreNodeStates(context, joinCount);
                passesRemaining--;
            }
            else
            {
                break;
            }
        }

        return context.TreeNodes;
    }

    /// <summary>
    /// Processes the identified compression candidates to create actual tree join operations.
    /// </summary>
    /// <param name="context">The compression context containing candidates and node state.</param>
    /// <param name="candidateCount">Number of valid candidates available for processing.</param>
    /// <param name="maxMultiJoins">Maximum number of join operations to create in this pass.</param>
    /// <returns>The number of join operations actually created.</returns>
    /// <remarks>
    /// <para>
    /// This method evaluates each compression candidate to determine if creating a
    /// tree join operation would be beneficial. It performs cost-benefit analysis
    /// and creates tree joins for candidates that meet the criteria.
    /// </para>
    /// <para>
    /// <b>Processing Steps:</b>
    /// <list type="number">
    /// <item>Extract left and right node IDs from each candidate pair encoding</item>
    /// <item>Verify both nodes are still available for joining</item>
    /// <item>Find an available node to serve as the join node</item>
    /// <item>Calculate the cost of using this node versus the potential savings</item>
    /// <item>Create the tree join if cost-effective and update tracking structures</item>
    /// </list>
    /// </para>
    /// <para>
    /// The method respects the maxMultiJoins limit to prevent excessive tree complexity
    /// in a single pass, which could lead to suboptimal overall compression.
    /// </para>
    /// </remarks>
    private static uint ProcessCompressionCandidates(
        CompressionContext context,
        uint candidateCount,
        uint maxMultiJoins
    )
    {
        var joinCount = 1U;
        for (
            var candidateIndex = 1U;
            candidateIndex < candidateCount && joinCount <= maxMultiJoins;
            candidateIndex++
        )
        {
            var leftNode = (context.Candidates.NodePairs[candidateIndex] >> 8) & 0xFF;
            var rightNode = context.Candidates.NodePairs[candidateIndex] & 0xFF;

            if (
                context.NodeAvailability.candidates[leftNode] != 1
                || context.NodeAvailability.candidates[rightNode] != 1
            )
            {
                continue;
            }

            var joinNode = FindNextAvailableNode(context);
            if (joinNode >= MaxByteCodes)
            {
                break;
            }

            var cost = 3 + context.Frequencies[joinNode];
            if (cost >= context.Candidates.Savings[candidateIndex])
            {
                continue;
            }

            CreateTreeJoin(context, leftNode, rightNode, joinNode);
            context.TreeNodes.Add((joinNode, leftNode, rightNode));
            context.Candidates.JoinNodes[joinCount++] = (byte)joinNode;
        }

        return joinCount;
    }

    /// <summary>
    /// Finds the next available node for use in tree join operations.
    /// </summary>
    /// <param name="context">The compression context containing availability tracking.</param>
    /// <returns>The node ID of the next available node, or MaxByteCodes if none available.</returns>
    /// <remarks>
    /// <para>
    /// This method searches through the frequency-sorted node list to find the next
    /// byte value that is available for use as a join node. It updates the context's
    /// NextAvailableNode index to optimize further searches.
    /// </para>
    /// <para>
    /// The search starts from the current NextAvailableNode position and continues
    /// until an available node is found or the end of the list is reached. This
    /// approach ensures that join nodes are selected in frequency order, using
    /// less common bytes first to minimize the impact on compression effectiveness.
    /// </para>
    /// </remarks>
    private static uint FindNextAvailableNode(CompressionContext context)
    {
        while (
            context.NextAvailableNode < MaxByteCodes
            && context.NodeAvailability.available[context.SortedNodes[context.NextAvailableNode]]
                == 0
        )
        {
            context.NextAvailableNode++;
        }

        return context.NextAvailableNode < MaxByteCodes
            ? context.SortedNodes[context.NextAvailableNode]
            : MaxByteCodes;
    }

    /// <summary>
    /// Creates a tree join operation by configuring the relationships between three nodes.
    /// </summary>
    /// <param name="context">The compression context to modify.</param>
    /// <param name="leftNode">The left child node of the join operation.</param>
    /// <param name="rightNode">The right child node of the join operation.</param>
    /// <param name="joinNode">The parent node that will replace the left-right pair.</param>
    /// <remarks>
    /// <para>
    /// This method establishes a tree join relationship where encountering the sequence
    /// [leftNode, rightNode] in the data will be replaced by [joinNode]. It configures
    /// all the necessary tracking structures to support this transformation.
    /// </para>
    /// <para>
    /// <b>Configuration Steps:</b>
    /// <list type="bullet">
    /// <item><b>Join Node:</b> Marked as unavailable, candidate status 2, clue type 3</item>
    /// <item><b>Left Node:</b> Marked as unavailable, candidate status 2, configured as join trigger (type 1)</item>
    /// <item><b>Right Node:</b> Marked as unavailable, candidate status 2</item>
    /// <item><b>Relationships:</b> Left node points to right node and join node</item>
    /// </list>
    /// </para>
    /// <para>
    /// The different NodeClues values control how the transformation algorithm processes
    /// each node type during data transformation phases.
    /// </para>
    /// </remarks>
    private static void CreateTreeJoin(
        CompressionContext context,
        uint leftNode,
        uint rightNode,
        uint joinNode
    )
    {
        // Mark join node
        context.NodeAvailability.available[joinNode] = 0;
        context.NodeAvailability.candidates[joinNode] = 2;
        context.NodeClues[joinNode] = 3;

        // Configure left node
        context.NodeAvailability.available[leftNode] = 0;
        context.NodeAvailability.candidates[leftNode] = 2;
        context.NodeClues[leftNode] = 1;
        context.RightChildren[leftNode] = (byte)rightNode;
        context.JoinNodes[leftNode] = (byte)joinNode;

        // Mark the right node
        context.NodeAvailability.available[rightNode] = 0;
        context.NodeAvailability.candidates[rightNode] = 2;
    }

    /// <summary>
    /// Restores node states to their normal configuration after join processing completes.
    /// </summary>
    /// <param name="context">The compression context containing the node state.</param>
    /// <param name="joinCount">Number of joins that were processed in this pass.</param>
    /// <remarks>
    /// <para>
    /// This method resets the NodeClues values for nodes that were temporarily
    /// configured for join operations. The reset allows these nodes to be processed
    /// normally in further transformation passes.
    /// </para>
    /// <para>
    /// Specifically, it clears the NodeClues values for:
    /// <list type="bullet">
    /// <item><b>Left nodes:</b> Reset from join trigger (1) to normal (0)</item>
    /// <item><b>Join nodes:</b> Reset from clue expansion (3) to normal (0)</item>
    /// </list>
    /// </para>
    /// <para>
    /// This restoration is essential for maintaining the correct algorithm state
    /// across multiple optimization passes.
    /// </para>
    /// </remarks>
    private static void RestoreNodeStates(CompressionContext context, uint joinCount)
    {
        for (var i = 1U; i < joinCount; i++)
        {
            var leftNode = (context.Candidates.NodePairs[i] >> 8) & 0xFF;
            var joinNode = context.Candidates.JoinNodes[i];
            context.NodeClues[leftNode] = 0;
            context.NodeClues[joinNode] = 0;
        }
    }

    /// <summary>
    /// Writes the complete compressed output including tree structure and transformed data.
    /// </summary>
    /// <param name="context">The compression context containing the compressed data.</param>
    /// <param name="output">The output buffer to write the compressed data to.</param>
    /// <param name="clueNode">The clue node value for the compressed stream.</param>
    /// <param name="treeNodes">List of tree node definitions to include in output.</param>
    /// <remarks>
    /// <para>
    /// This method generates the final compressed output by writing all components
    /// in the correct Binary Tree format order. The output structure follows the
    /// format specification precisely to ensure compatibility with decoders.
    /// </para>
    /// <para>
    /// <b>Output Structure:</b>
    /// <list type="number">
    /// <item><b>Clue Node ID:</b> 8-bit identifier for the special clue node</item>
    /// <item><b>Tree Node Count:</b> 8-bit count of tree join definitions</item>
    /// <item><b>Tree Definitions:</b> For each join: node ID, left child, right child (3×8 bits each)</item>
    /// <item><b>Compressed Data:</b> Transformed data stream (8 bits per byte)</item>
    /// <item><b>Termination:</b> Clue node + zero byte + 7-bit flush sequence</item>
    /// </list>
    /// </para>
    /// <para>
    /// The termination sequence ensures that any remaining bits in the bit buffer
    /// are properly flushed to the output stream, completing the compressed format.
    /// </para>
    /// </remarks>
    private static void WriteCompressionData(
        CompressionContext context,
        DataBuffer output,
        uint clueNode,
        List<(uint node, uint left, uint right)> treeNodes
    )
    {
        // Write tree structure
        WriteBitsToStream(context, output, clueNode, 8);
        WriteBitsToStream(context, output, (uint)treeNodes.Count, 8);

        foreach ((uint node, uint left, uint right) in treeNodes)
        {
            WriteBitsToStream(context, output, node, 8);
            WriteBitsToStream(context, output, left, 8);
            WriteBitsToStream(context, output, right, 8);
        }

        // Write compressed data
        var finalBuffer = context.ActiveBuffer;
        var finalSpan = finalBuffer.ValidReadOnlySpan;
        foreach (var dataByte in finalSpan)
        {
            WriteBitsToStream(context, output, dataByte, 8);
        }

        // Write termination sequence
        WriteBitsToStream(context, output, clueNode, 8);
        WriteBitsToStream(context, output, 0, 8);
        WriteBitsToStream(context, output, 0, 7); // Flush remaining bits
    }

    /// <summary>
    /// Internal compression method that handles the complete compression process for binary data.
    /// </summary>
    /// <param name="inputData">The raw input data to compress.</param>
    /// <param name="outputData">The output buffer for compressed data.</param>
    /// <param name="uncompressedSize">The size to write in the header (may differ from the input size).</param>
    /// <param name="enableZeroSuppression">Whether to apply zero suppression optimization.</param>
    /// <returns>The number of bytes written to the output buffer.</returns>
    /// <remarks>
    /// <para>
    /// This internal method orchestrates the complete compression pipeline from
    /// raw input data to final compressed output. It handles format header generation
    /// and delegates the core compression work to the Binary Tree algorithm.
    /// </para>
    /// <para>
    /// <b>Header Format Selection:</b>
    /// <list type="bullet">
    /// <item><b>Standard (0x46FB):</b> Used when uncompressed size equals input size</item>
    /// <item><b>Extended (0x47FB):</b> Used when sizes differ, includes both sizes</item>
    /// </list>
    /// </para>
    /// <para>
    /// The method uses hardcoded algorithm parameters (256 max passes, 32 max multi-joins)
    /// that provide a good balance between compression quality and processing time
    /// for typical EA game data.
    /// </para>
    /// </remarks>
    private static int CompressData(
        ReadOnlySpan<byte> inputData,
        byte[] outputData,
        int uncompressedSize,
        bool enableZeroSuppression
    )
    {
        var context = new CompressionContext
        {
            InputData = inputData.ToArray(), // Convert to Memory<byte> for storage
        };

        var outputBuffer = new DataBuffer(outputData);

        // Write format header
        if (uncompressedSize == inputData.Length)
        {
            WriteBitsToStream(context, outputBuffer, 0x46FB, 16);
        }
        else
        {
            WriteBitsToStream(context, outputBuffer, 0x47FB, 16);
            WriteBitsToStream(context, outputBuffer, (uint)uncompressedSize, 24);
        }

        WriteBitsToStream(context, outputBuffer, (uint)inputData.Length, 24);

        CompressWithBinaryTree(context, outputBuffer, 256, 32, enableZeroSuppression);
        return outputBuffer.Length;
    }

    /// <summary>
    /// Public interface method for encoding (compressing) data using the Binary Tree algorithm.
    /// </summary>
    /// <param name="decompressedData">The input data to compress.</param>
    /// <param name="compressedData">The output buffer for compressed data.</param>
    /// <returns>The number of bytes written to the compressed data buffer.</returns>
    /// <remarks>
    /// <para>
    /// This method serves as the public entry point for Binary Tree compression,
    /// providing a clean interface that accepts spans and handles the necessary
    /// buffer management internally.
    /// </para>
    /// <para>
    /// The method automatically applies the zero suppression setting from the
    /// <see cref="ShouldZeroSuppress"/> property and handles the conversion
    /// between span-based and array-based APIs.
    /// </para>
    /// <para>
    /// <b>Buffer Management:</b>
    /// <list type="bullet">
    /// <item>Input span is passed directly to internal processing</item>
    /// <item>Output buffer is sized to match the provided compressed data span</item>
    /// <item>Final compressed data is copied back to the output span</item>
    /// <item>Length is automatically limited to prevent buffer overruns</item>
    /// </list>
    /// </para>
    /// </remarks>
    public int Encode(ReadOnlySpan<byte> decompressedData, Span<byte> compressedData)
    {
        var outputBuffer = new byte[compressedData.Length];

        var compressedLength = CompressData(
            decompressedData,
            outputBuffer,
            decompressedData.Length,
            ShouldZeroSuppress
        );

        outputBuffer
            .AsSpan(0, Math.Min(compressedLength, compressedData.Length))
            .CopyTo(compressedData);

        return compressedLength;
    }
}
