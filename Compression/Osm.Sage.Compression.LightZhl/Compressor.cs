using JetBrains.Annotations;
using Osm.Sage.Compression.LightZhl.Internals;
using Osm.Sage.Compression.LightZhl.Options;

namespace Osm.Sage.Compression.LightZhl;

[PublicAPI]
public class Compressor : Buffer
{
    private readonly ref struct BackwardInput(
        ReadOnlySpan<byte> source,
        int srcIndex,
        int srcLeft,
        bool slowHash
    )
    {
        public ReadOnlySpan<byte> Source { get; } = source;
        public int SrcIndex { get; } = srcIndex;
        public int SrcLeft { get; } = srcLeft;
        public bool SlowHash { get; } = slowHash;
    }

    private readonly ref struct OverlapInput(
        ReadOnlySpan<byte> source,
        int srcIndex,
        int nRaw,
        int srcLeft
    )
    {
        public ReadOnlySpan<byte> Source { get; } = source;
        public int SrcIndex { get; } = srcIndex;
        public int NRaw { get; } = nRaw;
        public int SrcLeft { get; } = srcLeft;
    }

    private readonly ref struct CommitContext(
        ReadOnlySpan<byte> source,
        int srcEnd,
        bool slowHash,
        Encoder coder,
        bool lazyEnabled
    )
    {
        public ReadOnlySpan<byte> Source { get; } = source;
        public int SrcEnd { get; } = srcEnd;
        public bool SlowHash { get; } = slowHash;
        public Encoder Coder { get; } = coder;
        public bool LazyEnabled { get; } = lazyEnabled;
    }

    private readonly ref struct LazyState(
        int nRaw,
        int len,
        int hashPos,
        int bufPos,
        uint hash,
        uint bufAbsPos
    )
    {
        public int NRaw { get; } = nRaw;
        public int Len { get; } = len;
        public int HashPos { get; } = hashPos;
        public int BufPos { get; } = bufPos;
        public uint Hash { get; } = hash;
        public uint BufAbsPos { get; } = bufAbsPos;
    }

    private readonly struct MatchContext(
        int matchLen,
        int hashPos,
        int wrapBufPos,
        bool lazyForceMatch
    )
    {
        public int MatchLen { get; } = matchLen;
        public int HashPos { get; } = hashPos;
        public int WrapBufPos { get; } = wrapBufPos;
        public bool LazyForceMatch { get; } = lazyForceMatch;
    }

    private readonly ref struct RawWindow(int maxRaw, int srcLeft)
    {
        public int MaxRaw { get; } = maxRaw;
        public int SrcLeft { get; } = srcLeft;
    }

    private readonly struct StateSnapshot(int nRaw, uint hash)
    {
        public int NRaw { get; } = nRaw;
        public uint Hash { get; } = hash;
    }

    private readonly EncoderStat _stat = new();
    private readonly ushort[] _table = new ushort[Globals.TableSize];

    public Compressor() => Array.Fill(_table, unchecked((ushort)-1));

    public List<byte> Compress(
        ReadOnlySpan<byte> source,
        Action<CompressionOptions>? options = null
    )
    {
        CompressionOptions opts = new();
        options?.Invoke(opts);

        Encoder coder = new(_stat);

        var srcIndex = 0;
        var srcEnd = source.Length;

        uint hash = InitializeHash(source, opts.SlowHash);

        while (true)
        {
            if (HandleTailIfNeeded(source, srcIndex, srcEnd, coder))
            {
                break;
            }

            ProcessChunk(source, opts, srcEnd, ref srcIndex, ref hash, coder);
        }

        coder.Flush();
        return coder.Dest;
    }

    private static int ExtendOverlapForwardIfNeeded(
        bool overlap,
        int hashPos,
        int matchLen,
        int wrapBufPos,
        in OverlapInput input
    )
    {
        const int lzMin = 4;

        if (!(overlap && Wrap((uint)(hashPos + matchLen)) == wrapBufPos))
        {
            return matchLen;
        }

        var extraLimit = Math.Min(
            lzMin + Encoder.MaxMatchOver - matchLen,
            input.SrcLeft - input.NRaw - matchLen
        );

        var extra = 0;
        for (; extra < extraLimit; ++extra)
        {
            if (
                input.Source[input.SrcIndex + input.NRaw + extra]
                != input.Source[input.SrcIndex + input.NRaw + matchLen + extra]
            )
            {
                break;
            }
        }

        return matchLen + extra;
    }

