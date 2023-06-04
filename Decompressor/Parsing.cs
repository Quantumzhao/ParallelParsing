
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using ParallelParsing.Common;

namespace ParallelParsing;

public unsafe static class Parsing
{
	static object o = new object();
	public static IEnumerable<FastqRecord> Parse(CombinedMemory raw)
	{
		string? id;
		string? seq;
		string? other;
		string? quality;
		// Collection<FastqRecord> ret = new Collection<FastqRecord>();

		for (int i = 0; i < raw.Length; )
		{
			// empty space
			if (raw[i] == '\0') break;

			// skip @
			if (raw[i] != '@') throw new Exception();
			i++;
			// counter++;
			id = ParseLine(ref i, raw);
			if (id == null) break;

			seq = ParseLine(ref i, raw);
			if (seq == null) break;
			// skip +
			if (raw[i] != '+') throw new Exception();
			i++;

			other = ParseLine(ref i, raw);
			if (other == null) break;

			quality = ParseLine(ref i, raw);
			if (quality == null) break;

			yield return new FastqRecord(id, seq, other, quality);
			// ret.Add(new FastqRecord(id, seq, other, quality));
		}

		// return ret;
	}
	private static string? ParseLine(ref int pos, CombinedMemory raw)
	{
		var start = pos;

		while (true)
		{
			var b = raw[pos];
			if (IsNewLine(b) || b == 0) break;

			pos++;
		}

		if (raw[pos] == 0) return null;
		var ret = raw.Substring(start, pos);

		// consume \n
		pos++;
		return ret;
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

	public CombinedMemory(byte[]? prepend, byte[] rest)
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

	public string Substring(int from, int to)
	{
		if (from < _LengthP && to < _LengthP)
		{
			var sub = _Prepend.Slice(from, to - from);
			return Encoding.ASCII.GetString(sub.Span);
		}
		else if (from < _LengthP && to >= _LengthP)
		{
			Span<byte> ret = stackalloc byte[to - from];
			var pre = _Prepend.Slice(from, _LengthP - from);
			var post = _Rest.Slice(0, to - _LengthP);
			pre.Span.CopyTo(ret);
			post.Span.CopyTo(ret.Slice(_LengthP - from));
			return Encoding.ASCII.GetString(ret);
		}
		else
		{
			var sub = _Rest.Slice(from - _LengthP, to - from);
			return Encoding.ASCII.GetString(sub.Span);
		}
	}
}
