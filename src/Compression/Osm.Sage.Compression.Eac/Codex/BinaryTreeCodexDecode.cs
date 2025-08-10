using System.Buffers.Binary;

namespace Osm.Sage.Compression.Eac.Codex;

public partial class BinaryTreeCodex
{
    /// <summary>
    /// Maximum number of nodes supported in the binary tree structure.
    /// </summary>
    private const int NodeTableSize = 256;

    /// <summary>
    /// Extended Binary Tree format signature with metadata (0x47FB).
    /// </summary>
    private const ushort ExtendedSignature = 0x47FB;

    /// <summary>
    /// Manages the binary tree structure used for decompression and handles the decoding process.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The decoder maintains three key data structures:
    /// <list type="bullet">
    /// <item><b>Node Types:</b> Identifies each node as leaf (0), special (1), or internal (-1)</item>
    /// <item><b>Left Children:</b> Maps internal nodes to their left child nodes</item>
    /// <item><b>Right Children:</b> Maps internal nodes to their right child nodes</item>
    /// </list>
    /// </para>
    /// <para>
    /// The decoding algorithm traverses the tree structure, outputting leaf node values
    /// and recursively processing internal nodes to reconstruct the original data.
    /// </para>
    /// </remarks>
    private struct BinaryTreeDecoder()
    {
        /// <summary>
        /// Node type classification: 0 = leaf, 1 = special, -1 = internal.
        /// </summary>
        private readonly sbyte[] _nodeTypes = new sbyte[NodeTableSize];

        /// <summary>
        /// Left child node IDs for internal nodes.
        /// </summary>
        private readonly byte[] _leftChildren = new byte[NodeTableSize];

        /// <summary>
        /// Right child node IDs for internal nodes.
        /// </summary>
        private readonly byte[] _rightChildren = new byte[NodeTableSize];

        /// <summary>
        /// Current write position in the output buffer.
        /// </summary>
        private int _outputPosition = 0;

        /// <summary>
        /// Processes an internal node by recursively decoding its left and right subtrees.
        /// </summary>
        /// <param name="output">The output buffer to write decoded data to.</param>
        /// <param name="nodeId">The internal node ID to process.</param>
        /// <remarks>
        /// This method implements the core tree traversal logic, visiting child nodes
        /// in left-to-right order to maintain the correct decoding sequence.
        /// </remarks>
        private void ProcessInternalNode(Span<byte> output, byte nodeId)
        {
            ProcessNode(output, _leftChildren[nodeId]);
            ProcessNode(output, _rightChildren[nodeId]);
        }

        /// <summary>
        /// Initializes all nodes as leaf nodes (type 0).
        /// </summary>
        /// <remarks>
        /// This method prepares the decoder for building a new tree structure
        /// by resetting all nodes to their default leaf state.
        /// </remarks>
        public void InitializeNodes() => Array.Fill(_nodeTypes, (sbyte)0); // 0 = leaf node

        /// <summary>
        /// Marks a node as special (type 1) for format-specific handling.
        /// </summary>
        /// <param name="nodeId">The node ID to mark as special.</param>
        /// <remarks>
        /// Special nodes have format-specific meaning in the Binary Tree algorithm
        /// and are used as part of the decompression initialization process.
        /// </remarks>
        public void SetSpecialNode(byte nodeId) => _nodeTypes[nodeId] = 1; // Mark as special

        /// <summary>
        /// Configures a node as an internal node with specified left and right children.
        /// </summary>
        /// <param name="nodeId">The node ID to configure as internal.</param>
        /// <param name="leftChild">The left child node ID.</param>
        /// <param name="rightChild">The right child node ID.</param>
        /// <remarks>
        /// Internal nodes (type -1) represent branch points in the binary tree
        /// and contain references to their child nodes for tree traversal.
        /// </remarks>
        public void SetInternalNode(byte nodeId, byte leftChild, byte rightChild)
        {
            _nodeTypes[nodeId] = -1; // Mark as an internal node
            _leftChildren[nodeId] = leftChild;
            _rightChildren[nodeId] = rightChild;
        }

        /// <summary>
        /// Writes a leaf node value to the output buffer and advances the position.
        /// </summary>
        /// <param name="output">The output buffer to write to.</param>
        /// <param name="value">The byte value to write.</param>
        /// <remarks>
        /// This method handles bounds checking to prevent buffer overruns and
        /// automatically advances the output position for further writings.
        /// </remarks>
        public void WriteLeafValue(Span<byte> output, byte value)
        {
            if (_outputPosition >= output.Length)
            {
                return;
            }

            output[_outputPosition] = value;
            _outputPosition++;
        }