    private bool HandleTailIfNeeded(
        ReadOnlySpan<byte> source,
        int srcIndex,
        int srcEnd,
        Encoder coder
    )
    {
        var srcLeft = srcEnd - srcIndex;
        if (srcLeft >= Globals.Match)
        {
            return false;
        }

        FlushTail(source, srcIndex, srcLeft, coder);
        return true;
    }

    private MatchContext ComputeMatchContext(
        ReadOnlySpan<byte> source,
        CompressionOptions opts,
        int srcIndex,
        int srcLeft,
        ref int nRaw,
        ref uint hash
    )
    {
        const int lzMin = 4;

        var hash2 = (int)Globals.HashPos(hash, opts.SlowHash);

        var hashPos = _table[hash2];
        var wrapBufPos = Wrap(BufPos);
        _table[hash2] = (ushort)wrapBufPos;

        var matchLen = 0;
        var lazyForceMatch = false;

        if (hashPos == ushort.MaxValue || hashPos == wrapBufPos)
        {
            return new MatchContext(matchLen, hashPos, wrapBufPos, lazyForceMatch);
        }

        var matchLimit = Math.Min(
            Math.Min(Distance(wrapBufPos - hashPos), srcLeft - nRaw),
            lzMin + Encoder.MaxMatchOver
        );

        matchLen = Match(hashPos, source[(srcIndex + nRaw)..], matchLimit);
        matchLen = ExtendOverlapForwardIfNeeded(
            opts.Overlap,
            hashPos,
            matchLen,
            wrapBufPos,
            new OverlapInput(source, srcIndex, nRaw, srcLeft)
        );

        if (
            opts.BackwardMatch
            && TryBackwardMatch(
                ref nRaw,
                ref matchLen,
                ref hashPos,
                ref wrapBufPos,
                ref hash,
                new BackwardInput(source, srcIndex, srcLeft, opts.SlowHash)
            )
        )
        {
            // Force committing the match if we extended backward.
            lazyForceMatch = true;
        }

        return new MatchContext(matchLen, hashPos, wrapBufPos, lazyForceMatch);
    }

    private bool ResolveLazyIfAny(
        bool lazyEnabled,
        MatchContext mc,
        ref int srcIndex,
        ref int nRaw,
        ref uint hash,
        in LazyState lazy,
        in CommitContext ctx
    )
    {
        const int lzMin = 4;

        if (!(lazyEnabled && lazy.Len >= lzMin))
        {
            return false;
        }

        if (mc.MatchLen > lazy.Len)
        {
            CommitCurrentMatch(
                ref srcIndex,
                nRaw,
                mc.MatchLen,
                mc.HashPos,
                mc.WrapBufPos,
                ref hash,
                ctx
            );
        }
        else
        {
            CommitLazyMatch(ref srcIndex, ref nRaw, ref hash, lazy, ctx);
        }

        return true;
    }

    private bool HandleImmediateMatch(
        bool lazyEnabled,
        MatchContext mc,
        ref LazyState lazy,
        in StateSnapshot snap,
        ref int srcIndex,
        ref uint hash,
        in CommitContext ctx
    )
    {
        if (lazyEnabled && !mc.LazyForceMatch)
        {
            // Save lazy state
            lazy = new LazyState(
                snap.NRaw,
                mc.MatchLen,
                mc.HashPos,
                mc.WrapBufPos,
                snap.Hash,
                BufPos
            );

            return false;
        }

        // Commit immediately
        CommitCurrentMatch(
            ref srcIndex,
            snap.NRaw,
            mc.MatchLen,
            mc.HashPos,
            mc.WrapBufPos,
            ref hash,
            ctx
        );

        return true;
    }

    private bool HandleRawOverflow(
        in MatchContext mc,
        ref int nRaw,
        in RawWindow rw,
        ref int srcIndex,
        ref uint hash,
        in LazyState lazy,
        in CommitContext ctx
    )
    {
        const int lzMin = 4;

        if (nRaw + 1 <= rw.MaxRaw)
        {
            return false;
        }

        if (ctx.LazyEnabled && lazy.Len >= lzMin)
        {
            if (mc.MatchLen > lazy.Len)
            {
                CommitCurrentMatch(
                    ref srcIndex,
                    nRaw,
                    mc.MatchLen,
                    mc.HashPos,
                    mc.WrapBufPos,
                    ref hash,
                    ctx
                );
            }
            else
            {
                CommitLazyMatch(ref srcIndex, ref nRaw, ref hash, lazy, ctx);
            }

            return true;
        }

        FlushRaw(ctx.Source, ref srcIndex, rw.SrcLeft, ref nRaw, ctx.Coder);
        return true;
    }

