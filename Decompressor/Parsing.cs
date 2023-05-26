
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using ParallelParsing.Common;

namespace ParallelParsing;

public unsafe static class Parsing
{	
	public static IReadOnlyList<FastqRecord> Parse(byte[] raw)
	{
		string id;
		string seq;
		string other;
		string quality;
		List<FastqRecord> ret = new List<FastqRecord>(0);

		fixed (byte* start = raw)
		{
			var curr = start;
			for (int i = 0; (curr - start) < raw.Length; i++)
			{
				// emtry space
				if (*curr == '\0')
				{
					return ret;
				}

				// skip @
				if (*curr != '@') throw new Exception();
				curr++;
					// counter++;
				id = ParseLine(&curr);
				seq = ParseLine(&curr);
				// skip +
				if (*curr != '+') throw new Exception();
				curr++;
				other = ParseLine(&curr);
				quality = ParseLine(&curr);

				ret.Add(new FastqRecord(id, seq, other, quality));
			}
		}

		return ret;
	}
	private static string ParseLine(byte** currChar)
	{
		var sb = new StringBuilder();

		while (!IsNewLine(**currChar))
		{
			sb.Append((char)**currChar);

			(*currChar)++;
		}

		// consume \n
		(*currChar)++;
		return sb.ToString();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsNewLine(byte c) => c == '\n' || c == '\r';
}
