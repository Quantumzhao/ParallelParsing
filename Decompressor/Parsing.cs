
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using ParallelParsing.Common;

namespace ParallelParsing;

public unsafe static class Parsing
{
	static object o = new object();
	public static IReadOnlyList<FastqRecord> Parse(CombinedMemory raw)
	{
		string? id;
		string? seq;
		string? other;
		string? quality;
		List<FastqRecord> ret = new List<FastqRecord>(0);

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
		var sb = new StringBuilder();

		while (!IsNewLine(raw[pos]) && raw[pos] != 0)
		{
			sb.Append((char)raw[pos]);

			pos++;
		}

		if (raw[pos] == 0) return null;

		// consume \n
		pos++;
		return sb.ToString();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsNewLine(byte c) => c == '\n' || c == '\r';
}

public struct CombinedMemory : IDisposable
{
	private byte[] _Prepend;
	private byte[] _Rest;
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
			if (i < _Prepend.Length) return _Prepend[i];
			else
			{
				i -= _Prepend.Length;
				return _Rest[i];
			}
		}
	}

	public void Dispose()
	{
		
	}
}