    private void ProcessChunk(
        ReadOnlySpan<byte> source,
        CompressionOptions opts,
        int srcEnd,
        ref int srcIndex,
        ref uint hash,
        Encoder coder
    )
    {
        const int lzMin = 4;

        var srcLeft = srcEnd - srcIndex;

        var nRaw = 0;
        var maxRaw = Math.Min(srcLeft - Globals.Match, Encoder.MaxRaw);

        // Lazy match bookkeeping
        var lazy = new LazyState(0, 0, 0, 0, 0, 0);

        while (true)
        {
            var mc = ComputeMatchContext(source, opts, srcIndex, srcLeft, ref nRaw, ref hash);

            if (
                ResolveLazyIfAny(
                    opts.LazyMatch,
                    mc,
                    ref srcIndex,
                    ref nRaw,
                    ref hash,
                    in lazy,
                    new CommitContext(source, srcEnd, opts.SlowHash, coder, opts.LazyMatch)
                )
            )
            {
                break;
            }

            if (
                mc.MatchLen >= lzMin
                && HandleImmediateMatch(
                    opts.LazyMatch,
                    mc,
                    ref lazy,
                    new StateSnapshot(nRaw, hash),
                    ref srcIndex,
                    ref hash,
                    new CommitContext(source, srcEnd, opts.SlowHash, coder, opts.LazyMatch)
                )
            )
            {
                break;
            }

            if (
                HandleRawOverflow(
                    mc,
                    ref nRaw,
                    new RawWindow(maxRaw, srcLeft),
                    ref srcIndex,
                    ref hash,
                    in lazy,
                    new CommitContext(source, srcEnd, opts.SlowHash, coder, opts.LazyMatch)
                )
            )
            {
                break;
            }

            // Advance one raw byte
            hash = Globals.UpdateHash(hash, source, srcIndex + nRaw, opts.SlowHash);
            ToBuf(source[srcIndex + nRaw]);
            nRaw++;
        }
    }

    private uint UpdateTable(
        uint hash,
        ReadOnlySpan<byte> source,
        uint pos,
        int len,
        bool slowHash = false
    )
    {
        var sourceIndex = 0;
        switch (len)
        {
            case <= 0:
                return 0;
            case > Globals.SkipHash:
            {
                ++sourceIndex;
                hash = 0;
                for (var i = 0; i < Globals.Match; i++)
                {
                    hash = Globals.UpdateHash(hash, source[sourceIndex + len + i], slowHash);
                }

                return hash;
            }
        }

        hash = Globals.UpdateHash(hash, source, sourceIndex, slowHash);
        ++sourceIndex;

        for (var i = 0; i < len; ++i)
        {
            var wrappedPos = (ushort)((pos + (uint)i) & Globals.BufMask);
            _table[Globals.HashPos(hash, slowHash)] = wrappedPos;
            hash = Globals.UpdateHash(hash, source, sourceIndex + i, slowHash);
        }

        return hash;
    }

    private static uint InitializeHash(ReadOnlySpan<byte> source, bool slowHash)
    {
        uint hash = 0;
        if (source.Length < Globals.Match)
        {
            return hash;
        }

        for (var i = 0; i < Globals.Match; i++)
        {
            hash = Globals.UpdateHash(hash, source[i], slowHash);
        }

        return hash;
    }

    private void FlushTail(ReadOnlySpan<byte> source, int srcIndex, int srcLeft, Encoder coder)
    {
        if (srcLeft <= 0)
        {
            return;
        }

        ToBuf(source.Slice(srcIndex, srcLeft));
        coder.PutRaw(source.Slice(srcIndex, srcLeft));
    }

