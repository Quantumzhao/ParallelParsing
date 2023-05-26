
using System.Text;
using ParallelParsing.Common;

namespace ParallelParsing.Benchmark.Naive;

public static class Parsing
{
	public static IEnumerable<FastqRecord> Parse(BigQueue<byte> raw)
	{
		string id;
		string seq;
		string other;
		string quality;
		List<FastqRecord> ret = new List<FastqRecord>(0);

		for (int i = 0; !raw.IsAtEnd; i++)
		{
			// emtry space
			if (raw.Peek() == '\0')
			{
				return ret;
			}

			// skip @
			if (raw.Dequeue() != '@') throw new Exception();
			id = ParseLine(raw);
			seq = ParseLine(raw);
			// skip +
			if (raw.Dequeue() != '+') throw new Exception();
			other = ParseLine(raw);
			quality = ParseLine(raw);

			ret.Add(new FastqRecord(id, seq, other, quality));
		}

		return ret;
	}
	private static string ParseLine(BigQueue<byte> raw)
	{
		var sb = new StringBuilder();

		while (!IsNewLine(raw.Peek()))
		{
			sb.Append((char)raw.Dequeue());
		}

		// consume \n
		raw.Dequeue();
		return sb.ToString();
	}

	private static bool IsNewLine(byte c) => c == '\n' || c == '\r';
}