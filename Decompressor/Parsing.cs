
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using ParallelParsing.Common;

namespace ParallelParsing;

public unsafe static class Parsing
{
	public static IReadOnlyCollection<FastqRecord> Parse(CombinedMemory raw)
	{
		string? id;
		string? seq;
		string? other;
		string? quality;
		Collection<FastqRecord> ret = new Collection<FastqRecord>();

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

			ret.Add(new FastqRecord(id, seq, other, quality));
		}

		return ret;
	}
	private static string? ParseLine(ref int pos, CombinedMemory raw)
	{
		var start = pos;

		while (!IsNewLine(raw[pos]) && raw[pos] != 0)
		{
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
	private Memory<byte> _Rest;
	public readonly int Length;

	public CombinedMemory(byte[]? prepend, byte[] rest)
	{
		_Prepend = prepend ?? Array.Empty<byte>();
		_Rest = rest;
		Length = _Prepend.Length + _Rest.Length;
	}

	public byte this[int i]
	{
		get
		{
			if (i < _Prepend.Length) return _Prepend.Span[i];
			else
			{
				i -= _Prepend.Length;
				return _Rest.Span[i];
			}
		}
	}

	public string Substring(int from, int to)
	{
		var lenP = _Prepend.Length;
		var lenR = _Rest.Length;
		if (from < lenP && to < lenP)
		{
			return Encoding.ASCII.GetString(_Prepend.Slice(from, to - from).Span);
		}
		else if (from < lenP && to >= lenP)
		{
			var first = _Prepend.Slice(from, lenP - from);
			var second = _Rest.Slice(0, to - lenP);
			Span<byte> buf = stackalloc byte[to - from];
			first.Span.CopyTo(buf);
			second.Span.CopyTo(buf.Slice(_Prepend.Length - from));
			return Encoding.ASCII.GetString(buf);
		}
		else if (from >= lenP && to >= lenP)
		{
			return Encoding.ASCII.GetString(_Rest.Slice(from - lenP, to - from).Span);
		}
		else throw new UnreachableException();
	}
}
