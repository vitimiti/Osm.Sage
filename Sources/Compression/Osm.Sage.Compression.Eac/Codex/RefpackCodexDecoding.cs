namespace Osm.Sage.Compression.Eac.Codex;

public partial class RefpackCodex
{
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
}