        /// <summary>
        /// Processes a node based on its type, either writing leaf values or traversing subtrees.
        /// </summary>
        /// <param name="output">The output buffer for decoded data.</param>
        /// <param name="nodeId">The node ID to process.</param>
        /// <remarks>
        /// <para>
        /// This is the main dispatch method that handles different node types:
        /// <list type="bullet">
        /// <item><b>Leaf nodes (type 0):</b> Output the node ID as a literal byte value</item>
        /// <item><b>Internal nodes (type &lt; 0):</b> Recursively process left and right subtrees</item>
        /// <item><b>Special nodes (type 1):</b> Currently ignored in processing</item>
        /// </list>
        /// </para>
        /// </remarks>
        public void ProcessNode(Span<byte> output, byte nodeId)
        {
            var nodeType = _nodeTypes[nodeId];
            switch (nodeType)
            {
                // Leaf node
                case 0:
                    WriteLeafValue(output, nodeId);
                    break;
                // Internal node
                case < 0:
                    ProcessInternalNode(output, nodeId);
                    break;
            }
        }
    }

    /// <summary>
    /// Provides sequential reading capabilities for compressed data with built-in validation.
    /// </summary>
    /// <param name="data">The compressed data span to read from.</param>
    /// <remarks>
    /// <para>
    /// This ref struct efficiently handles sequential data reading operations
    /// with automatic bounds checking and format-specific multibyte reads.
    /// Being a ref struct ensures stack-only allocation for optimal performance.
    /// </para>
    /// <para>
    /// The reader supports various data formats commonly used in Binary Tree compression:
    /// <list type="bullet">
    /// <item>8-bit byte values</item>
    /// <item>16-bit big-endian integers (signatures)</item>
    /// <item>24-bit big-endian integers (size fields)</item>
    /// </list>
    /// </para>
    /// </remarks>
    private ref struct DataReader(ReadOnlySpan<byte> data)
    {
        /// <summary>
        /// The source data being read from.
        /// </summary>
        private readonly ReadOnlySpan<byte> _data = data;

        /// <summary>
        /// Current read position within the data.
        /// </summary>
        private int _position = 0;

        /// <summary>
        /// Gets whether there is more data available to read.
        /// </summary>
        /// <value><c>true</c> if more data is available; otherwise, <c>false</c>.</value>
        public bool HasData => _position < _data.Length;

        /// <summary>
        /// Reads a single byte from the current position and advances the position.
        /// </summary>
        /// <returns>The byte value at the current position.</returns>
        /// <exception cref="ArgumentException">Thrown when insufficient data is available.</exception>
        public byte ReadByte()
        {
            ValidateCanRead(1);
            return _data[_position++];
        }

        /// <summary>
        /// Skips the specified number of bytes by advancing the read position.
        /// </summary>
        /// <param name="bytes">The number of bytes to skip.</param>
        /// <exception cref="ArgumentException">Thrown when insufficient data is available to skip.</exception>
        /// <remarks>
        /// This method is useful for skipping metadata sections or padding
        /// that are not needed for the current decompression operation.
        /// </remarks>
        public void Skip(int bytes)
        {
            ValidateCanRead(bytes);
            _position += bytes;
        }

        /// <summary>
        /// Reads a 24-bit big-endian integer from the current position.
        /// </summary>
        /// <returns>The 24-bit integer value as a 32-bit int.</returns>
        /// <exception cref="ArgumentException">Thrown when insufficient data is available.</exception>
        /// <remarks>
        /// Binary Tree format uses 24-bit size fields to specify uncompressed
        /// data lengths, limiting the maximum file size to 16MB (2^24 bytes).
        /// </remarks>
        public int Read24BitBigEndian()
        {
            ValidateCanRead(3);
            var result =
                (_data[_position] << 16) | (_data[_position + 1] << 8) | _data[_position + 2];

            _position += 3;
            return result;
        }

        /// <summary>
        /// Reads a 16-bit big-endian integer from the current position.
        /// </summary>
        /// <returns>The 16-bit integer value.</returns>
        /// <exception cref="ArgumentException">Thrown when insufficient data is available.</exception>
        /// <remarks>
        /// Used primarily for reading format signatures that identify the
        /// specific variant of Binary Tree compression being processed.
        /// </remarks>
        public ushort Read16BitBigEndian()
        {
            ValidateCanRead(2);
            var result = BinaryPrimitives.ReadUInt16BigEndian(_data.Slice(_position, 2));
            _position += 2;
            return result;
        }

        /// <summary>
        /// Validates that sufficient data is available for the requested read operation.
        /// </summary>
        /// <param name="bytesNeeded">The number of bytes required for the read operation.</param>
        /// <exception cref="ArgumentException">Thrown when insufficient data is available.</exception>
        private void ValidateCanRead(int bytesNeeded)
        {
            if (_position + bytesNeeded > _data.Length)
            {
                throw new ArgumentException($"Not enough data to read {bytesNeeded} bytes");
            }
        }
    }

