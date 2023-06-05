
using System.Buffers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ParallelParsing.Common;

namespace ParallelParsing;

public unsafe static class Parsing
{
	static object o = new object();
	public static IEnumerable<FastqRecord> Parse(CombinedMemory raw)
	{
		// var ret = new List<FastqRecord>();

		for (int i = 0; i < raw.Length;)
		{
			// empty space
			if (raw[i] == '\0') break;

			// if (raw[i] != '@') throw new Exception();
			// skip @
			i++;
			var start = i;

			var idnFrom = i;
			var idnLen = ParseLine(ref i, raw);
			if (idnLen == -1) break;

			var seqFrom = i;
			var seqLen = ParseLine(ref i, raw);
			if (seqLen == -1) break;
			// skip +
			// if (raw[i] != '+') throw new Exception();
			i++;

			var plsFrom = i;
			var plsLen = ParseLine(ref i, raw);
			if (plsLen == -1) break;

			var qltFrom = i;
			var qltLen = ParseLine(ref i, raw);
			if (qltLen == -1) break;
			var end = i;

			var owner = MemoryPool<byte>.Shared.Rent(end - start);
			var mem = owner.Memory;
			raw.CopyTo(start, end, mem);
			var idn = mem.Slice(idnFrom - start, idnLen);
			var seq = mem.Slice(seqFrom - start, seqLen);
			var pls = mem.Slice(plsFrom - start, plsLen);
			var qlt = mem.Slice(qltFrom - start, qltLen);

			yield return new FastqRecord(owner, idn, seq, pls, qlt);
			// ret.Add(new FastqRecord(id, seq, other, quality));
		}

		// return ret;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ParseLine(ref int pos, CombinedMemory raw)
	{
		var start = pos;
		while (true)
		{
			var b = raw[pos];
			if (b == '\n' || b == 0) break;
			else pos++;
		}

		if (raw[pos] == 0) return -1;
		
		// consume \n
		pos++;
		return pos - start;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsNewLine(byte c) => c == '\n' || c == '\r';
}

public struct CombinedMemory
{
	private Memory<byte> _Prepend;
	private int _LengthP;
	private Memory<byte> _Rest;
	public readonly int Length;

	public CombinedMemory(byte[]? prepend, Memory<byte> rest)
	{
		_Prepend = prepend;
		_Rest = rest;
		_LengthP = prepend?.Length ?? 0;
		Length = _LengthP + _Rest.Length;
	}

	public byte this[int i]
	{
		get
		{
			if (i < _LengthP) return _Prepend.Span[i];
			else return _Rest.Span[i - _LengthP];
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyTo(int from, int to, Memory<byte> buf)
	{
		if (from < _LengthP && to < _LengthP)
		{
			var sub = _Prepend.Slice(from, to - from);
			sub.CopyTo(buf);
		}
		else if (from < _LengthP && to >= _LengthP)
		{
			var pre = _Prepend.Slice(from, _LengthP - from);
			var post = _Rest.Slice(0, to - _LengthP);
			pre.CopyTo(buf);
			post.CopyTo(buf.Slice(_LengthP - from));
		}
		else
		{
			var sub = _Rest.Slice(from - _LengthP, to - from);
			sub.CopyTo(buf);
		}
	}
}