    private void CommitCurrentMatch(
        ref int srcIndex,
        int nRaw,
        int matchLen,
        int hashPos,
        int wrapBufPos,
        ref uint hash,
        in CommitContext ctx
    )
    {
        const int lzMin = 4;

        ctx.Coder.PutMatch(
            ctx.Source.Slice(srcIndex, nRaw),
            matchLen - lzMin,
            Distance(wrapBufPos - hashPos)
        );

        var rem = ctx.SrcEnd - (srcIndex + nRaw + 1) - Globals.Match;
        var updLen = Math.Min(matchLen - 1, Math.Max(rem, 0));
        hash = UpdateTable(hash, ctx.Source[(srcIndex + nRaw)..], BufPos + 1, updLen, ctx.SlowHash);

        ToBuf(ctx.Source.Slice(srcIndex + nRaw, matchLen));
        srcIndex += nRaw + matchLen;
    }

    private void CommitLazyMatch(
        ref int srcIndex,
        ref int nRaw,
        ref uint hash,
        in LazyState state,
        in CommitContext ctx
    )
    {
        const int lzMin = 4;

        // Restore nRaw to the captured lazy state
        nRaw = state.NRaw;

        // Restore hash pipeline for UPDATE_HASH_EX step
        hash = state.Hash;
        hash = Globals.UpdateHash(hash, ctx.Source, srcIndex + nRaw, ctx.SlowHash);

        // Rewind buffer to the absolute position when lazy was captured
        if (BufPos != state.BufAbsPos)
        {
            Rewind(BufPos - state.BufAbsPos);
        }

        // Emit the lazy match using saved wrapped disp base
        ctx.Coder.PutMatch(
            ctx.Source.Slice(srcIndex, nRaw),
            state.Len - lzMin,
            Distance(state.BufPos - state.HashPos)
        );

        // Update table using the restored absolute position (+2)
        var rem = ctx.SrcEnd - (srcIndex + nRaw + 2) - Globals.Match;
        var updLen = Math.Min(state.Len - 2, Math.Max(rem, 0));
        hash = UpdateTable(
            hash,
            ctx.Source[(srcIndex + nRaw + 1)..],
            state.BufAbsPos + 2,
            updLen,
            ctx.SlowHash
        );

        // Write the match bytes starting at the restored position
        ToBuf(ctx.Source.Slice(srcIndex + nRaw, state.Len));
        srcIndex += nRaw + state.Len;
    }

    private void FlushRaw(
        ReadOnlySpan<byte> source,
        ref int srcIndex,
        int srcLeft,
        ref int nRaw,
        Encoder coder
    )
    {
        if (nRaw + Globals.Match >= srcLeft && srcLeft <= Encoder.MaxRaw)
        {
            ToBuf(source.Slice(srcIndex + nRaw, srcLeft - nRaw));
            nRaw = srcLeft;
        }

        coder.PutRaw(source.Slice(srcIndex, nRaw));
        srcIndex += nRaw;
    }

    private bool TryBackwardMatch(
        ref int nRaw,
        ref int matchLen,
        ref ushort hashPos,
        ref int wrapBufPos,
        ref uint hash,
        in BackwardInput input
    )
    {
        const int lzMin = 4;

        if (matchLen < lzMin - 1)
        {
            return false;
        }

        // Compute how much we are allowed to extend
        var d = Distance(wrapBufPos - hashPos); // distance between current pos and match pos
        var extraLimit = int.Min(lzMin + Encoder.MaxMatchOver - matchLen, nRaw);
        extraLimit = int.Min(Math.Min(extraLimit, d - matchLen), Globals.BufSize - d);
        if (extraLimit <= 0)
        {
            return false;
        }

        var extra = 0;
        // Compare backwards: buffer byte before hashPos - extra - 1 vs source byte before current window
        for (; extra < extraLimit; ++extra)
        {
            var bufByte = Buf[Wrap((uint)(hashPos - extra - 1))];
            var srcByte = input.Source[input.SrcIndex + nRaw - extra - 1];
            if (bufByte != srcByte)
            {
                break;
            }
        }

        if (extra == 0)
        {
            return false;
        }

        // Apply extension: shift window left
        nRaw -= extra;
        Rewind((uint)extra); // BufPos -= extra
        hashPos -= (ushort)extra;
        matchLen += extra;
        wrapBufPos = Wrap(BufPos);

        hash =
            input.SrcLeft - nRaw >= Globals.Match
                ? Globals.CalcHash(
                    input.Source.Slice(input.SrcIndex + nRaw, Globals.Match),
                    input.SlowHash
                )
                : 0;

        return true;
    }
}