    /// <summary>
    /// Parses the Binary Tree format header and extracts the uncompressed data size.
    /// </summary>
    /// <param name="reader">The data reader positioned at the start of the compressed data.</param>
    /// <returns>The expected size of the uncompressed data in bytes.</returns>
    /// <remarks>
    /// <para>
    /// The header parsing handles both Binary Tree format variants:
    /// <list type="bullet">
    /// <item><b>Standard (0x46FB):</b> 2-byte signature + 3-byte size</item>
    /// <item><b>Extended (0x47FB):</b> 2-byte signature + 3-byte metadata + 3-byte size</item>
    /// </list>
    /// </para>
    /// <para>
    /// The method automatically skips the metadata section for extended format
    /// files to ensure consistent size field positioning across variants.
    /// </para>
    /// </remarks>
    private static int ParseHeader(ref DataReader reader)
    {
        var signature = reader.Read16BitBigEndian();

        // Skip metadata for extended format only
        if (signature is ExtendedSignature)
        {
            reader.Skip(3);
        }
        // For standard format (StandardSignature), we proceed directly to size field

        return reader.Read24BitBigEndian(); // Return uncompressed length
    }

    /// <summary>
    /// Constructs the binary tree structure from the compressed data tree definition.
    /// </summary>
    /// <param name="reader">The data reader positioned after the header.</param>
    /// <param name="decoder">The decoder instance to configure with tree structure.</param>
    /// <remarks>
    /// <para>
    /// The tree building process follows this sequence:
    /// <list type="number">
    /// <item>Initialize all nodes as leaf nodes</item>
    /// <item>Read and mark the special node ID</item>
    /// <item>Read the node count and configure each internal node with its children</item>
    /// </list>
    /// </para>
    /// <para>
    /// Each internal node definition consists of three bytes:
    /// <list type="bullet">
    /// <item>Node ID (the internal node being defined)</item>
    /// <item>Left child node ID</item>
    /// <item>Right child node ID</item>
    /// </list>
    /// </para>
    /// </remarks>
    private static void BuildDecodingTree(ref DataReader reader, ref BinaryTreeDecoder decoder)
    {
        decoder.InitializeNodes();

        var specialNodeId = reader.ReadByte();
        decoder.SetSpecialNode(specialNodeId);

        var nodeCount = reader.ReadByte();
        for (var i = 0; i < nodeCount; i++)
        {
            var nodeId = reader.ReadByte();
            var leftChild = reader.ReadByte();
            var rightChild = reader.ReadByte();
            decoder.SetInternalNode(nodeId, leftChild, rightChild);
        }
    }

    /// <summary>
    /// Processes the compressed data payload using the constructed binary tree.
    /// </summary>
    /// <param name="reader">The data reader positioned at the compressed payload.</param>
    /// <param name="decoder">The configured decoder with a built tree structure.</param>
    /// <param name="output">The output buffer for decompressed data.</param>
    /// <remarks>
    /// <para>
    /// The decoding algorithm processes the compressed data in this pattern:
    /// <list type="number">
    /// <item>Read a node ID and process it through the tree</item>
    /// <item>Read the next byte - if it's 0, stop decoding</item>
    /// <item>Otherwise, output the byte as a literal value</item>
    /// <item>Repeat until data exhausted or termination byte encountered</item>
    /// </list>
    /// </para>
    /// <para>
    /// This dual approach allows the format to efficiently encode both
    /// tree-compressed sequences and literal byte values within the same stream.
    /// </para>
    /// </remarks>
    private static void DecodeData(
        ref DataReader reader,
        ref BinaryTreeDecoder decoder,
        Span<byte> output
    )
    {
        while (reader.HasData)
        {
            var nodeId = reader.ReadByte();
            decoder.ProcessNode(output, nodeId);

            if (!reader.HasData)
            {
                break;
            }

            var nextByte = reader.ReadByte();
            if (nextByte == 0)
            {
                break;
            }

            decoder.WriteLeafValue(output, nextByte);
        }
    }

    /// <summary>
    /// Decompresses Binary Tree format data into the specified output buffer.
    /// </summary>
    /// <param name="compressedData">The compressed data to decode.</param>
    /// <param name="decompressedData">The output buffer for decompressed data.</param>
    /// <returns>The expected uncompressed data size from the header.</returns>
    /// <exception cref="ArgumentException">Thrown when the compressed data is invalid or corrupted.</exception>
    /// <remarks>
    /// <para>
    /// This is the main entry point for Binary Tree decompression, orchestrating
    /// the complete decompression process through these phases:
    /// <list type="number">
    /// <item><b>Header Parsing:</b> Extract format signature and uncompressed size</item>
    /// <item><b>Tree Construction:</b> Build the binary tree from node definitions</item>
    /// <item><b>Data Decoding:</b> Process compressed payload using the tree structure</item>
    /// </list>
    /// </para>
    /// <para>
    /// The method returns the expected uncompressed size as specified in the header,
    /// which callers can use to validate that the decompression completed
    /// successfully and produced the expected amount of data.
    /// </para>
    /// </remarks>
    public int Decode(ReadOnlySpan<byte> compressedData, Span<byte> decompressedData)
    {
        var reader = new DataReader(compressedData);
        var decoder = new BinaryTreeDecoder();

        var uncompressedLength = ParseHeader(ref reader);
        BuildDecodingTree(ref reader, ref decoder);
        DecodeData(ref reader, ref decoder, decompressedData);

        return uncompressedLength;
    }
}
